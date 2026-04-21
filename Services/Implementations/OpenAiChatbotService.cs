using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class OpenAiChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ISettingService _settingService;
        private readonly IChatbotModerationService _moderationService;
        private readonly IChatbotPiiRedactionService _piiRedactionService;
        private readonly IChatbotPromptBuilder _promptBuilder;
        private readonly IChatbotKnowledgeService _knowledgeService;
        private readonly IAuditService _auditService;
        private readonly ILogger<OpenAiChatbotService> _logger;

        public OpenAiChatbotService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ISettingService settingService,
            IChatbotModerationService moderationService,
            IChatbotPiiRedactionService piiRedactionService,
            IChatbotPromptBuilder promptBuilder,
            IChatbotKnowledgeService knowledgeService,
            IAuditService auditService,
            ILogger<OpenAiChatbotService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _settingService = settingService;
            _moderationService = moderationService;
            _piiRedactionService = piiRedactionService;
            _promptBuilder = promptBuilder;
            _knowledgeService = knowledgeService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ChatbotAskResponse> AskAsync(ClaimsPrincipal user, string message, string? sessionId = null, string? languageCode = null)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!await IsChatbotEnabledForUserAsync(user))
            {
                await LogUsageAuditAsync(userId, "CHATBOT_DISABLED_REQUEST", sessionId ?? string.Empty, $"MessageLength={message?.Length ?? 0}");
                return new ChatbotAskResponse
                {
                    SessionId = sessionId ?? string.Empty,
                    Answer = "The chatbot is currently disabled for your role. Please contact support.",
                    IsBlocked = true,
                    Reason = "ChatbotDisabled",
                    ProviderModel = "ConfigurationGuard"
                };
            }

            var moderation = _moderationService.Evaluate(message);
            var role = ResolveRole(user);
            var session = await GetOrCreateSessionAsync(sessionId, userId, role);
            session.PreferredLanguage = NormalizeLanguage(languageCode);
            await _context.SaveChangesAsync();

            var (isRateLimited, usageCount, usageLimit) = await CheckHourlyUsageLimitAsync(userId);
            if (isRateLimited)
            {
                const string rateLimitMessage = "You have reached the hourly chatbot usage limit. Please try again after one hour or contact support for urgent assistance.";
                await AddMessageAsync(session.Id, "Assistant", rateLimitMessage, "RateLimited", 0, "Safety");
                await AddEventAsync(session.Id, null, "RateLimitExceeded", "Warning", $"UserId={userId}; HourlyCount={usageCount}; Limit={usageLimit}");
                await LogUsageAuditAsync(userId, "CHATBOT_RATE_LIMITED", session.Id, $"HourlyCount={usageCount}; Limit={usageLimit}");

                return new ChatbotAskResponse
                {
                    SessionId = session.Id,
                    Answer = rateLimitMessage,
                    IsBlocked = true,
                    EscalationSuggested = false,
                    ConfidenceScore = 0.10m,
                    Reason = "RateLimitExceeded",
                    ProviderModel = "RateLimiter",
                    DetectedCategory = "Safety",
                    DetectedLanguage = session.PreferredLanguage
                };
            }

            var category = DetectCategory(message);

            await AddMessageAsync(session.Id, "User", message, moderation.IsBlocked ? "Blocked" : "Allowed", 0, category);
            await AddEventAsync(session.Id, null, "QuestionCategorized", "Info", $"Category={category}");

            if (moderation.IsBlocked)
            {
                await AddMessageAsync(session.Id, "Assistant", moderation.SafeResponse, "SafeFallback", 0, "Safety");
                await AddEventAsync(session.Id, null, "ModerationBlocked", "Warning", moderation.Reason);
                await LogUsageAuditAsync(userId, "CHATBOT_PROMPT_BLOCKED", session.Id, moderation.Reason);
                return new ChatbotAskResponse
                {
                    SessionId = session.Id,
                    Answer = moderation.SafeResponse,
                    IsBlocked = true,
                    EscalationSuggested = moderation.NeedsEmergencyEscalation,
                    ConfidenceScore = 0.10m,
                    Reason = moderation.Reason,
                    ProviderModel = "SafetyGuardrails",
                    DetectedCategory = category,
                    DetectedLanguage = session.PreferredLanguage
                };
            }

            var knowledgeContext = await _knowledgeService.RetrieveContextAsync(user, message, session.PreferredLanguage);
            var answer = await AskProviderAsync(user, message, knowledgeContext);
            answer = EnsureSourceLine(answer, knowledgeContext.Sources);
            var confidence = ComputeConfidence(answer, knowledgeContext.Sources.Count);
            var outputModeration = _moderationService.EvaluateOutput(answer, knowledgeContext.Sources.Count, confidence);
            if (outputModeration.IsBlocked)
            {
                await AddMessageAsync(session.Id, "Assistant", outputModeration.SafeResponse, "BlockedOutput", 0, "Safety");
                await AddEventAsync(session.Id, null, "OutputModerationBlocked", "Warning", outputModeration.Reason);
                await LogUsageAuditAsync(userId, "CHATBOT_OUTPUT_BLOCKED", session.Id, outputModeration.Reason);

                return new ChatbotAskResponse
                {
                    SessionId = session.Id,
                    Answer = outputModeration.SafeResponse,
                    IsBlocked = true,
                    EscalationSuggested = true,
                    ConfidenceScore = 0.10m,
                    Reason = outputModeration.Reason,
                    ProviderModel = "OutputSafetyGuardrails",
                    DetectedCategory = knowledgeContext.DetectedCategory,
                    DetectedLanguage = knowledgeContext.LanguageCode,
                    Sources = knowledgeContext.Sources
                };
            }

            var threshold = await GetDecimalSettingAsync("ChatbotUnresolvedThreshold", 0.45m);

            long? escalationId = null;
            var escalationEnabled = await GetBoolSettingAsync("ChatbotEnableEscalation", true);
            if (escalationEnabled && confidence <= threshold)
            {
                var escalation = await EscalateAsync(user, session.Id, null, "Low confidence response auto-escalation.");
                escalationId = escalation?.Id;
            }

            await AddMessageAsync(session.Id, "Assistant", answer, "Allowed", EstimateTokenCount(answer), knowledgeContext.DetectedCategory);

            if (confidence <= threshold)
            {
                session.IsUnresolved = true;
                await _context.SaveChangesAsync();
                await AddEventAsync(session.Id, null, "UnresolvedConversation", "Warning", $"Confidence={confidence:0.00}");
            }

            var providerModel = _configuration["OpenAI:Model"] ?? "FallbackTemplate";
            await LogUsageAuditAsync(
                userId,
                "CHATBOT_RESPONSE_SERVED",
                session.Id,
                $"Category={knowledgeContext.DetectedCategory}; Confidence={confidence:0.00}; Sources={knowledgeContext.Sources.Count}; HourlyCount={usageCount}; HourlyLimit={usageLimit}; Provider={providerModel}");

            return new ChatbotAskResponse
            {
                SessionId = session.Id,
                Answer = answer,
                EscalationSuggested = confidence <= threshold,
                ConfidenceScore = confidence,
                Sources = knowledgeContext.Sources,
                ProviderModel = providerModel,
                DetectedCategory = knowledgeContext.DetectedCategory,
                DetectedLanguage = knowledgeContext.LanguageCode,
                EscalationId = escalationId
            };
        }

        public async Task<IReadOnlyList<ChatMessage>> GetSessionMessagesAsync(string sessionId, ClaimsPrincipal user, int take = 30)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
                return Array.Empty<ChatMessage>();

            var ownsSession = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId);
            if (!ownsSession)
                return Array.Empty<ChatMessage>();

            return await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(take)
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<bool> SubmitFeedbackAsync(ClaimsPrincipal user, string sessionId, long? messageId, string feedbackType, string? comment = null)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            var ownsSession = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId);
            if (!ownsSession)
            {
                return false;
            }

            var normalizedType = string.Equals(feedbackType, "Helpful", StringComparison.OrdinalIgnoreCase)
                ? "Helpful"
                : "NotHelpful";

            _context.ChatFeedback.Add(new ChatFeedback
            {
                SessionId = sessionId,
                MessageId = messageId,
                FeedbackType = normalizedType,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });

            if (string.Equals(normalizedType, "NotHelpful", StringComparison.OrdinalIgnoreCase))
            {
                var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                if (session != null)
                {
                    session.IsUnresolved = true;
                }

                await AddEventAsync(sessionId, messageId, "NegativeFeedback", "Warning", "User marked response as not helpful.");
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsChatbotEnabledForUserAsync(ClaimsPrincipal user)
        {
            var featureToggles = await _settingService.GetFeatureTogglesAsync();
            var global = featureToggles.ChatbotEnabled;
            if (!global) return false;

            if (user.Identity?.IsAuthenticated != true)
            {
                return true;
            }

            if (user.IsInRole("Patient")) return await GetBoolSettingAsync("ChatbotEnabledForPatients", true);
            if (user.IsInRole("Admin") || user.IsInRole("SuperAdmin")) return await GetBoolSettingAsync("ChatbotEnabledForAdmins", true);
            return await GetBoolSettingAsync("ChatbotEnabledForStaff", true);
        }

        public async Task<ChatEscalation?> EscalateAsync(ClaimsPrincipal user, string sessionId, long? messageId, string reason, string escalationType = "Support")
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session == null)
            {
                return null;
            }

            var support = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == "ChatbotSupportContact");
            var escalation = new ChatEscalation
            {
                SessionId = sessionId,
                MessageId = messageId,
                UserId = userId,
                EscalationType = string.IsNullOrWhiteSpace(escalationType) ? "Support" : escalationType,
                Reason = reason,
                Status = "Pending",
                TargetContact = support?.Value ?? "support@hospital.com",
                CreatedAtUtc = DateTime.UtcNow
            };

            session.IsEscalated = true;
            _context.ChatEscalations.Add(escalation);
            await _context.SaveChangesAsync();
            await AddEventAsync(sessionId, messageId, "EscalationCreated", "Info", reason);
            return escalation;
        }

        public async Task<bool> MarkSessionUnresolvedAsync(ClaimsPrincipal user, string sessionId, string reason)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session == null)
            {
                return false;
            }

            session.IsUnresolved = true;
            await _context.SaveChangesAsync();
            await AddEventAsync(sessionId, null, "UnresolvedMarkedByUser", "Warning", reason);
            return true;
        }

        public async Task<IReadOnlyList<ChatEscalation>> GetEscalationsAsync(string status = "Pending", int take = 100)
        {
            var query = _context.ChatEscalations.AsNoTracking();
            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Status == status);
            }

            return await query
                .OrderByDescending(e => e.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> ResolveEscalationAsync(long escalationId, string targetContact, string resolverUserId)
        {
            var escalation = await _context.ChatEscalations.FirstOrDefaultAsync(e => e.Id == escalationId);
            if (escalation == null)
            {
                return false;
            }

            escalation.Status = "Resolved";
            escalation.TargetContact = targetContact;
            escalation.ResolvedByUserId = resolverUserId;
            escalation.ResolvedAtUtc = DateTime.UtcNow;

            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == escalation.SessionId);
            if (session != null)
            {
                session.IsEscalated = true;
                session.IsUnresolved = false;
            }

            await _context.SaveChangesAsync();
            await AddEventAsync(escalation.SessionId, escalation.MessageId, "EscalationResolved", "Info", $"Target={targetContact}");
            return true;
        }

        public async Task<ChatbotAnalyticsSnapshot> GetAnalyticsAsync(int days = 30)
        {
            var from = DateTime.UtcNow.AddDays(-Math.Max(1, days));

            var sessions = await _context.ChatSessions.AsNoTracking()
                .Where(s => s.StartedAtUtc >= from)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.Id).ToList();

            var messages = await _context.ChatMessages.AsNoTracking()
                .Where(m => sessionIds.Contains(m.SessionId))
                .ToListAsync();

            var escalations = await _context.ChatEscalations.AsNoTracking()
                .Where(e => e.CreatedAtUtc >= from)
                .ToListAsync();

            var unresolved = sessions.Count(s => s.IsUnresolved);
            var categoryCounts = messages
                .Where(m => !string.Equals(m.SenderType, "Assistant", StringComparison.OrdinalIgnoreCase))
                .GroupBy(m => string.IsNullOrWhiteSpace(m.Category) ? "General" : m.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalSessions = sessions.Count;
            return new ChatbotAnalyticsSnapshot
            {
                TotalSessions = totalSessions,
                TotalMessages = messages.Count,
                TotalEscalations = escalations.Count,
                UnresolvedSessions = unresolved,
                EscalationRate = totalSessions == 0 ? 0 : (decimal)escalations.Count / totalSessions,
                UnresolvedRate = totalSessions == 0 ? 0 : (decimal)unresolved / totalSessions,
                CategoryCounts = categoryCounts
            };
        }

        public async Task<ChatbotAdminSettings> GetAdminSettingsAsync()
        {
            var settings = await _context.Settings.AsNoTracking()
                .Where(s => s.Category == "Chatbot")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new ChatbotAdminSettings
            {
                Enabled = ParseBool(settings, "ChatbotEnabled", true),
                EnableEscalation = ParseBool(settings, "ChatbotEnableEscalation", true),
                EnableAppointmentGuidance = ParseBool(settings, "ChatbotEnableAppointmentGuidance", true),
                EnableBillingGuidance = ParseBool(settings, "ChatbotEnableBillingGuidance", true),
                EnableMultilingual = ParseBool(settings, "ChatbotEnableMultilingual", false),
                EnabledForPatients = ParseBool(settings, "ChatbotEnabledForPatients", true),
                EnabledForStaff = ParseBool(settings, "ChatbotEnabledForStaff", true),
                EnabledForAdmins = ParseBool(settings, "ChatbotEnabledForAdmins", true),
                Model = settings.GetValueOrDefault("ChatbotModel", "gpt-4o-mini"),
                Temperature = ParseDecimal(settings, "ChatbotTemperature", 0.2m),
                MaxTokens = ParseInt(settings, "ChatbotMaxTokens", 350),
                UnresolvedThreshold = ParseDecimal(settings, "ChatbotUnresolvedThreshold", 0.45m),
                HourlyUsageLimit = ParseInt(settings, "ChatbotHourlyUsageLimit", 100),
                SupportedLanguagesCsv = settings.GetValueOrDefault("ChatbotSupportedLanguages", "en"),
                DefaultLanguage = settings.GetValueOrDefault("ChatbotDefaultLanguage", "en"),
                RetentionDays = ParseInt(settings, "ChatbotRetentionDays", 90),
                EventLogRetentionDays = ParseInt(settings, "ChatbotEventLogRetentionDays", 30),
                EnablePiiRedaction = ParseBool(settings, "ChatbotEnablePiiRedaction", true),
                RedactionLevel = settings.GetValueOrDefault("ChatbotRedactionLevel", "Standard"),
                DeleteUnconsentedData = ParseBool(settings, "ChatbotDeleteUnconsentedData", true)
            };
        }

        public async Task<bool> UpdateAdminSettingsAsync(ChatbotAdminSettings settings, string modifiedByUserId)
        {
            var values = new Dictionary<string, string>
            {
                ["ChatbotEnabled"] = settings.Enabled.ToString().ToLowerInvariant(),
                ["ChatbotEnableEscalation"] = settings.EnableEscalation.ToString().ToLowerInvariant(),
                ["ChatbotEnableAppointmentGuidance"] = settings.EnableAppointmentGuidance.ToString().ToLowerInvariant(),
                ["ChatbotEnableBillingGuidance"] = settings.EnableBillingGuidance.ToString().ToLowerInvariant(),
                ["ChatbotEnableMultilingual"] = settings.EnableMultilingual.ToString().ToLowerInvariant(),
                ["ChatbotEnabledForPatients"] = settings.EnabledForPatients.ToString().ToLowerInvariant(),
                ["ChatbotEnabledForStaff"] = settings.EnabledForStaff.ToString().ToLowerInvariant(),
                ["ChatbotEnabledForAdmins"] = settings.EnabledForAdmins.ToString().ToLowerInvariant(),
                ["ChatbotModel"] = settings.Model,
                ["ChatbotTemperature"] = settings.Temperature.ToString("0.##"),
                ["ChatbotMaxTokens"] = settings.MaxTokens.ToString(),
                ["ChatbotUnresolvedThreshold"] = settings.UnresolvedThreshold.ToString("0.##"),
                ["ChatbotHourlyUsageLimit"] = settings.HourlyUsageLimit.ToString(),
                ["ChatbotSupportedLanguages"] = settings.SupportedLanguagesCsv,
                ["ChatbotDefaultLanguage"] = settings.DefaultLanguage,
                ["ChatbotRetentionDays"] = settings.RetentionDays.ToString(),
                ["ChatbotEventLogRetentionDays"] = settings.EventLogRetentionDays.ToString(),
                ["ChatbotEnablePiiRedaction"] = settings.EnablePiiRedaction.ToString().ToLowerInvariant(),
                ["ChatbotRedactionLevel"] = settings.RedactionLevel,
                ["ChatbotDeleteUnconsentedData"] = settings.DeleteUnconsentedData.ToString().ToLowerInvariant()
            };

            foreach (var entry in values)
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == entry.Key);
                if (setting == null)
                {
                    _context.Settings.Add(new Setting
                    {
                        Key = entry.Key,
                        Value = entry.Value,
                        Type = "string",
                        Category = "Chatbot",
                        Description = "Chatbot configuration setting",
                        IsSystem = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedBy = modifiedByUserId
                    });
                }
                else
                {
                    setting.Value = entry.Value;
                    setting.ModifiedDate = DateTime.UtcNow;
                    setting.ModifiedBy = modifiedByUserId;
                }
            }

            await _context.SaveChangesAsync();
            await AddEventAsync(null, null, "AdminSettingsUpdated", "Info", $"Updated by {modifiedByUserId}");
            return true;
        }

        private async Task<string> AskProviderAsync(ClaimsPrincipal user, string userInput, ChatKnowledgeContext context)
        {
            var enabled = bool.TryParse(_configuration["OpenAI:Enabled"], out var parsedEnabled) && parsedEnabled;
            var apiKey = _configuration["OpenAI:ApiKey"];
            var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com";
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            if (!enabled || string.IsNullOrWhiteSpace(apiKey))
            {
                return BuildFallbackAnswer(context);
            }

            var payload = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = _promptBuilder.BuildSystemPrompt(user, context) },
                    new { role = "user", content = userInput }
                },
                temperature = await GetDecimalSettingAsync("ChatbotTemperature", 0.2m),
                max_tokens = await GetIntSettingAsync("ChatbotMaxTokens", 350)
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var url = $"{baseUrl.TrimEnd('/')}/v1/chat/completions";
                var requestBody = JsonSerializer.Serialize(payload);
                using var response = await client.PostAsync(url, new StringContent(requestBody, Encoding.UTF8, "application/json"));
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAI request failed with status {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
                    await AddEventAsync(null, null, "ProviderError", "Warning", $"Status={(int)response.StatusCode}");
                    return BuildFallbackAnswer(context);
                }

                using var document = JsonDocument.Parse(responseBody);
                var content = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return string.IsNullOrWhiteSpace(content) ? BuildFallbackAnswer(context) : content.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI chat call failed. Falling back to safe local guidance.");
                await AddEventAsync(null, null, "ProviderException", "Warning", ex.Message);
                return BuildFallbackAnswer(context);
            }
        }

        private static string BuildFallbackAnswer(ChatKnowledgeContext context)
        {
            var topSources = context.Sources
                .Take(3)
                .Select(s => s.SourceName)
                .DefaultIfEmpty("General Support Guidance");

            return "I can help with MedyxHMS navigation, appointment guidance, billing guidance, and support contacts. "
                + "The live AI provider is not configured right now, so this is a safe fallback response. "
                + "Please ask module-oriented questions such as: where to book appointments, where to view billing, or where to find reports. "
                + $"Sources: {string.Join("; ", topSources)}.";
        }

        private async Task<ChatSession> GetOrCreateSessionAsync(string? sessionId, string? userId, string role)
        {
            if (!string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(userId))
            {
                var existing = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.Status == "Active");
                if (existing != null)
                    return existing;
            }

            var session = new ChatSession
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                UserRole = role,
                Status = "Active",
                Channel = "Web",
                StartedAtUtc = DateTime.UtcNow,
                PreferredLanguage = "en"
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        private async Task AddMessageAsync(string sessionId, string senderType, string content, string moderationStatus, int tokenCount, string category)
        {
            _context.ChatMessages.Add(new ChatMessage
            {
                SessionId = sessionId,
                SenderType = senderType,
                Content = content,
                ModerationStatus = moderationStatus,
                TokenCount = tokenCount,
                Category = category,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private async Task AddEventAsync(string? sessionId, long? messageId, string eventType, string severity, string details)
        {
            var piiRedactionEnabled = await GetBoolSettingAsync("ChatbotEnablePiiRedaction", true);
            var redactionLevel = await GetStringSettingAsync("ChatbotRedactionLevel", "Standard");

            var sanitizedDetails = piiRedactionEnabled
                ? _piiRedactionService.RedactEventDetails(details, eventType, redactionLevel)
                : details;

            _context.ChatbotEventLogs.Add(new ChatbotEventLog
            {
                SessionId = sessionId,
                MessageId = messageId,
                EventType = eventType,
                Severity = severity,
                Details = sanitizedDetails,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private static int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Math.Max(1, text.Length / 4);
        }

        private static string EnsureSourceLine(string answer, IReadOnlyList<ChatKnowledgeSource> sources)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return answer;
            }

            if (answer.Contains("Sources:", StringComparison.OrdinalIgnoreCase))
            {
                return answer;
            }

            var sourceLine = sources.Count == 0
                ? "Sources: General Support Guidance"
                : $"Sources: {string.Join("; ", sources.Take(4).Select(s => s.SourceName))}";

            return $"{answer.Trim()}\n\n{sourceLine}";
        }

        private static decimal ComputeConfidence(string answer, int sourceCount)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return 0.15m;
            }

            var lowSignal = answer.Contains("not enough verified", StringComparison.OrdinalIgnoreCase)
                || answer.Contains("do not have enough", StringComparison.OrdinalIgnoreCase)
                || answer.Contains("fallback", StringComparison.OrdinalIgnoreCase);

            if (lowSignal)
            {
                return sourceCount > 0 ? 0.40m : 0.25m;
            }

            if (sourceCount >= 4) return 0.82m;
            if (sourceCount >= 2) return 0.72m;
            if (sourceCount == 1) return 0.58m;
            return 0.42m;
        }

        private static string ResolveRole(ClaimsPrincipal user)
        {
            if (user.IsInRole("SuperAdmin")) return "SuperAdmin";
            if (user.IsInRole("Admin")) return "Admin";
            if (user.IsInRole("Doctor")) return "Doctor";
            if (user.IsInRole("Nurse")) return "Nurse";
            if (user.IsInRole("Patient")) return "Patient";
            return "Staff";
        }

        private static string DetectCategory(string message)
        {
            var lower = message.ToLowerInvariant();
            if (lower.Contains("appointment") || lower.Contains("book") || lower.Contains("schedule")) return "Appointment";
            if (lower.Contains("bill") || lower.Contains("invoice") || lower.Contains("payment")) return "Billing";
            if (lower.Contains("support") || lower.Contains("contact") || lower.Contains("handoff")) return "Support";
            return "Navigation";
        }

        private static string NormalizeLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return "en";
            var lang = languageCode.Trim().ToLowerInvariant();
            return lang.Length > 5 ? "en" : lang;
        }

        private async Task<bool> GetBoolSettingAsync(string key, bool fallback)
        {
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return fallback;
            return bool.TryParse(setting.Value, out var parsed) ? parsed : fallback;
        }

        private async Task<decimal> GetDecimalSettingAsync(string key, decimal fallback)
        {
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return fallback;
            return decimal.TryParse(setting.Value, out var parsed) ? parsed : fallback;
        }

        private async Task<int> GetIntSettingAsync(string key, int fallback)
        {
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return fallback;
            return int.TryParse(setting.Value, out var parsed) ? parsed : fallback;
        }

        private async Task<string> GetStringSettingAsync(string key, string fallback)
        {
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return fallback;

            var value = setting.Value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private async Task<(bool IsRateLimited, int UsageCount, int UsageLimit)> CheckHourlyUsageLimitAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (false, 0, 0);
            }

            var usageLimit = await GetIntSettingAsync("ChatbotHourlyUsageLimit", 100);
            if (usageLimit <= 0)
            {
                return (false, 0, usageLimit);
            }

            var windowStartUtc = DateTime.UtcNow.AddHours(-1);

            var usageCount = await _context.ChatMessages
                .AsNoTracking()
                .Join(
                    _context.ChatSessions.AsNoTracking(),
                    m => m.SessionId,
                    s => s.Id,
                    (m, s) => new { Message = m, Session = s })
                .Where(x => x.Session.UserId == userId
                    && x.Message.SenderType == "User"
                    && x.Message.CreatedAtUtc >= windowStartUtc)
                .CountAsync();

            return (usageCount >= usageLimit, usageCount, usageLimit);
        }

        private static bool ParseBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
        {
            return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        {
            return values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private static decimal ParseDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
        {
            return values.TryGetValue(key, out var value) && decimal.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private async Task LogUsageAuditAsync(string? userId, string action, string sessionId, string details)
        {
            try
            {
                await _auditService.LogActivityAsync(userId, action, "ChatSession", string.IsNullOrWhiteSpace(sessionId) ? "N/A" : sessionId, null, details);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write chatbot usage audit action {Action}", action);
            }
        }
    }
}
