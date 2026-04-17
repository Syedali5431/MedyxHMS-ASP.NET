using System;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.Models
{
    public class NotificationDeliveryLog
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Channel { get; set; } // Email | SMS

        [Required, MaxLength(50)]
        public string Provider { get; set; } // SMTP | Twilio

        [Required, MaxLength(200)]
        public string Recipient { get; set; }

        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string MessageBody { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } // Sent | Failed

        [MaxLength(2000)]
        public string ProviderResponse { get; set; }

        [MaxLength(50)]
        public string RelatedEntityType { get; set; }

        [MaxLength(100)]
        public string RelatedEntityId { get; set; }

        public bool IsTest { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
