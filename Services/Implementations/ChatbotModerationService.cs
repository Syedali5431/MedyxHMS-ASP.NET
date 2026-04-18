using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class ChatbotModerationService : IChatbotModerationService
    {
        private static readonly string[] EmergencyTerms =
        {
            "chest pain", "can\u2019t breathe", "cant breathe", "suicid", "stroke", "unconscious", "bleeding heavily", "overdose"
        };

        private static readonly string[] UnsafeMedicalTerms =
        {
            "diagnose", "prescribe", "dosage", "what medicine should i take", "treatment plan", "antibiotic for"
        };

        public ChatModerationResult Evaluate(string input)
        {
            var text = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    Reason = "EmptyInput",
                    SafeResponse = "Please enter a question so I can help with navigation or support information."
                };
            }

            if (ContainsAny(text, EmergencyTerms))
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    NeedsEmergencyEscalation = true,
                    Reason = "EmergencyEscalation",
                    SafeResponse = "This may be an emergency. Please contact emergency services immediately and speak to a licensed clinician."
                };
            }

            if (ContainsAny(text, UnsafeMedicalTerms))
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    Reason = "UnsafeMedicalAdviceRequest",
                    SafeResponse = "I can help with hospital workflows and support information, but I cannot provide diagnosis or treatment advice. Please contact a licensed clinician."
                };
            }

            return new ChatModerationResult
            {
                IsBlocked = false,
                Reason = "Allowed"
            };
        }

        private static bool ContainsAny(string input, IEnumerable<string> terms)
        {
            return terms.Any(term => input.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }
}
