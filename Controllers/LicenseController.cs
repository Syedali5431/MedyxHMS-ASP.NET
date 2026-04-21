using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class LicenseController : Controller
    {
        private readonly ILicenseService _licenseService;
        private readonly ILicenseFileService _licenseFileService;
        private readonly IModuleService _moduleService;
        private readonly ISettingService _settingService;
        private readonly ILogger<LicenseController> _logger;

        public LicenseController(
            ILicenseService licenseService,
            ILicenseFileService licenseFileService,
            IModuleService moduleService,
            ISettingService settingService,
            ILogger<LicenseController> logger)
        {
            _licenseService = licenseService;
            _licenseFileService = licenseFileService;
            _moduleService = moduleService;
            _settingService = settingService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var (snapshot, legacyFullAccess, rows) = await BuildEntitlementRowsAsync();

            var model = new LicenseManagementViewModel
            {
                Snapshot = snapshot,
                AuditHistory = (await _licenseService.GetAuditHistoryAsync()).ToList(),
                ReminderHistory = (await _licenseService.GetReminderHistoryAsync()).ToList(),
                PublicKeyModulusHex = await _settingService.GetSettingValueAsync("LicensePublicKeyModulusHex"),
                PublicKeyExponentHex = await _settingService.GetSettingValueAsync("LicensePublicKeyExponentHex"),
                VerificationKey = await _settingService.GetSettingValueAsync("LicenseVerificationKey"),
                IsLegacyFullAccessLicense = legacyFullAccess,
                ModuleEntitlements = rows
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ExportEntitlementMatrix()
        {
            var (snapshot, legacyFullAccess, rows) = await BuildEntitlementRowsAsync();

            var builder = new StringBuilder();
            builder.AppendLine("LicenseReference,LicenseExpiryUtc,LegacyFullAccessMode,ModuleKey,ModuleName,Status");

            foreach (var row in rows)
            {
                builder.Append('"').Append(EscapeCsv(snapshot.License.LicenseReference)).Append("\",");
                builder.Append('"').Append(snapshot.License.ExpiresAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")).Append("\",");
                builder.Append(legacyFullAccess ? "true" : "false").Append(',');
                builder.Append('"').Append(EscapeCsv(row.Key)).Append("\",");
                builder.Append('"').Append(EscapeCsv(row.DisplayName)).Append("\",");
                builder.Append('"').Append(row.IsLicensed ? "Licensed" : "Locked").AppendLine("\"");
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var fileName = $"license-entitlements-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SavePublicKey(LicenseManagementViewModel model)
        {
            try
            {
                var normalizedModulus = NormalizeHex(model.PublicKeyModulusHex);
                var normalizedExponent = NormalizeHex(model.PublicKeyExponentHex);

                ValidatePublicKeyHex(normalizedModulus, normalizedExponent);
                var verificationKey = LicenseCryptoUtility.ComputeVerificationKey(normalizedModulus, normalizedExponent);

                await _settingService.UpdateSettingAsync("LicensePublicKeyModulusHex", normalizedModulus);
                await _settingService.UpdateSettingAsync("LicensePublicKeyExponentHex", normalizedExponent);
                await _settingService.UpdateSettingAsync("LicenseVerificationKey", verificationKey);

                TempData["SuccessMessage"] = $"License public key updated successfully. Verification Key: {verificationKey}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "License public key settings update failed.");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Upload(IFormFile licenseFile)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                await _licenseFileService.ValidateAndActivateAsync(
                    licenseFile,
                    userId,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = "Signed license uploaded and activated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Signed license upload failed.");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Renew(LicenseManagementViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                await _licenseService.RenewAsync(
                    model.SelectedRenewalTermYears,
                    userId,
                    model.Notes,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = $"License renewed successfully for {model.SelectedRenewalTermYears} year(s).";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "License renewal failed.");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SendReminder()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                var result = await _licenseService.SendReminderAsync(
                    force: true,
                    performedByUserId: userId,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = string.Equals(result.Status, "Skipped", StringComparison.OrdinalIgnoreCase)
                    ? result.ErrorMessage ?? "Reminder skipped."
                    : $"Reminder processed. Sent={result.SentToCount}, Failed={result.FailedCount}.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Manual license reminder failed.");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Expired(string? returnUrl = null)
        {
            var model = new LicenseExpiredViewModel
            {
                Snapshot = await _licenseService.GetCurrentSnapshotAsync(),
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        public IActionResult FeatureLocked(string? moduleKey = null, string? reason = null, string? returnUrl = null)
        {
            var model = new FeatureLockedViewModel
            {
                ModuleKey = moduleKey ?? string.Empty,
                ReturnUrl = returnUrl,
                Message = string.Equals(reason, "admin", StringComparison.OrdinalIgnoreCase)
                    ? "This feature is currently disabled by your administrator."
                    : "Please buy this feature to use it."
            };

            return View(model);
        }

        private static string NormalizeHex(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Public key hex values are required.");

            var cleaned = new string(value
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .Trim();

            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[2..];

            return cleaned.ToUpperInvariant();
        }

        private static void ValidatePublicKeyHex(string modulusHex, string exponentHex)
        {
            if (modulusHex.Length < 512)
                throw new InvalidDataException("Public key modulus appears too short.");

            if (!IsHex(modulusHex) || !IsHex(exponentHex))
                throw new InvalidDataException("Public key values must be valid hexadecimal strings.");

            if (modulusHex.Length % 2 != 0 || exponentHex.Length % 2 != 0)
                throw new InvalidDataException("Hex values must contain an even number of characters.");

            byte[] modulus;
            byte[] exponent;

            try
            {
                modulus = Convert.FromHexString(modulusHex);
                exponent = Convert.FromHexString(exponentHex);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Public key hex conversion failed.", ex);
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = modulus,
                    Exponent = exponent
                });
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Public key parameters are invalid for RSA.", ex);
            }
        }

        private static bool IsHex(string value)
        {
            return value.All(c =>
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'F') ||
                (c >= 'a' && c <= 'f'));
        }

        private async Task<(MedyxHMS.Models.LicenseSnapshot Snapshot, bool LegacyFullAccess, List<ModuleEntitlementRow> Rows)> BuildEntitlementRowsAsync()
        {
            var snapshot = await _licenseService.GetCurrentSnapshotAsync();
            var allModules = await _moduleService.GetAllModulesAsync();

            var licensedCsv = snapshot.License.LicensedModulesCsv ?? string.Empty;
            var licensedSet = licensedCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var legacyFullAccess = licensedSet.Count == 0;

            var rows = allModules
                .Select(module => new ModuleEntitlementRow
                {
                    Key = module.Key,
                    DisplayName = module.DisplayName,
                    IsLicensed = legacyFullAccess || licensedSet.Contains(module.Key)
                })
                .ToList();

            return (snapshot, legacyFullAccess, rows);
        }

        private static string EscapeCsv(string? value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }
    }
}