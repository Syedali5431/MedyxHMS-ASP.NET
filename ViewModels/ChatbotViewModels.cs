using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for ChatbotViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    public class ChatbotPageViewModel
    {
        public string? SessionId { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public string LastAnswer { get; set; } = string.Empty;

        public bool EscalationSuggested { get; set; }

        public decimal ConfidenceScore { get; set; }

        public string LanguageCode { get; set; } = "en";

        public long? EscalationId { get; set; }

        public string DetectedCategory { get; set; } = "General";

        public List<ChatKnowledgeSource> Sources { get; set; } = new List<ChatKnowledgeSource>();

        public string? ErrorMessage { get; set; }

        public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
    }

    public class ChatbotAskRequestViewModel
    {
        public string? SessionId { get; set; }

        [MaxLength(12)]
        public string? LanguageCode { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Prompt { get; set; } = string.Empty;
    }

    public class ChatbotEscalationRequestViewModel
    {
        [Required]
        [MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        public long? MessageId { get; set; }

        [Required]
        [MaxLength(1200)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(30)]
        public string EscalationType { get; set; } = "Support";
    }

    public class ChatbotUnresolvedRequestViewModel
    {
        [Required]
        [MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(1200)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ChatbotFeedbackRequestViewModel
    {
        [Required]
        [MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        public long? MessageId { get; set; }

        [Required]
        [RegularExpression("^(Helpful|NotHelpful)$")]
        public string FeedbackType { get; set; } = "Helpful";

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class ChatbotAdminSettingsViewModel
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

    public class ChatbotAdminAnalyticsViewModel
    {
        public ChatbotAnalyticsSnapshot Snapshot { get; set; } = new ChatbotAnalyticsSnapshot();
        public int Days { get; set; } = 30;
    }

    public class ChatbotEscalationsViewModel
    {
        public string Status { get; set; } = "Pending";
        public List<ChatEscalation> Items { get; set; } = new List<ChatEscalation>();
    }

    /// <summary>
    /// ViewModel for displaying consent request form to users.
    /// </summary>
    public class ChatbotConsentViewModel
    {
        /// <summary>Full text of consent terms to display</summary>
        public string ConsentTerms { get; set; } = string.Empty;

        /// <summary>Version of consent terms</summary>
        public string ConsentVersion { get; set; } = "1.0";

        /// <summary>Whether this is a consent renewal (vs. initial)</summary>
        public bool IsRenewal { get; set; } = false;

        /// <summary>User's current consent status (if any)</summary>
        public ChatbotConsent? CurrentConsent { get; set; }
    }

    /// <summary>
    /// ViewModel for accepting consent terms.
    /// </summary>
    public class ChatbotConsentAcceptViewModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "AI processing consent is required.")]
        public bool ConsentedToAiProcessing { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Data retention consent is required.")]
        public bool ConsentedToDataRetention { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Third-party processing consent is required.")]
        public bool ConsentedToThirdPartyProcessing { get; set; }

        public string ConsentVersion { get; set; } = "1.0";
    }
}
