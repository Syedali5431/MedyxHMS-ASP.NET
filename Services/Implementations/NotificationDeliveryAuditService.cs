using System;
using System.Threading.Tasks;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class NotificationDeliveryAuditService : INotificationDeliveryAuditService
    {
        private readonly ApplicationDbContext _db;

        public NotificationDeliveryAuditService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(
            string channel,
            string provider,
            string recipient,
            string subject,
            string messageBody,
            string status,
            string? providerResponse = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null,
            bool isTest = false)
        {
            var entry = new NotificationDeliveryLog
            {
                Channel = (channel ?? string.Empty).Trim(),
                Provider = (provider ?? string.Empty).Trim(),
                Recipient = (recipient ?? string.Empty).Trim(),
                Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim(),
                MessageBody = messageBody ?? string.Empty,
                Status = (status ?? string.Empty).Trim(),
                ProviderResponse = string.IsNullOrWhiteSpace(providerResponse)
                    ? null
                    : Truncate(providerResponse.Trim(), 2000),
                RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType)
                    ? null
                    : Truncate(relatedEntityType.Trim(), 50),
                RelatedEntityId = string.IsNullOrWhiteSpace(relatedEntityId)
                    ? null
                    : Truncate(relatedEntityId.Trim(), 100),
                IsTest = isTest,
                CreatedAt = DateTime.UtcNow
            };

            _db.NotificationDeliveryLogs.Add(entry);
            await _db.SaveChangesAsync();
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value;

            return value[..max];
        }
    }
}
