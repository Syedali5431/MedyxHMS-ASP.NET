using System.Security.Claims;
using System.Text;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for LicenseService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class LicenseService : ILicenseService
    {
        private const string DefaultSuperAdminContact = "superadmin@hospital.com";

        private readonly ApplicationDbContext _context;
        private readonly IEmailNotificationProvider _emailNotificationProvider;
        private readonly ISettingService _settingService;
        private readonly ILicenseFileService _licenseFileService;
        private readonly ISystemNotificationService _systemNotificationService;
        private readonly ILogger<LicenseService> _logger;

        public LicenseService(
            ApplicationDbContext context,
            IEmailNotificationProvider emailNotificationProvider,
            ISettingService settingService,
            ILicenseFileService licenseFileService,
            ISystemNotificationService systemNotificationService,
            ILogger<LicenseService> logger)
        {
            _context = context;
            _emailNotificationProvider = emailNotificationProvider;
            _settingService = settingService;
            _licenseFileService = licenseFileService;
            _systemNotificationService = systemNotificationService;
            _logger = logger;
        }

        public async Task<LicenseSnapshot> GetCurrentSnapshotAsync()
        {
            var license = await GetActiveLicenseAsync();
            if (license == null)
            {
                var missing = new LicenseRecord
                {
                    LicenseReference = "UNLICENSED",
                    ProductName = "MedyxHMS",
                    TenantId = "UNCONFIGURED",
                    LicenseGuid = Guid.Empty,
                    IssuedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                    MaxConcurrentUsers = 0,
                    VerificationKey = string.Empty,
                    LicensedModulesCsv = string.Empty,
                    PublicKeyModulusHex = string.Empty,
                    PublicKeyExponentHex = string.Empty,
                    Nonce = "N/A",
                    SignatureAlgorithm = "RSA-SHA256",
                    SignatureHex = string.Empty,
                    EncodedLicenseFile = string.Empty,
                    CanonicalPayloadJson = string.Empty,
                    PayloadSha256Hex = string.Empty,
                    IsSignatureValid = false,
                    Status = LicenseState.Expired.ToString(),
                    IsActive = false
                };

                return new LicenseSnapshot
                {
                    License = missing,
                    State = LicenseState.Expired,
                    DaysRemaining = -1,
                    ReminderDue = false,
                    SuperAdminContact = await GetSuperAdminContactAsync(),
                    BillingContact = await GetBillingContactAsync()
                };
            }

            var signatureValid = await _licenseFileService.IsCurrentLicenseCryptographicallyValidAsync();
            var state = DetermineState(license.ExpiresAtUtc, DateTime.UtcNow);
            if (!signatureValid)
                state = LicenseState.Expired;

            var daysRemaining = (license.ExpiresAtUtc.Date - DateTime.UtcNow.Date).Days;

            if (!string.Equals(license.Status, state.ToString(), StringComparison.Ordinal))
            {
                license.Status = state.ToString();
                license.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new LicenseSnapshot
            {
                License = license,
                State = state,
                DaysRemaining = daysRemaining,
                ReminderDue = state != LicenseState.Expired
                    && daysRemaining == 5
                    && license.LastReminderCycleExpiryUtc?.Date != license.ExpiresAtUtc.Date,
                SuperAdminContact = await GetSuperAdminContactAsync(),
                BillingContact = await GetBillingContactAsync()
            };
        }

        public async Task<IReadOnlyList<LicenseAuditLog>> GetAuditHistoryAsync(int take = 20)
        {
            var license = await GetActiveLicenseAsync();
            if (license == null)
                return Array.Empty<LicenseAuditLog>();

            return await _context.LicenseAuditLogs
                .Where(a => a.LicenseRecordId == license.Id)
                .OrderByDescending(a => a.PerformedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<LicenseReminderLog>> GetReminderHistoryAsync(int take = 20)
        {
            var license = await GetActiveLicenseAsync();
            if (license == null)
                return Array.Empty<LicenseReminderLog>();

            return await _context.LicenseReminderLogs
                .Where(r => r.LicenseRecordId == license.Id)
                .OrderByDescending(r => r.TriggeredAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<LicenseRecord> RenewAsync(int termYears, string performedByUserId, string? notes = null, string? ipAddress = null)
        {
            throw new InvalidOperationException("Manual renewal is disabled. Upload a digitally signed .lic file from MedyxHMS-Lic.");
        }

        public async Task<ReminderDispatchResult> SendReminderAsync(bool force, string? performedByUserId = null, string? ipAddress = null)
        {
            var now = DateTime.UtcNow;
            var snapshot = await GetCurrentSnapshotAsync();
            var license = snapshot.License;

            if (license.Id <= 0)
            {
                return new ReminderDispatchResult
                {
                    Status = "Skipped",
                    ErrorMessage = "No active signed license is available."
                };
            }

            if (snapshot.State == LicenseState.Expired)
            {
                return new ReminderDispatchResult
                {
                    Status = "Skipped",
                    ErrorMessage = "License is already expired."
                };
            }

            if (!force && snapshot.DaysRemaining != 5)
            {
                return new ReminderDispatchResult
                {
                    Status = "Skipped",
                    ErrorMessage = $"Reminder is sent only 5 days before expiry. Current days remaining: {snapshot.DaysRemaining}."
                };
            }

            if (!force && license.LastReminderCycleExpiryUtc?.Date == license.ExpiresAtUtc.Date)
            {
                return new ReminderDispatchResult
                {
                    Status = "Skipped",
                    ErrorMessage = "Reminder already sent for the current license cycle."
                };
            }

            var recipients = await _context.Users
                .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                .Select(u => u.Email!)
                .Distinct()
                .OrderBy(email => email)
                .ToListAsync();

            if (recipients.Count == 0)
            {
                return new ReminderDispatchResult
                {
                    Status = "Skipped",
                    ErrorMessage = "No active user emails are available for reminder dispatch."
                };
            }

            var subject = await BuildReminderSubjectAsync(snapshot);
            var body = await BuildReminderBodyAsync(snapshot);
            var sent = 0;
            var failed = 0;
            var failures = new List<string>();

            foreach (var email in recipients)
            {
                try
                {
                    await _emailNotificationProvider.SendAsync(email, subject, body);
                    sent++;
                }
                catch (Exception ex)
                {
                    failed++;
                    failures.Add($"{email}: {ex.Message}");
                    _logger.LogWarning(ex, "License reminder delivery failed for {Recipient}", email);
                }
            }

            var status = failed == 0 ? "Sent" : sent > 0 ? "PartialFailure" : "Failed";
            var errorMessage = failures.Count == 0 ? null : string.Join(Environment.NewLine, failures.Take(10));

            _context.LicenseReminderLogs.Add(new LicenseReminderLog
            {
                LicenseRecordId = license.Id,
                ReminderType = force ? "ManualResend" : "PreExpiry",
                TargetExpiryUtc = license.ExpiresAtUtc,
                TriggeredAtUtc = now,
                SentToCount = sent,
                Status = status,
                ErrorMessage = errorMessage
            });

            _context.LicenseAuditLogs.Add(new LicenseAuditLog
            {
                LicenseRecordId = license.Id,
                ActionType = force ? "ReminderResent" : "ReminderSent",
                PerformedByUserId = performedByUserId,
                PerformedAtUtc = now,
                OldExpiresAtUtc = license.ExpiresAtUtc,
                NewExpiresAtUtc = license.ExpiresAtUtc,
                Details = errorMessage ?? $"Reminder processed for {sent} recipients.",
                IpAddress = ipAddress
            });

            if (sent > 0)
            {
                license.LastReminderSentAtUtc = now;
                license.LastReminderCycleExpiryUtc = license.ExpiresAtUtc;
                license.UpdatedAtUtc = now;

                await _systemNotificationService.NotifyAllNonPatientsAsync(
                    "License expiry reminder",
                    $"System license expires on {license.ExpiresAtUtc:yyyy-MM-dd}. Please renew before expiry.",
                    "LicenseReminder",
                    "LicenseRecord",
                    license.Id.ToString());
            }

            await _context.SaveChangesAsync();

            return new ReminderDispatchResult
            {
                Attempted = true,
                SentToCount = sent,
                FailedCount = failed,
                Status = status,
                ErrorMessage = errorMessage
            };
        }

        public async Task<bool> ShouldRestrictAccessAsync(ClaimsPrincipal user, string requestPath)
        {
            if (user.Identity?.IsAuthenticated != true)
                return false;

            if (IsExemptRequest(user, requestPath))
                return false;

            var snapshot = await GetCurrentSnapshotAsync();
            return snapshot.State == LicenseState.Expired || !snapshot.License.IsSignatureValid;
        }

        public async Task<bool> IsModuleLicensedForCurrentLicenseAsync(string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                return false;

            var license = await GetActiveLicenseAsync();
            if (license == null || !license.IsActive || !license.IsSignatureValid || license.ExpiresAtUtc < DateTime.UtcNow)
                return false;

            var raw = license.LicensedModulesCsv ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return true; // Backward-compatible: old licenses without module list are treated as full access.

            var allowed = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return allowed.Contains(moduleKey.Trim());
        }

        private async Task<LicenseRecord?> GetActiveLicenseAsync()
        {
            var license = await _context.LicenseRecords
                .OrderByDescending(l => l.IsActive)
                .ThenByDescending(l => l.CreatedAtUtc)
                .FirstOrDefaultAsync(l => l.IsActive);
            return license;
        }

        private static LicenseState DetermineState(DateTime expiryUtc, DateTime nowUtc)
        {
            var daysRemaining = (expiryUtc.Date - nowUtc.Date).Days;
            if (daysRemaining < 0)
                return LicenseState.Expired;

            return daysRemaining <= 5 ? LicenseState.ExpiringSoon : LicenseState.Active;
        }

        private bool IsExemptRequest(ClaimsPrincipal user, string requestPath)
        {
            if (user.IsInRole("SuperAdmin") || user.IsInRole("Admin") || user.IsInRole("Patient"))
                return true;

            if (string.IsNullOrWhiteSpace(requestPath))
                return false;

            return requestPath.StartsWith("/PatientPortal", StringComparison.OrdinalIgnoreCase)
                || requestPath.StartsWith("/License/Expired", StringComparison.OrdinalIgnoreCase)
                || requestPath.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase)
                || requestPath.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GetSuperAdminContactAsync()
        {
            var configuredContact = await _settingService.GetSettingValueAsync("LicenseSuperAdminContact");
            if (!string.IsNullOrWhiteSpace(configuredContact))
                return configuredContact.Trim();

            var superAdminRoleId = await _context.Set<IdentityRole>()
                .Where(role => role.Name == "SuperAdmin")
                .Select(role => role.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(superAdminRoleId))
                return DefaultSuperAdminContact;

            var emails = await _context.UserRoles
                .Where(userRole => userRole.RoleId == superAdminRoleId)
                .Join(
                    _context.Users,
                    userRole => userRole.UserId,
                    user => user.Id,
                    (_, user) => user)
                .Where(user => user.IsActive && !string.IsNullOrWhiteSpace(user.Email))
                .Select(user => user.Email!)
                .Distinct()
                .Take(3)
                .ToListAsync();

            return emails.Count == 0 ? DefaultSuperAdminContact : string.Join(", ", emails);
        }

        private async Task<string> GetBillingContactAsync()
        {
            var configuredContact = await _settingService.GetSettingValueAsync("LicenseBillingContact");
            return string.IsNullOrWhiteSpace(configuredContact)
                ? await GetSuperAdminContactAsync()
                : configuredContact.Trim();
        }

        private async Task<string> BuildReminderSubjectAsync(LicenseSnapshot snapshot)
        {
            var template = await _settingService.GetSettingValueAsync("LicenseReminderSubject");
            template = string.IsNullOrWhiteSpace(template)
                ? "MedyxHMS license expires in {DaysRemaining} days"
                : template;

            var hospitalName = (await _settingService.GetHospitalSettingsAsync()).Name;
            return ApplyTemplate(template, snapshot, hospitalName);
        }

        private async Task<string> BuildReminderBodyAsync(LicenseSnapshot snapshot)
        {
            var template = await _settingService.GetSettingValueAsync("LicenseReminderBody");
            if (string.IsNullOrWhiteSpace(template))
            {
                var builder = new StringBuilder();
                builder.AppendLine("This is a reminder that your MedyxHMS license will expire soon.");
                builder.AppendLine();
                builder.AppendLine("Expiry Date: {ExpiryDate}");
                builder.AppendLine("Days Remaining: {DaysRemaining}");
                builder.AppendLine();
                builder.AppendLine("Please arrange payment and contact a SuperAdmin user to complete the renewal.");
                builder.AppendLine("SuperAdmin Contact: {SuperAdminContact}");
                builder.AppendLine("Billing Contact: {BillingContact}");
                builder.AppendLine();
                builder.AppendLine("Hospital/App: {HospitalName}");
                template = builder.ToString();
            }

            var hospitalName = (await _settingService.GetHospitalSettingsAsync()).Name;
            return ApplyTemplate(template, snapshot, hospitalName);
        }

        private static string ApplyTemplate(string template, LicenseSnapshot snapshot, string? hospitalName)
        {
            return template
                .Replace("{HospitalName}", hospitalName ?? "MedyxHMS", StringComparison.Ordinal)
                .Replace("{ExpiryDate}", snapshot.License.ExpiresAtUtc.ToString("yyyy-MM-dd"), StringComparison.Ordinal)
                .Replace("{DaysRemaining}", snapshot.DaysRemaining.ToString(), StringComparison.Ordinal)
                .Replace("{SuperAdminContact}", snapshot.SuperAdminContact, StringComparison.Ordinal)
                .Replace("{BillingContact}", snapshot.BillingContact, StringComparison.Ordinal);
        }
    }
}
