using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

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
}
