using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.Models
{
    public class ChatSession
    {
        [Key, MaxLength(64)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(40)]
        public string UserRole { get; set; } = "Guest";

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAtUtc { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        [MaxLength(20)]
        public string Channel { get; set; } = "Web";

        public bool IsEscalated { get; set; }

        public bool IsUnresolved { get; set; }

        [MaxLength(12)]
        public string PreferredLanguage { get; set; } = "en";

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public ICollection<ChatFeedback> FeedbackItems { get; set; } = new List<ChatFeedback>();

        public ICollection<ChatEscalation> Escalations { get; set; } = new List<ChatEscalation>();
    }

    public class ChatMessage
    {
        public long Id { get; set; }

        [Required, MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string SenderType { get; set; } = string.Empty; // User | Assistant | System

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(30)]
        public string ModerationStatus { get; set; } = "Unchecked";

        public int TokenCount { get; set; }

        [MaxLength(30)]
        public string Category { get; set; } = "General";

        public ChatSession? Session { get; set; }
    }

    public class ChatEscalation
    {
        public long Id { get; set; }

        [Required, MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        public long? MessageId { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [Required, MaxLength(30)]
        public string EscalationType { get; set; } = "Support";

        [Required, MaxLength(1200)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        [MaxLength(200)]
        public string? TargetContact { get; set; }

        [MaxLength(450)]
        public string? ResolvedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAtUtc { get; set; }

        public ChatSession? Session { get; set; }

        public ChatMessage? Message { get; set; }
    }

    public class ChatbotEventLog
    {
        public long Id { get; set; }

        [MaxLength(64)]
        public string? SessionId { get; set; }

        public long? MessageId { get; set; }

        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Severity { get; set; } = "Info";

        [Required, MaxLength(2000)]
        public string Details { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class ChatFeedback
    {
        public long Id { get; set; }

        [Required, MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        public long? MessageId { get; set; }

        [Required, MaxLength(20)]
        public string FeedbackType { get; set; } = string.Empty; // Helpful | NotHelpful

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ChatSession? Session { get; set; }

        public ChatMessage? Message { get; set; }
    }

    public class ChatModerationResult
    {
        public bool IsBlocked { get; set; }

        public bool NeedsEmergencyEscalation { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string SafeResponse { get; set; } = string.Empty;
    }

    public class ChatbotAskResponse
    {
        public string SessionId { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public bool IsBlocked { get; set; }

        public bool EscalationSuggested { get; set; }

        public decimal ConfidenceScore { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string ProviderModel { get; set; } = string.Empty;

        public string DetectedCategory { get; set; } = "General";

        public string DetectedLanguage { get; set; } = "en";

        public long? EscalationId { get; set; }

        public List<ChatKnowledgeSource> Sources { get; set; } = new List<ChatKnowledgeSource>();
    }

    public class ChatbotAdminSettings
    {
        public bool Enabled { get; set; }
        public bool EnableEscalation { get; set; }
        public bool EnableAppointmentGuidance { get; set; }
        public bool EnableBillingGuidance { get; set; }
        public bool EnableMultilingual { get; set; }
        public bool EnabledForPatients { get; set; }
        public bool EnabledForStaff { get; set; }
        public bool EnabledForAdmins { get; set; }
        public string Model { get; set; } = "gpt-4o-mini";
        public decimal Temperature { get; set; } = 0.2m;
        public int MaxTokens { get; set; } = 350;
        public decimal UnresolvedThreshold { get; set; } = 0.45m;
        public int HourlyUsageLimit { get; set; } = 100;
        public string SupportedLanguagesCsv { get; set; } = "en";
        public string DefaultLanguage { get; set; } = "en";
        public int RetentionDays { get; set; } = 90;
        public int EventLogRetentionDays { get; set; } = 30;
        public bool EnablePiiRedaction { get; set; } = true;
        public string RedactionLevel { get; set; } = "Standard";
        public bool DeleteUnconsentedData { get; set; } = true;
    }

    public class ChatbotAnalyticsSnapshot
    {
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public int TotalEscalations { get; set; }
        public int UnresolvedSessions { get; set; }
        public decimal EscalationRate { get; set; }
        public decimal UnresolvedRate { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
    }

    public class ChatKnowledgeContext
    {
        public string SystemContext { get; set; } = string.Empty;

        public string DetectedCategory { get; set; } = "General";

        public string LanguageCode { get; set; } = "en";

        public List<ChatKnowledgeSource> Sources { get; set; } = new List<ChatKnowledgeSource>();
    }

    public class ChatKnowledgeSource
    {
        public string SourceType { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string Excerpt { get; set; } = string.Empty;
    }

    public class ChatbotCleanupResult
    {
        public string Category { get; set; } = string.Empty;

        public int DeletedCount { get; set; }

        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Tracks user consent for AI-powered chatbot functionality.
    /// Supports GDPR compliance, audit trails, and version tracking.
    /// </summary>
    public class ChatbotConsent
    {
        public long Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        /// <summary>Version of consent terms accepted (e.g., "1.0", "2.0")</summary>
        [Required, MaxLength(10)]
        public string ConsentVersion { get; set; } = "1.0";

        /// <summary>Whether user explicitly accepted AI processing</summary>
        public bool ConsentedToAiProcessing { get; set; }

        /// <summary>Whether user consented to data retention (for transcript history)</summary>
        public bool ConsentedToDataRetention { get; set; }

        /// <summary>Whether user consented to OpenAI API processing</summary>
        public bool ConsentedToThirdPartyProcessing { get; set; }

        /// <summary>User IP address at time of consent</summary>
        [MaxLength(50)]
        public string? UserIpAddress { get; set; }

        /// <summary>User agent/browser info at time of consent</summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>Timestamp when consent was given</summary>
        public DateTime ConsentedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Timestamp when consent was revoked (if applicable)</summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>Whether consent is currently active</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Optional reason for revocation</summary>
        [MaxLength(500)]
        public string? RevocationReason { get; set; }
    }

    /// <summary>
    /// Audit trail for consent modifications and rejections.
    /// Supports compliance audits and user dispute resolution.
    /// </summary>
    public class ChatbotConsentAudit
    {
        public long Id { get; set; }

        public long? ConsentId { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [Required, MaxLength(30)]
        public string Action { get; set; } = string.Empty; // Accepted | Rejected | Revoked | Renewed

        [Required, MaxLength(10)]
        public string ConsentVersion { get; set; } = "1.0";

        /// <summary>JSON payload of consent flags at time of action</summary>
        [Required]
        public string ConsentStateJson { get; set; } = "{}";

        [MaxLength(500)]
        public string? UserIpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
