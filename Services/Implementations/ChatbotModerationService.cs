using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

// Purpose: Contains application code for ChatbotModerationService and its related runtime behavior.
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

        private static readonly string[] PromptInjectionTerms =
        {
            "ignore previous instructions",
            "ignore all instructions",
            "you are now",
            "system prompt",
            "developer message",
            "disable safety",
            "act as a licensed physician",
            "jailbreak"
        };

        private static readonly string[] UnsafeOutputTerms =
        {
            "i diagnose",
            "your diagnosis",
            "take this dosage",
            "prescribe",
            "definitely has"
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

            if (ContainsAny(text, PromptInjectionTerms))
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    Reason = "PromptInjectionAttempt",
                    SafeResponse = "I cannot follow instruction-overrides or hidden prompt requests. I can only help with approved hospital navigation and support topics."
                };
            }

            return new ChatModerationResult
            {
                IsBlocked = false,
                Reason = "Allowed"
            };
        }

        public ChatModerationResult EvaluateOutput(string output, int sourceCount, decimal confidenceScore)
        {
            var text = (output ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    Reason = "EmptyOutput",
                    SafeResponse = "I could not prepare a safe response right now. Please try again or contact support."
                };
            }

            if (confidenceScore < 0.40m || sourceCount <= 0)
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    Reason = "LowConfidenceOutput",
                    SafeResponse = "I cannot provide a reliable answer right now. Please contact hospital support for verified guidance."
                };
            }

            if (ContainsAny(text, UnsafeOutputTerms))
            {
                return new ChatModerationResult
                {
                    IsBlocked = true,
                    Reason = "UnsafeMedicalOutput",
                    SafeResponse = "I cannot provide diagnosis or treatment advice. Please consult a licensed clinician."
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
