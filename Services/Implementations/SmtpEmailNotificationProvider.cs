using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedyxHMS.Services.Implementations
{
    public class SmtpEmailNotificationProvider : IEmailNotificationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ISettingService _settingService;
        private readonly INotificationDeliveryAuditService _notificationDeliveryAuditService;
        private readonly ILogger<SmtpEmailNotificationProvider> _logger;

        public SmtpEmailNotificationProvider(
            IConfiguration configuration,
            ISettingService settingService,
            INotificationDeliveryAuditService notificationDeliveryAuditService,
            ILogger<SmtpEmailNotificationProvider> logger)
        {
            _configuration = configuration;
            _settingService = settingService;
            _notificationDeliveryAuditService = notificationDeliveryAuditService;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            if (await IsEmailOptedOutAsync(toEmail))
            {
                _logger.LogInformation("Email notification skipped due to recipient opt-out. To={Recipient}", toEmail);
                await _notificationDeliveryAuditService.LogAsync(
                    channel: "Email",
                    provider: "SMTP",
                    recipient: toEmail,
                    subject: subject,
                    messageBody: body,
                    status: "Skipped",
                    providerResponse: "Recipient is in email opt-out list.",
                    isTest: IsTestMessage(subject));
                return;
            }

            var host = await GetSettingOrConfigAsync("Notification:Smtp:Host");
            var portRaw = await GetSettingOrConfigAsync("Notification:Smtp:Port");
            var fromEmail = await GetSettingOrConfigAsync("Notification:Smtp:FromEmail");
            var fromName = await GetSettingOrConfigAsync("Notification:Smtp:FromName") ?? "Medyx Hospital";
            var username = await GetSettingOrConfigAsync("Notification:Smtp:Username");
            var password = await GetSettingOrConfigAsync("Notification:Smtp:Password");
            var enableSslRaw = await GetSettingOrConfigAsync("Notification:Smtp:EnableSsl");
            var retryEnabledRaw = await GetSettingOrConfigAsync("Notification:Smtp:RetryEnabled");
            var retryCountRaw = await GetSettingOrConfigAsync("Notification:Smtp:RetryCount");
            var retryDelayRaw = await GetSettingOrConfigAsync("Notification:Smtp:RetryDelayMilliseconds");

            if (string.IsNullOrWhiteSpace(host)
                || string.IsNullOrWhiteSpace(portRaw)
                || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning(
                    "SMTP notification skipped due to missing configuration. Target={TargetEmail}",
                    toEmail);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "Email",
                    provider: "SMTP",
                    recipient: toEmail,
                    subject: subject,
                    messageBody: body,
                    status: "Failed",
                    providerResponse: "SMTP configuration incomplete.",
                    isTest: IsTestMessage(subject));
                return;
            }

            if (!int.TryParse(portRaw, out var port))
                port = 587;

            var enableSsl = true;
            if (bool.TryParse(enableSslRaw, out var parsedSsl))
                enableSsl = parsedSsl;

            var retryEnabled = !bool.TryParse(retryEnabledRaw, out var parsedRetryEnabled) || parsedRetryEnabled;
            var retryCount = int.TryParse(retryCountRaw, out var parsedRetryCount) ? Math.Max(0, parsedRetryCount) : 2;
            var retryDelayMilliseconds = int.TryParse(retryDelayRaw, out var parsedRetryDelay) ? Math.Max(100, parsedRetryDelay) : 800;

            var maxAttempts = retryEnabled ? retryCount + 1 : 1;
            Exception? lastException = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var message = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    };
                    message.To.Add(toEmail);

                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false
                    };

                    if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
                        client.Credentials = new NetworkCredential(username, password);

                    await client.SendMailAsync(message);

                    await _notificationDeliveryAuditService.LogAsync(
                        channel: "Email",
                        provider: "SMTP",
                        recipient: toEmail,
                        subject: subject,
                        messageBody: body,
                        status: "Sent",
                        providerResponse: $"Accepted by SMTP client. Attempts={attempt}/{maxAttempts}.",
                        isTest: IsTestMessage(subject));
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "SMTP send attempt {Attempt}/{MaxAttempts} failed for {Recipient}",
                        attempt,
                        maxAttempts,
                        toEmail);

                    if (attempt >= maxAttempts)
                    {
                        break;
                    }

                    // Progressive backoff prevents immediate repeated retries against a degraded SMTP provider.
                    var delay = retryDelayMilliseconds * attempt;
                    await Task.Delay(delay);
                }
            }

            var errorMessage = lastException?.Message ?? "Unknown SMTP send error.";
            await _notificationDeliveryAuditService.LogAsync(
                channel: "Email",
                provider: "SMTP",
                recipient: toEmail,
                subject: subject,
                messageBody: body,
                status: "Failed",
                providerResponse: $"{errorMessage} Attempts={maxAttempts}/{maxAttempts}.",
                isTest: IsTestMessage(subject));
            throw lastException ?? new InvalidOperationException("SMTP send failed without a captured exception.");
        }

        private async Task<string?> GetSettingOrConfigAsync(string key)
        {
            var fromDb = await _settingService.GetSettingValueAsync(key);
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb;

            return _configuration[key];
        }

        private static bool IsTestMessage(string subject)
        {
            return !string.IsNullOrWhiteSpace(subject)
                && subject.Contains("test", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> IsEmailOptedOutAsync(string recipientEmail)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
                return false;

            var enabledRaw = await GetSettingOrConfigAsync("Notification:OptOut:EnableEmailOptOut");
            if (bool.TryParse(enabledRaw, out var enabled) && !enabled)
                return false;

            var listRaw = await GetSettingOrConfigAsync("Notification:OptOut:EmailRecipients") ?? string.Empty;
            var values = ParseDelimitedValues(listRaw);
            var target = recipientEmail.Trim().ToLowerInvariant();
            return values.Contains(target);
        }

        private static HashSet<string> ParseDelimitedValues(string raw)
        {
            return raw
                .Split(new[] { ',', ';', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
