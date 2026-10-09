using MedyxHMS.Models;
using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Interfaces
{
    /// <summary>Security policy: two-step login for admins, sign-in timeout, audit-log retention, backup folder.</summary>
    public interface ISecurityPolicyService
    {
        Task<SecurityPolicy> GetPolicyAsync();
        Task SavePolicyAsync(SecurityPolicy policy, string? modifiedBy);

        /// <summary>Applies the sign-in timeout to the authentication cookie (also used at start-up).</summary>
        void ApplySessionTimeout(int minutes);

        /// <summary>Whether the user has two-step login set up (cached briefly).</summary>
        Task<bool> IsMfaEnabledAsync(string userId);
        void ForgetMfaStatus(string userId);

        /// <summary>Deletes audit and user-action logs older than the retention period. Returns rows deleted.</summary>
        Task<int> PurgeOldAuditLogsAsync(string? performedBy);
    }

    public interface IDatabaseBackupService
    {
        Task<DatabaseBackup> BackupNowAsync(string? createdBy);
        Task<List<DatabaseBackup>> GetHistoryAsync(int take = 50);
        Task<string> GetEffectiveFolderAsync();
        string DatabaseName { get; }
    }
}
