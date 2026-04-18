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
    }

    public class LicenseExpiredViewModel
    {
        public LicenseSnapshot Snapshot { get; set; } = new LicenseSnapshot();

        public string? ReturnUrl { get; set; }
    }
}