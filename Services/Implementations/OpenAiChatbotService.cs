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
        private readonly IChatbotModerationService _moderationService;
        private readonly IChatbotPromptBuilder _promptBuilder;
        private readonly IChatbotKnowledgeService _knowledgeService;
        private readonly ILogger<OpenAiChatbotService> _logger;

        public OpenAiChatbotService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IChatbotModerationService moderationService,
            IChatbotPromptBuilder promptBuilder,
            IChatbotKnowledgeService knowledgeService,
            ILogger<OpenAiChatbotService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _moderationService = moderationService;
            _promptBuilder = promptBuilder;
            _knowledgeService = knowledgeService;
            _logger = logger;
        }

        public async Task<ChatbotAskResponse> AskAsync(ClaimsPrincipal user, string message, string? sessionId = null)
        {
            var moderation = _moderationService.Evaluate(message);
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = ResolveRole(user);
            var session = await GetOrCreateSessionAsync(sessionId, userId, role);

            await AddMessageAsync(session.Id, "User", message, moderation.IsBlocked ? "Blocked" : "Allowed", 0);

            if (moderation.IsBlocked)
            {
                await AddMessageAsync(session.Id, "Assistant", moderation.SafeResponse, "SafeFallback", 0);
                return new ChatbotAskResponse
                {
                    SessionId = session.Id,
                    Answer = moderation.SafeResponse,
                    IsBlocked = true,
                    EscalationSuggested = moderation.NeedsEmergencyEscalation,
                    ConfidenceScore = 0.10m,
                    Reason = moderation.Reason,
                    ProviderModel = "SafetyGuardrails"
                };
            }

            var knowledgeContext = await _knowledgeService.RetrieveContextAsync(user, message);
            var answer = await AskProviderAsync(user, message, knowledgeContext);
            answer = EnsureSourceLine(answer, knowledgeContext.Sources);
            var confidence = ComputeConfidence(answer, knowledgeContext.Sources.Count);

            await AddMessageAsync(session.Id, "Assistant", answer, "Allowed", EstimateTokenCount(answer));

            return new ChatbotAskResponse
            {
                SessionId = session.Id,
                Answer = answer,
                EscalationSuggested = confidence <= 0.45m,
                ConfidenceScore = confidence,
                Sources = knowledgeContext.Sources,
                ProviderModel = _configuration["OpenAI:Model"] ?? "FallbackTemplate"
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

            await _context.SaveChangesAsync();
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
                temperature = 0.2,
                max_tokens = 350
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
                StartedAtUtc = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        private async Task AddMessageAsync(string sessionId, string senderType, string content, string moderationStatus, int tokenCount)
        {
            _context.ChatMessages.Add(new ChatMessage
            {
                SessionId = sessionId,
                SenderType = senderType,
                Content = content,
                ModerationStatus = moderationStatus,
                TokenCount = tokenCount,
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
    }
}
