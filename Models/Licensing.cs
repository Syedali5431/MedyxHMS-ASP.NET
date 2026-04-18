using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.Models
{
    public enum LicenseState
    {
        Active = 1,
        ExpiringSoon = 2,
        Expired = 3
    }

    public class LicenseRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string LicenseReference { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = LicenseState.Active.ToString();

        public DateTime? LastReminderSentAtUtc { get; set; }

        public DateTime? LastReminderCycleExpiryUtc { get; set; }

        [MaxLength(450)]
        public string? RenewedByUserId { get; set; }

        public DateTime? RenewedAtUtc { get; set; }

        public int? RenewalTermYears { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<LicenseAuditLog> AuditLogs { get; set; } = new List<LicenseAuditLog>();

        public ICollection<LicenseReminderLog> ReminderLogs { get; set; } = new List<LicenseReminderLog>();
    }

    public class LicenseAuditLog
    {
        public int Id { get; set; }

        public int LicenseRecordId { get; set; }

        [Required, MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? PerformedByUserId { get; set; }

        public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? OldExpiresAtUtc { get; set; }

        public DateTime? NewExpiresAtUtc { get; set; }

        public int? RenewalTermYears { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public LicenseRecord? LicenseRecord { get; set; }

        public ApplicationUser? PerformedByUser { get; set; }
    }

    public class LicenseReminderLog
    {
        public int Id { get; set; }

        public int LicenseRecordId { get; set; }

        [Required, MaxLength(50)]
        public string ReminderType { get; set; } = string.Empty;

        public DateTime TargetExpiryUtc { get; set; }

        public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;

        public int SentToCount { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public LicenseRecord? LicenseRecord { get; set; }
    }

    public class LicenseSnapshot
    {
        public LicenseRecord License { get; set; } = new LicenseRecord();

        public LicenseState State { get; set; }

        public int DaysRemaining { get; set; }

        public bool ReminderDue { get; set; }

        public string SuperAdminContact { get; set; } = string.Empty;

        public string BillingContact { get; set; } = string.Empty;
    }

    public class ReminderDispatchResult
    {
        public bool Attempted { get; set; }

        public int SentToCount { get; set; }

        public int FailedCount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}