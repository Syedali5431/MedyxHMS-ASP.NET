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

        [MaxLength(100)]
        public string ProductName { get; set; } = "MedyxHMS";

        [MaxLength(150)]
        public string TenantId { get; set; } = string.Empty;

        public Guid LicenseGuid { get; set; }

        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }

        public int MaxConcurrentUsers { get; set; }

        [Required, MaxLength(64)]
        public string VerificationKey { get; set; } = string.Empty;

        [Required]
        public string LicensedModulesCsv { get; set; } = string.Empty;

        [Required]
        public string PublicKeyModulusHex { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string PublicKeyExponentHex { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string Nonce { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string SignatureAlgorithm { get; set; } = "RSA-SHA256";

        [Required]
        public string SignatureHex { get; set; } = string.Empty;

        [Required]
        public string EncodedLicenseFile { get; set; } = string.Empty;

        [Required]
        public string CanonicalPayloadJson { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string PayloadSha256Hex { get; set; } = string.Empty;

        public bool IsSignatureValid { get; set; }

        public DateTime? LastValidatedAtUtc { get; set; }

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

    public class SignedLicenseFile
    {
        public LicensePayload Payload { get; set; } = new();

        public string SignatureHex { get; set; } = string.Empty;

        public string Algorithm { get; set; } = "RSA-SHA256";
    }

    public class LicensePayload
    {
        public string ProductName { get; set; } = string.Empty;

        public string TenantId { get; set; } = string.Empty;

        public Guid LicenseId { get; set; }

        public DateTime IssuedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public int MaxConcurrentUsers { get; set; }

        public string VerificationKey { get; set; } = string.Empty;

        public List<string> LicensedModules { get; set; } = new();

        public string Nonce { get; set; } = string.Empty;
    }

    public class ConcurrentLoginDecision
    {
        public bool IsAllowed { get; set; }

        public string? DenyReason { get; set; }

        public int ActiveUsers { get; set; }

        public int MaxConcurrentUsers { get; set; }
    }
}