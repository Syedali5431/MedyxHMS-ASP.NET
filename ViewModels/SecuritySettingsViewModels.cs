using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    /// <summary>Security policy (Setup / Settings → Security &amp; Backups).</summary>
    public class SecurityPolicy
    {
        /// <summary>Admins and SuperAdmins must set up and use two-step login (authenticator app).</summary>
        [Display(Name = "Require two-step login for Admin and SuperAdmin")]
        public bool RequireMfaForAdmins { get; set; } = true;

        /// <summary>Signed-in users are signed out after this many minutes without activity.</summary>
        [Display(Name = "Sign-in timeout (minutes of inactivity)")]
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>Audit and user-action logs older than this are deleted automatically (0 = keep forever).</summary>
        [Display(Name = "Keep audit logs for (days)")]
        public int AuditLogRetentionDays { get; set; } = 365;

        /// <summary>Folder on the database server for backups (empty = SQL Server's default backup folder).</summary>
        [Display(Name = "Backup folder (on the database server)")]
        public string BackupFolder { get; set; } = string.Empty;

        /// <summary>
        /// Test accounts (user names, comma-separated) that may sign in without two-step login even with an
        /// administrator role. For testing only – leave empty on a live system.
        /// </summary>
        [Display(Name = "Test accounts without two-step login")]
        public string MfaExemptUserNames { get; set; } = string.Empty;

        /// <summary>The exempt user names as a clean, comma-separated list.</summary>
        public static string NormalizeUserNames(string? names) =>
            string.Join(", ", (names ?? string.Empty)
                .Split(new[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase));

        public bool IsMfaExempt(string? userName) =>
            !string.IsNullOrWhiteSpace(userName)
            && MfaExemptUserNames.Split(new[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(n => string.Equals(n, userName, StringComparison.OrdinalIgnoreCase));

        public static readonly int[] TimeoutChoices = { 10, 15, 20, 30, 45, 60 };
        public const int MinimumRetentionDays = 90;
    }

    public class AdminMfaStatus
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool MfaEnabled { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class SecuritySettingsViewModel
    {
        public SecurityPolicy Policy { get; set; } = new();
        public List<AdminMfaStatus> Admins { get; set; } = new();
        public int AuditLogCount { get; set; }
        public DateTime? OldestAuditLog { get; set; }
        public string? LastAuditPurge { get; set; }
        public bool EnforcementDisabledByConfig { get; set; }
    }

    public class BackupsViewModel
    {
        public List<DatabaseBackup> History { get; set; } = new();
        public string EffectiveFolder { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
