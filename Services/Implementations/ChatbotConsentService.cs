using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

// Purpose: Contains application code for ChatbotConsentService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Service for managing chatbot user consent tracking and audit compliance.
    /// Supports GDPR, consent versioning, and comprehensive audit trails.
    /// </summary>
    public class ChatbotConsentService : IChatbotConsentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ChatbotConsentService> _logger;

        // Current consent version - increment when terms change
        private const string CURRENT_CONSENT_VERSION = "1.0";

        public ChatbotConsentService(ApplicationDbContext dbContext, ILogger<ChatbotConsentService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get the current (active) consent record for a user.
        /// </summary>
        public async Task<ChatbotConsent?> GetCurrentConsentAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                // For anonymous users, always return null (no consent tracked)
                return null;
            }

            var consent = await _dbContext.ChatbotConsents
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.ConsentedAtUtc)
                .FirstOrDefaultAsync();

            return consent;
        }

        /// <summary>
        /// Check if user has active, valid consent for chatbot use.
        /// </summary>
        public async Task<bool> HasActiveConsentAsync(string? userId)
        {
            var consent = await GetCurrentConsentAsync(userId);
            
            if (consent == null)
            {
                return false;
            }

            // Check if consent has been revoked
            if (!consent.IsActive || consent.RevokedAtUtc.HasValue)
            {
                return false;
            }

            // For additional safety: if user never explicitly accepted AI processing, deny
            if (!consent.ConsentedToAiProcessing)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Record user acceptance of consent terms.
        /// </summary>
        public async Task<ChatbotConsent> AcceptConsentAsync(string? userId, bool aiProcessing, bool dataRetention,
            bool thirdPartyProcessing, string consentVersion, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                // Revoke any previous consent for this user
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var previousConsents = await _dbContext.ChatbotConsents
                        .Where(c => c.UserId == userId && c.IsActive)
                        .ToListAsync();

                    foreach (var prev in previousConsents)
                    {
                        prev.IsActive = false;
                        prev.RevokedAtUtc = DateTime.UtcNow;
                        prev.RevocationReason = "Superseded by new consent acceptance";
                    }
                }

                // Create new consent record
                var consent = new ChatbotConsent
                {
                    UserId = userId,
                    ConsentVersion = consentVersion,
                    ConsentedToAiProcessing = aiProcessing,
                    ConsentedToDataRetention = dataRetention,
                    ConsentedToThirdPartyProcessing = thirdPartyProcessing,
                    UserIpAddress = ipAddress,
                    UserAgent = userAgent,
                    ConsentedAtUtc = DateTime.UtcNow,
                    IsActive = true
                };

                _dbContext.ChatbotConsents.Add(consent);

                // Log audit trail
                var auditState = new
                {
                    aiProcessing,
                    dataRetention,
                    thirdPartyProcessing
                };

                var audit = new ChatbotConsentAudit
                {
                    ConsentId = null, // Will be set after SaveChangesAsync
                    UserId = userId,
                    Action = "Accepted",
                    ConsentVersion = consentVersion,
                    ConsentStateJson = JsonSerializer.Serialize(auditState),
                    UserIpAddress = ipAddress,
                    UserAgent = userAgent
                };

                _dbContext.ChatbotConsentAudits.Add(audit);
                await _dbContext.SaveChangesAsync();

                // Update audit record with consent ID
                audit.ConsentId = consent.Id;
                _dbContext.ChatbotConsentAudits.Update(audit);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Consent accepted for user {userId} (version {consentVersion})");
                return consent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting consent for user {userId}");
                throw;
            }
        }

        /// <summary>
        /// Record user rejection of consent terms (prevents chatbot use).
        /// </summary>
        public async Task<ChatbotConsentAudit> RejectConsentAsync(string? userId, string consentVersion,
            string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var auditState = new
                {
                    aiProcessing = false,
                    dataRetention = false,
                    thirdPartyProcessing = false
                };

                var audit = new ChatbotConsentAudit
                {
                    ConsentId = null,
                    UserId = userId,
                    Action = "Rejected",
                    ConsentVersion = consentVersion,
                    ConsentStateJson = JsonSerializer.Serialize(auditState),
                    UserIpAddress = ipAddress,
                    UserAgent = userAgent
                };

                _dbContext.ChatbotConsentAudits.Add(audit);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Consent rejected for user {userId} (version {consentVersion})");
                return audit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting consent for user {userId}");
                throw;
            }
        }

        /// <summary>
        /// Revoke existing consent for a user (e.g., at user request or for compliance).
        /// </summary>
        public async Task<bool> RevokeConsentAsync(string? userId, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            try
            {
                var activeConsents = await _dbContext.ChatbotConsents
                    .Where(c => c.UserId == userId && c.IsActive)
                    .ToListAsync();

                if (!activeConsents.Any())
                {
                    _logger.LogWarning($"No active consent found to revoke for user {userId}");
                    return false;
                }

                foreach (var consent in activeConsents)
                {
                    consent.IsActive = false;
                    consent.RevokedAtUtc = DateTime.UtcNow;
                    consent.RevocationReason = reason ?? "User-initiated revocation";
                }

                // Log revocation in audit trail
                var audit = new ChatbotConsentAudit
                {
                    UserId = userId,
                    Action = "Revoked",
                    ConsentVersion = activeConsents.First().ConsentVersion,
                    ConsentStateJson = "{}",
                    Notes = reason
                };

                _dbContext.ChatbotConsentAudits.Add(audit);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Consent revoked for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking consent for user {userId}");
                throw;
            }
        }

        /// <summary>
        /// Renew consent with the latest version.
        /// </summary>
        public async Task<ChatbotConsent> RenewConsentAsync(string? userId, bool aiProcessing, bool dataRetention,
            bool thirdPartyProcessing, string? ipAddress = null, string? userAgent = null)
        {
            // Use AcceptConsentAsync with current version - it handles superseding previous consent
            return await AcceptConsentAsync(userId, aiProcessing, dataRetention, thirdPartyProcessing,
                CURRENT_CONSENT_VERSION, ipAddress, userAgent);
        }

        /// <summary>
        /// Get complete audit trail for consent actions by a user.
        /// </summary>
        public async Task<IReadOnlyList<ChatbotConsentAudit>> GetConsentAuditAsync(string? userId, int take = 50)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Array.Empty<ChatbotConsentAudit>();
            }

            var audits = await _dbContext.ChatbotConsentAudits
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(take)
                .ToListAsync();

            return audits.AsReadOnly();
        }

        /// <summary>
        /// Get consent terms/text for display to user.
        /// </summary>
        public async Task<string> GetConsentTermsAsync(string version)
        {
            // For now, return hardcoded terms. In production, store in database or external file.
            var terms = version switch
            {
                "1.0" => @"
# MedyxHMS Chatbot - Consent & Privacy Terms (v1.0)

## What is the MedyxHMS Chatbot?
The MedyxHMS Chatbot is an AI-powered assistant that helps you navigate the hospital's services, find information about appointments, billing, policies, and connect with support.

## AI Processing & OpenAI
This chatbot is powered by **OpenAI's GPT model**. When you ask a question:
- Your message is sent to OpenAI's servers for processing
- OpenAI may retain your data according to their Privacy Policy: https://openai.com/privacy
- We retain conversation history for compliance and improvement purposes

## Important Limitations
âš ï¸ **The chatbot cannot provide medical advice, diagnosis, or treatment recommendations.**
- Emergency issues are escalated to hospital staff
- For medical concerns, please contact a licensed clinician

## Your Data & Privacy
- âœ“ Your consent is stored securely
- âœ“ You can request your transcript history
- âœ“ You can revoke consent at any time
- âœ“ We comply with healthcare privacy regulations

## By accepting, you agree to:
1. **AI Processing**: Your messages will be processed by OpenAI's models
2. **Data Retention**: We keep transcripts for up to 30 days for history and compliance
3. **Third-Party Processing**: OpenAI processes your data per their Privacy Policy

---
Last Updated: April 2026
",
                _ => "Unknown consent version"
            };

            return await Task.FromResult(terms);
        }

        /// <summary>
        /// Check if user must provide or renew consent (based on version/policy changes).
        /// </summary>
        public async Task<bool> RequiresConsentRenewalAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                // Anonymous users are not prompted for consent
                return false;
            }

            var currentConsent = await GetCurrentConsentAsync(userId);

            if (currentConsent == null)
            {
                // No consent on record - requires initial consent
                return true;
            }

            if (!currentConsent.IsActive || currentConsent.RevokedAtUtc.HasValue)
            {
                // Consent was revoked - requires renewal
                return true;
            }

            if (currentConsent.ConsentVersion != CURRENT_CONSENT_VERSION)
            {
                // Terms have been updated - requires renewal
                return true;
            }

            // Consent is current and active
            return false;
        }
    }
}
