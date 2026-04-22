using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

// Purpose: Contains application code for SmsNotificationProviderRouter and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class SmsNotificationProviderRouter : ISmsNotificationProvider
    {
        private readonly ISettingService _settingService;
        private readonly IConfiguration _configuration;
        private readonly INotificationDeliveryAuditService _notificationDeliveryAuditService;
        private readonly TwilioSmsNotificationProvider _twilioProvider;
        private readonly AfricaTalkingSmsNotificationProvider _africaTalkingProvider;
        private readonly ILogger<SmsNotificationProviderRouter> _logger;

        public SmsNotificationProviderRouter(
            ISettingService settingService,
            IConfiguration configuration,
            INotificationDeliveryAuditService notificationDeliveryAuditService,
            TwilioSmsNotificationProvider twilioProvider,
            AfricaTalkingSmsNotificationProvider africaTalkingProvider,
            ILogger<SmsNotificationProviderRouter> logger)
        {
            _settingService = settingService;
            _configuration = configuration;
            _notificationDeliveryAuditService = notificationDeliveryAuditService;
            _twilioProvider = twilioProvider;
            _africaTalkingProvider = africaTalkingProvider;
            _logger = logger;
        }

        public async Task SendAsync(string toPhone, string message)
        {
            if (await IsPhoneOptedOutAsync(toPhone))
            {
                _logger.LogInformation("SMS skipped due to recipient opt-out. To={Recipient}", toPhone);
                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "Router",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Skipped",
                    providerResponse: "Recipient is in SMS opt-out list.",
                    isTest: IsTestMessage(message));
                return;
            }

            var provider = await ResolveProviderAsync();

            if (string.Equals(provider, "AfricaTalking", StringComparison.OrdinalIgnoreCase))
            {
                await _africaTalkingProvider.SendAsync(toPhone, message);
                return;
            }

            if (!string.Equals(provider, "Twilio", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unknown SMS provider '{Provider}'. Falling back to Twilio.", provider);
            }

            await _twilioProvider.SendAsync(toPhone, message);
        }

        private async Task<string> ResolveProviderAsync()
        {
            var fromDb = await _settingService.GetSettingValueAsync("Notification:Sms:Provider");
            if (!string.IsNullOrWhiteSpace(fromDb))
            {
                return fromDb.Trim();
            }

            return (_configuration["Notification:Sms:Provider"] ?? "Twilio").Trim();
        }

        private async Task<bool> IsPhoneOptedOutAsync(string recipientPhone)
        {
            if (string.IsNullOrWhiteSpace(recipientPhone))
                return false;

            var enabledRaw = await GetSettingOrConfigAsync("Notification:OptOut:EnableSmsOptOut");
            if (bool.TryParse(enabledRaw, out var enabled) && !enabled)
                return false;

            var listRaw = await GetSettingOrConfigAsync("Notification:OptOut:PhoneRecipients") ?? string.Empty;
            var values = ParseDelimitedValues(listRaw);
            var targetRaw = recipientPhone.Trim();
            var targetDigits = NormalizePhone(targetRaw);
            return values.Contains(targetRaw, StringComparer.OrdinalIgnoreCase)
                || values.Contains(targetDigits, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string?> GetSettingOrConfigAsync(string key)
        {
            var fromDb = await _settingService.GetSettingValueAsync(key);
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb;

            return _configuration[key];
        }

        private static HashSet<string> ParseDelimitedValues(string raw)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = raw.Split(new[] { ',', ';', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                set.Add(trimmed);
                set.Add(NormalizePhone(trimmed));
            }

            return set;
        }

        private static string NormalizePhone(string value)
        {
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static bool IsTestMessage(string message)
        {
            return !string.IsNullOrWhiteSpace(message)
                && message.Contains("test", StringComparison.OrdinalIgnoreCase);
        }
    }
}
