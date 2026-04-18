using System.Security.Claims;
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
        private readonly ILogger<LicenseController> _logger;

        public LicenseController(ILicenseService licenseService, ILogger<LicenseController> logger)
        {
            _licenseService = licenseService;
            _logger = logger;
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var model = new LicenseManagementViewModel
            {
                Snapshot = await _licenseService.GetCurrentSnapshotAsync(),
                AuditHistory = (await _licenseService.GetAuditHistoryAsync()).ToList(),
                ReminderHistory = (await _licenseService.GetReminderHistoryAsync()).ToList()
            };

            return View(model);
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
    }
}