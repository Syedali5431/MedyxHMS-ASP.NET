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

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public ICollection<ChatFeedback> FeedbackItems { get; set; } = new List<ChatFeedback>();
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

        public ChatSession? Session { get; set; }
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

        public string Reason { get; set; } = string.Empty;

        public string ProviderModel { get; set; } = string.Empty;
    }
}
