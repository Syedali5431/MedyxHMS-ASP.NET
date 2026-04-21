using System.Net.Http;
using System.Net.Http.Headers;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedyxHMS.Services.Implementations
{
    public class AfricaTalkingSmsNotificationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ISettingService _settingService;
        private readonly INotificationDeliveryAuditService _notificationDeliveryAuditService;
        private readonly ILogger<AfricaTalkingSmsNotificationProvider> _logger;

        public AfricaTalkingSmsNotificationProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ISettingService settingService,
            INotificationDeliveryAuditService notificationDeliveryAuditService,
            ILogger<AfricaTalkingSmsNotificationProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _settingService = settingService;
            _notificationDeliveryAuditService = notificationDeliveryAuditService;
            _logger = logger;
        }

        public async Task SendAsync(string toPhone, string message)
        {
            var username = await GetSettingOrConfigAsync("Notification:Sms:AfricaTalking:Username");
            var apiKey = await GetSettingOrConfigAsync("Notification:Sms:AfricaTalking:ApiKey");
            var senderId = await GetSettingOrConfigAsync("Notification:Sms:AfricaTalking:SenderId");

            var liveSendRaw = await GetSettingOrConfigAsync("Notification:Sms:AfricaTalking:EnableLiveSend");
            var enableLiveSend = bool.TryParse(liveSendRaw, out var parsed) && parsed;

            if (!enableLiveSend)
            {
                _logger.LogInformation(
                    "Africa's Talking SMS send skipped because live sending is disabled. To={Phone}",
                    toPhone);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "AfricaTalking",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Failed",
                    providerResponse: "Africa's Talking live send disabled.",
                    isTest: IsTestMessage(message));
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Africa's Talking SMS send skipped due to missing configuration. To={Phone}", toPhone);
                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "AfricaTalking",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Failed",
                    providerResponse: "Africa's Talking configuration incomplete.",
                    isTest: IsTestMessage(message));
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.africastalking.com/version1/messaging");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("apiKey", apiKey);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("username", username),
                new("to", toPhone),
                new("message", message)
            };

            if (!string.IsNullOrWhiteSpace(senderId))
            {
                formData.Add(new KeyValuePair<string, string>("from", senderId));
            }

            request.Content = new FormUrlEncodedContent(formData);

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Africa's Talking SMS failed. Status={StatusCode} | To={Phone} | Response={Response}",
                    (int)response.StatusCode,
                    toPhone,
                    responseBody);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "SMS",
                    provider: "AfricaTalking",
                    recipient: toPhone,
                    subject: null,
                    messageBody: message,
                    status: "Failed",
                    providerResponse: responseBody,
                    isTest: IsTestMessage(message));
                return;
            }

            await _notificationDeliveryAuditService.LogAsync(
                channel: "SMS",
                provider: "AfricaTalking",
                recipient: toPhone,
                subject: null,
                messageBody: message,
                status: "Sent",
                providerResponse: "Accepted by Africa's Talking API.",
                isTest: IsTestMessage(message));
        }

        private async Task<string?> GetSettingOrConfigAsync(string key)
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