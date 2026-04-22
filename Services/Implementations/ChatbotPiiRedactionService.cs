using System.Text.RegularExpressions;
using MedyxHMS.Services.Interfaces;

// Purpose: Contains application code for ChatbotPiiRedactionService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ChatbotPiiRedactionService : IChatbotPiiRedactionService
    {
        private static readonly Regex EmailRegex = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new(@"(?<!\d)(?:\+?\d[\d\s\-\(\)]{8,}\d)(?!\d)", RegexOptions.Compiled);
        private static readonly Regex UuidRegex = new(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
        private static readonly Regex LongNumericIdRegex = new(@"(?<!\d)\d{8,}(?!\d)", RegexOptions.Compiled);

        public string RedactEventDetails(string details, string eventType, string redactionLevel)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                return string.Empty;
            }

            var normalizedLevel = NormalizeLevel(redactionLevel);
            var output = details;

            output = EmailRegex.Replace(output, "[EMAIL_REDACTED]");
            output = PhoneRegex.Replace(output, "[PHONE_REDACTED]");

            if (!string.Equals(normalizedLevel, "Minimal", StringComparison.OrdinalIgnoreCase))
            {
                output = UuidRegex.Replace(output, "[ID_REDACTED]");
                output = LongNumericIdRegex.Replace(output, "[NUMERIC_ID_REDACTED]");
                output = RedactKnownKeyValues(output);
            }

            if (string.Equals(normalizedLevel, "Strict", StringComparison.OrdinalIgnoreCase))
            {
                output = EnforceStrictTruncation(output);
            }

            return output;
        }

        private static string NormalizeLevel(string? redactionLevel)
        {
            if (string.IsNullOrWhiteSpace(redactionLevel))
            {
                return "Standard";
            }

            return redactionLevel.Trim() switch
            {
                "Minimal" => "Minimal",
                "Strict" => "Strict",
                _ => "Standard"
            };
        }

        private static string RedactKnownKeyValues(string input)
        {
            var output = input;
            output = Regex.Replace(output, @"UserId\s*=\s*[^;\s]+", "UserId=[ID_REDACTED]", RegexOptions.IgnoreCase);
            output = Regex.Replace(output, @"Target\s*=\s*[^;\s]+", "Target=[CONTACT_REDACTED]", RegexOptions.IgnoreCase);
            output = Regex.Replace(output, @"Updated by\s+[^;\s]+", "Updated by [ID_REDACTED]", RegexOptions.IgnoreCase);
            return output;
        }

        private static string EnforceStrictTruncation(string input)
        {
            const int maxLength = 240;
            if (input.Length <= maxLength)
            {
                return input;
            }

            return input[..maxLength] + "...[TRUNCATED]";
        }
    }
}
