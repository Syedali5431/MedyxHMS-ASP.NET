using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class LicenseManagementViewModel
    {
        public LicenseSnapshot Snapshot { get; set; } = new LicenseSnapshot();

        public List<LicenseAuditLog> AuditHistory { get; set; } = new List<LicenseAuditLog>();

        public List<LicenseReminderLog> ReminderHistory { get; set; } = new List<LicenseReminderLog>();

        [Range(1, 3)]
        public int SelectedRenewalTermYears { get; set; } = 1;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Public Key Modulus (Hex)")]
        public string? PublicKeyModulusHex { get; set; }

        [Display(Name = "Public Key Exponent (Hex)")]
        public string? PublicKeyExponentHex { get; set; }

        [Display(Name = "Verification Key")]
        public string? VerificationKey { get; set; }

        public bool IsLegacyFullAccessLicense { get; set; }

        public List<ModuleEntitlementRow> ModuleEntitlements { get; set; } = new List<ModuleEntitlementRow>();
    }

    public class ModuleEntitlementRow
    {
        public string Key { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsLicensed { get; set; }
    }

    public class LicenseExpiredViewModel
    {
        public LicenseSnapshot Snapshot { get; set; } = new LicenseSnapshot();

        public string? ReturnUrl { get; set; }
    }

    public class FeatureLockedViewModel
    {
        public string ModuleKey { get; set; } = string.Empty;

        public string Message { get; set; } = "Please buy this feature to use it.";

        public string? ReturnUrl { get; set; }
    }
}