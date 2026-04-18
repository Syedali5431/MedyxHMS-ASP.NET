using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class ChatbotPageViewModel
    {
        public string? SessionId { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public string LastAnswer { get; set; } = string.Empty;

        public bool EscalationSuggested { get; set; }

        public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
    }
}
