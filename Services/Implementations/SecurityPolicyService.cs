using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MedyxHMS.Services.Implementations
{
    public class SecurityPolicyService : ISecurityPolicyService
    {
        private const string Category = "Security";
        public const string KeyRequireMfa = "Security:RequireMfaForAdmins";
        public const string KeyTimeout = "Security:SessionTimeoutMinutes";
        public const string KeyRetention = "Security:AuditLogRetentionDays";
        public const string KeyBackupFolder = "Security:BackupFolder";
        public const string KeyLastPurge = "Security:LastAuditPurge";
        public const string KeyMfaExempt = "Security:MfaExemptUserNames";
        private const string PolicyCacheKey = "security:policy";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IOptionsMonitor<CookieAuthenticationOptions> _cookieOptions;
        private readonly IAuditService _audit;
        private readonly ILogger<SecurityPolicyService> _logger;

        public SecurityPolicyService(ApplicationDbContext context, IMemoryCache cache, IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
            IAuditService audit, ILogger<SecurityPolicyService> logger)
        {
            _context = context;
            _cache = cache;
            _cookieOptions = cookieOptions;
            _audit = audit;
            _logger = logger;
        }

        public async Task<SecurityPolicy> GetPolicyAsync()
        {
            if (_cache.TryGetValue(PolicyCacheKey, out SecurityPolicy? cached) && cached != null)
            {
                return Copy(cached);
            }

            var values = await _context.Settings.AsNoTracking()
                .Where(s => s.Category == Category)
                .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);

            var defaults = new SecurityPolicy();
            var policy = new SecurityPolicy
            {
                RequireMfaForAdmins = !bool.TryParse(values.GetValueOrDefault(KeyRequireMfa), out var mfa) || mfa,
                SessionTimeoutMinutes = NormalizeTimeout(int.TryParse(values.GetValueOrDefault(KeyTimeout), out var t) ? t : defaults.SessionTimeoutMinutes),
                AuditLogRetentionDays = int.TryParse(values.GetValueOrDefault(KeyRetention), out var r) ? NormalizeRetention(r) : defaults.AuditLogRetentionDays,
                BackupFolder = values.GetValueOrDefault(KeyBackupFolder) ?? string.Empty,
                MfaExemptUserNames = SecurityPolicy.NormalizeUserNames(values.GetValueOrDefault(KeyMfaExempt))
            };

            _cache.Set(PolicyCacheKey, policy, CacheDuration);
            return Copy(policy);
        }

        public async Task SavePolicyAsync(SecurityPolicy policy, string? modifiedBy)
        {
            await UpsertAsync(KeyRequireMfa, policy.RequireMfaForAdmins.ToString().ToLowerInvariant(), "bool", "Admins and SuperAdmins must use two-step login", modifiedBy);
            await UpsertAsync(KeyTimeout, NormalizeTimeout(policy.SessionTimeoutMinutes).ToString(), "int", "Sign-in timeout in minutes of inactivity", modifiedBy);
            await UpsertAsync(KeyRetention, NormalizeRetention(policy.AuditLogRetentionDays).ToString(), "int", "Days to keep audit logs (0 = forever)", modifiedBy);
            await UpsertAsync(KeyBackupFolder, (policy.BackupFolder ?? string.Empty).Trim(), "string", "Database backup folder on the database server", modifiedBy);
            await UpsertAsync(KeyMfaExempt, SecurityPolicy.NormalizeUserNames(policy.MfaExemptUserNames), "string", "Test accounts that may sign in without two-step login (testing only)", modifiedBy);
            await _context.SaveChangesAsync();
            _cache.Remove(PolicyCacheKey);
            ApplySessionTimeout(NormalizeTimeout(policy.SessionTimeoutMinutes));
        }

        public void ApplySessionTimeout(int minutes)
        {
            // The cookie handler reads these options on every request, so the new timeout applies to the next
            // sign-in or cookie renewal without a restart.
            var options = _cookieOptions.Get(IdentityConstants.ApplicationScheme);
            options.ExpireTimeSpan = TimeSpan.FromMinutes(NormalizeTimeout(minutes));
            options.SlidingExpiration = true;
        }

        public async Task<bool> IsMfaEnabledAsync(string userId)
        {
            var key = MfaCacheKey(userId);
            if (_cache.TryGetValue(key, out bool enabled))
            {
                return enabled;
            }

            enabled = await _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.MFAEnabled).FirstOrDefaultAsync();
            _cache.Set(key, enabled, CacheDuration);
            return enabled;
        }

        public void ForgetMfaStatus(string userId) => _cache.Remove(MfaCacheKey(userId));

        public async Task<int> PurgeOldAuditLogsAsync(string? performedBy)
        {
            var policy = await GetPolicyAsync();
            if (policy.AuditLogRetentionDays <= 0)
            {
                return 0;
            }

            var cutoff = DateTime.Now.AddDays(-policy.AuditLogRetentionDays);
            var audit = await _context.AuditLogs.Where(a => a.Timestamp < cutoff).ExecuteDeleteAsync();
            var actions = await _context.UserActionLogs.Where(a => a.LoggedDate < cutoff).ExecuteDeleteAsync();
            var total = audit + actions;

            await UpsertAsync(KeyLastPurge, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC – {total} entries older than {cutoff:yyyy-MM-dd} deleted", "string", "Last audit-log retention run", performedBy);
            await _context.SaveChangesAsync();

            // The purge itself is recorded (it is newer than the cutoff, so it is kept).
            await _audit.LogActivityAsync(null, "AUDIT_RETENTION_PURGE", "AuditLog", "retention", null,
                $"{audit} audit log and {actions} user-action entries older than {cutoff:yyyy-MM-dd} deleted ({policy.AuditLogRetentionDays}-day retention) by {performedBy ?? "scheduled job"}");
            _logger.LogInformation("Audit retention: deleted {Audit} audit logs and {Actions} user-action logs older than {Cutoff}", audit, actions, cutoff);
            return total;
        }

        public static int NormalizeTimeout(int minutes) =>
            SecurityPolicy.TimeoutChoices.Contains(minutes) ? minutes : 30;

        public static int NormalizeRetention(int days) =>
            days <= 0 ? 0 : Math.Max(days, SecurityPolicy.MinimumRetentionDays);

        private static string MfaCacheKey(string userId) => "security:mfa:" + userId;

        private static SecurityPolicy Copy(SecurityPolicy p) => new()
        {
            RequireMfaForAdmins = p.RequireMfaForAdmins,
            SessionTimeoutMinutes = p.SessionTimeoutMinutes,
            AuditLogRetentionDays = p.AuditLogRetentionDays,
            BackupFolder = p.BackupFolder,
            MfaExemptUserNames = p.MfaExemptUserNames
        };

        private async Task UpsertAsync(string key, string value, string type, string description, string? modifiedBy)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                _context.Settings.Add(new Setting
                {
                    Key = key, Value = value, Type = type, Category = Category, Description = description,
                    IsSystem = true, CreatedDate = DateTime.Now, ModifiedBy = modifiedBy ?? "System"
                });
                return;
            }

            setting.Value = value;
            setting.ModifiedDate = DateTime.Now;
            setting.ModifiedBy = modifiedBy ?? "System";
        }
    }
}
