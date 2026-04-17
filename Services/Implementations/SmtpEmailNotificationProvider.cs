using System;
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
        private readonly INotificationDeliveryAuditService _notificationDeliveryAuditService;
        private readonly ILogger<SmtpEmailNotificationProvider> _logger;

        public SmtpEmailNotificationProvider(
            IConfiguration configuration,
            INotificationDeliveryAuditService notificationDeliveryAuditService,
            ILogger<SmtpEmailNotificationProvider> logger)
        {
            _configuration = configuration;
            _notificationDeliveryAuditService = notificationDeliveryAuditService;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Notification:Smtp:Host"];
            var portRaw = _configuration["Notification:Smtp:Port"];
            var fromEmail = _configuration["Notification:Smtp:FromEmail"];
            var fromName = _configuration["Notification:Smtp:FromName"] ?? "Medyx Hospital";
            var username = _configuration["Notification:Smtp:Username"];
            var password = _configuration["Notification:Smtp:Password"];
            var enableSslRaw = _configuration["Notification:Smtp:EnableSsl"];

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

            try
            {
                await client.SendMailAsync(message);

                await _notificationDeliveryAuditService.LogAsync(
                    channel: "Email",
                    provider: "SMTP",
                    recipient: toEmail,
                    subject: subject,
                    messageBody: body,
                    status: "Sent",
                    providerResponse: "Accepted by SMTP client.",
                    isTest: IsTestMessage(subject));
            }
            catch (Exception ex)
            {
                await _notificationDeliveryAuditService.LogAsync(
                    channel: "Email",
                    provider: "SMTP",
                    recipient: toEmail,
                    subject: subject,
                    messageBody: body,
                    status: "Failed",
                    providerResponse: ex.Message,
                    isTest: IsTestMessage(subject));
                throw;
            }
        }

        private static bool IsTestMessage(string subject)
        {
            return !string.IsNullOrWhiteSpace(subject)
                && subject.Contains("test", StringComparison.OrdinalIgnoreCase);
        }
    }
}
