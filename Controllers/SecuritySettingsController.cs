using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Extensions;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    /// <summary>Security &amp; Backups (ISO 27001-style controls): two-step login policy, sign-in timeout,
    /// audit-log retention and database backups. SuperAdmin only.</summary>
    [Authorize(Roles = "SuperAdmin")]
    public class SecuritySettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISecurityPolicyService _policy;
        private readonly IDatabaseBackupService _backups;
        private readonly IAuditService _audit;
        private readonly IConfiguration _configuration;

        public SecuritySettingsController(ApplicationDbContext context, ISecurityPolicyService policy, IDatabaseBackupService backups,
            IAuditService audit, IConfiguration configuration)
        {
            _context = context;
            _policy = policy;
            _backups = backups;
            _audit = audit;
            _configuration = configuration;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            return View(await BuildModelAsync(await _policy.GetPolicyAsync()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind(Prefix = "Policy")] SecurityPolicy policy)
        {
            if (!SecurityPolicy.TimeoutChoices.Contains(policy.SessionTimeoutMinutes))
            {
                ModelState.AddModelError("Policy.SessionTimeoutMinutes", "Choose one of the listed timeouts.");
            }

            if (policy.AuditLogRetentionDays < 0 || (policy.AuditLogRetentionDays > 0 && policy.AuditLogRetentionDays < SecurityPolicy.MinimumRetentionDays))
            {
                ModelState.AddModelError("Policy.AuditLogRetentionDays", $"Enter 0 (keep forever) or at least {SecurityPolicy.MinimumRetentionDays} days.");
            }
            else if (policy.AuditLogRetentionDays > 3650)
            {
                ModelState.AddModelError("Policy.AuditLogRetentionDays", "Enter at most 3650 days (10 years).");
            }

            policy.BackupFolder = (policy.BackupFolder ?? string.Empty).Trim();
            if (policy.BackupFolder.Length > 0 && (!Path.IsPathFullyQualified(policy.BackupFolder) || policy.BackupFolder.Length > 400 || policy.BackupFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0))
            {
                ModelState.AddModelError("Policy.BackupFolder", "Enter a full folder path on the database server (for example D:\\Backups\\MedyxHMS), or leave it empty.");
            }

            // Test accounts without two-step login: existing accounts only, never a SuperAdmin.
            policy.MfaExemptUserNames = SecurityPolicy.NormalizeUserNames(policy.MfaExemptUserNames);
            if (policy.MfaExemptUserNames.Length > 500)
            {
                ModelState.AddModelError("Policy.MfaExemptUserNames", "Enter at most 500 characters.");
            }
            else if (policy.MfaExemptUserNames.Length > 0)
            {
                var names = policy.MfaExemptUserNames.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();
                var normalized = names.Select(n => n.ToUpperInvariant()).ToList();
                var accounts = await _context.Users.Where(u => normalized.Contains(u.NormalizedUserName!)).Select(u => new { u.Id, u.UserName }).ToListAsync();
                var unknown = names.Where(n => !accounts.Any(a => string.Equals(a.UserName, n, StringComparison.OrdinalIgnoreCase))).ToList();
                var accountIds = accounts.Select(a => a.Id).ToList();
                var superAdmins = await (from ur in _context.Set<IdentityUserRole<string>>()
                                         join r in _context.Set<IdentityRole>() on ur.RoleId equals r.Id
                                         join u in _context.Users on ur.UserId equals u.Id
                                         where r.Name == "SuperAdmin" && accountIds.Contains(ur.UserId)
                                         select u.UserName).ToListAsync();
                if (unknown.Any())
                    ModelState.AddModelError("Policy.MfaExemptUserNames", "No account with the user name " + string.Join(", ", unknown) + ".");
                if (superAdmins.Any())
                    ModelState.AddModelError("Policy.MfaExemptUserNames", "SuperAdmin accounts always need two-step login: " + string.Join(", ", superAdmins) + ".");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildModelAsync(policy));
            }

            var before = await _policy.GetPolicyAsync();
            await _policy.SavePolicyAsync(policy, User.Identity?.Name);
            await _audit.LogActivityAsync(CurrentUserId, "UPDATE", "SecurityPolicy", "Security",
                $"MFA admins: {before.RequireMfaForAdmins}, timeout: {before.SessionTimeoutMinutes} min, retention: {before.AuditLogRetentionDays} days, backup folder: {before.BackupFolder}, without two-step login: {before.MfaExemptUserNames}",
                $"MFA admins: {policy.RequireMfaForAdmins}, timeout: {policy.SessionTimeoutMinutes} min, retention: {policy.AuditLogRetentionDays} days, backup folder: {policy.BackupFolder}, without two-step login: {policy.MfaExemptUserNames}");

            TempData["SuccessMessage"] = "Security settings saved. The new sign-in timeout applies from the next sign-in or page visit.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeAuditLogs()
        {
            var policy = await _policy.GetPolicyAsync();
            if (policy.AuditLogRetentionDays <= 0)
            {
                TempData["InfoMessage"] = "Audit logs are kept forever (retention 0), so nothing was deleted.";
                return RedirectToAction(nameof(Index));
            }

            var deleted = await _policy.PurgeOldAuditLogsAsync(User.Identity?.Name);
            TempData["SuccessMessage"] = $"{deleted} log entries older than {policy.AuditLogRetentionDays} days were deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── Database backups ─────────────────────────────────────

        public async Task<IActionResult> Backups()
        {
            return View(new BackupsViewModel
            {
                History = await _backups.GetHistoryAsync(),
                EffectiveFolder = await _backups.GetEffectiveFolderAsync(),
                DatabaseName = _backups.DatabaseName
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupNow()
        {
            var result = await _backups.BackupNowAsync(User.Identity?.Name);
            if (result.Status == "Succeeded")
            {
                TempData["SuccessMessage"] = $"Backup completed and verified: {result.FilePath}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Backup failed: {result.Message}";
            }

            return RedirectToAction(nameof(Backups));
        }

        private async Task<SecuritySettingsViewModel> BuildModelAsync(SecurityPolicy policy)
        {
            var adminRows = await (from u in _context.Users
                                   join ur in _context.Set<IdentityUserRole<string>>() on u.Id equals ur.UserId
                                   join r in _context.Set<IdentityRole>() on ur.RoleId equals r.Id
                                   where u.IsActive && (r.Name == "Admin" || r.Name == "SuperAdmin")
                                   select new { u.UserName, u.FirstName, u.LastName, u.MFAEnabled, u.LastLoginDate, Role = r.Name }).ToListAsync();

            var admins = adminRows.GroupBy(a => a.UserName)
                .Select(g => new AdminMfaStatus
                {
                    UserName = g.Key ?? string.Empty,
                    FullName = $"{g.First().FirstName} {g.First().LastName}".Trim(),
                    Roles = string.Join(", ", g.Select(x => x.Role).Distinct().OrderBy(x => x)),
                    MfaEnabled = g.First().MFAEnabled,
                    LastLoginDate = g.First().LastLoginDate
                })
                .OrderBy(a => a.UserName)
                .ToList();

            return new SecuritySettingsViewModel
            {
                Policy = policy,
                Admins = admins,
                AuditLogCount = await _context.AuditLogs.CountAsync(),
                OldestAuditLog = await _context.AuditLogs.MinAsync(a => (DateTime?)a.Timestamp),
                LastAuditPurge = await _context.Settings.Where(s => s.Key == SecurityPolicyService.KeyLastPurge).Select(s => s.Value).FirstOrDefaultAsync(),
                EnforcementDisabledByConfig = AdminMfaEnforcementMiddleware.IsDisabledByConfig(_configuration)
            };
        }
    }
}
