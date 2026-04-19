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

        public List<ChatKnowledgeSource> Sources { get; set; } = new List<ChatKnowledgeSource>();

        public string? ErrorMessage { get; set; }

        public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
    }

    public class ChatbotAskRequestViewModel
    {
        public string? SessionId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Prompt { get; set; } = string.Empty;
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
}
