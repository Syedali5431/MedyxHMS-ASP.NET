using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Purpose: Contains application code for TwilioSmsNotificationProvider and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class TwilioSmsNotificationProvider : ISmsNotificationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ISettingService _settingService;
        private readonly INotificationDeliveryAuditService _notificationDeliveryAuditService;
        private readonly ILogger<TwilioSmsNotificationProvider> _logger;

        public TwilioSmsNotificationProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ISettingService settingService,
            INotificationDeliveryAuditService notificationDeliveryAuditService,
            ILogger<TwilioSmsNotificationProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _settingService = settingService;
            _notificationDeliveryAuditService = notificationDeliveryAuditService;
            _logger = logger;
        }

        public async Task SendAsync(string toPhone, string message)
        {
            var accountSid = await GetSettingOrConfigAsync("Notification:Sms:Twilio:AccountSid");
            var authToken = await GetSettingOrConfigAsync("Notification:Sms:Twilio:AuthToken");
            var fromPhone = await GetSettingOrConfigAsync("Notification:Sms:Twilio:FromPhone");

            var liveSendRaw = await GetSettingOrConfigAsync("Notification:Sms:Twilio:EnableLiveSend");
            var enableLiveSend = bool.TryParse(liveSendRaw, out var parsed) && parsed;

            if (!enableLiveSend)
            {
                _logger.LogInformation(
                    "Twilio SMS send skipped because live sending is disabled. To={Phone} | Message={Message}",
                    toPhone,
                    message);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "Twilio",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Failed",
                    providerResponse: "Twilio live send disabled.",
                    isTest: IsTestMessage(message));
                return;
            }

            if (string.IsNullOrWhiteSpace(accountSid)
                || string.IsNullOrWhiteSpace(authToken)
                || string.IsNullOrWhiteSpace(fromPhone))
            {
                _logger.LogWarning(
                    "Twilio SMS send skipped due to missing configuration. To={Phone}",
                    toPhone);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "Twilio",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Failed",
                    providerResponse: "Twilio configuration incomplete.",
                    isTest: IsTestMessage(message));
                return;
            }

            var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new("To", toPhone),
                    new("From", fromPhone),
                    new("Body", message)
                })
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Twilio SMS failed. Status={StatusCode} | To={Phone} | Response={Response}",
                    (int)response.StatusCode,
                    toPhone,
                    responseBody);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "Twilio",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Failed",
                    providerResponse: responseBody,
                    isTest: IsTestMessage(message));
                return;
            }

            _logger.LogInformation("Twilio SMS sent successfully. To={Phone}", toPhone);

            await _notificationDeliveryAuditService.LogAsync(
                channel: "SMS",
                provider: "Twilio",
                recipient: toPhone,
                subject: null,
                messageBody: message,
                status: "Sent",
                providerResponse: "Accepted by Twilio API.",
                isTest: IsTestMessage(message));
        }

        private async Task<string> GetSettingOrConfigAsync(string key)
        {
            var fromDb = await _settingService.GetSettingValueAsync(key);
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb;

            return _configuration[key];
        }

        private static bool IsTestMessage(string message)
        {
            return !string.IsNullOrWhiteSpace(message)
                && message.Contains("test", StringComparison.OrdinalIgnoreCase);
        }
    }
}
