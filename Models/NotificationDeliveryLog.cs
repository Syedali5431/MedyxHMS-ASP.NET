using System;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.Models
{
    public class NotificationDeliveryLog
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Channel { get; set; } = string.Empty; // Email | SMS

        [Required, MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // SMTP | Twilio

        [Required, MaxLength(200)]
        public string Recipient { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string MessageBody { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = string.Empty; // Sent | Failed

        [MaxLength(2000)]
        public string ProviderResponse { get; set; } = string.Empty;

        [MaxLength(50)]
        public string RelatedEntityType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string RelatedEntityId { get; set; } = string.Empty;

        public bool IsTest { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
