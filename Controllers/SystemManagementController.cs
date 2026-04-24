using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Pharmacist,Accountant,Receptionist,LabTechnician,Radiologist,Staff")]
    public class SystemManagementController : Controller
    {
        private readonly IReportCatalogVisibilityService _reportCatalogVisibilityService;
        private readonly ILogger<SystemManagementController> _logger;

        public SystemManagementController(
            IReportCatalogVisibilityService reportCatalogVisibilityService,
            ILogger<SystemManagementController> logger)
        {
            _reportCatalogVisibilityService = reportCatalogVisibilityService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ReportManagement(string? search)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var canSeeAdminOnly = User.IsInRole("Admin") || isSuperAdmin;

            var inactiveKeys = await _reportCatalogVisibilityService.GetInactiveKeysAsync();
            var visibleItems = await _reportCatalogVisibilityService.GetVisibleItemsForUserAsync(canSeeAdminOnly, isSuperAdmin);

            var filtered = string.IsNullOrWhiteSpace(search)
                ? visibleItems
                : visibleItems
                    .Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || i.Summary.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || i.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var rows = filtered
                .Select((item, index) => new SystemManagementReportRowViewModel
                {
                    SerialNo = index + 1,
                    Key = item.Key,
                    Name = item.Name,
                    Purpose = item.Summary,
                    IsActive = !inactiveKeys.Contains(item.Key)
                })
                .ToList();

            var vm = new SystemManagementReportListViewModel
            {
                IsSuperAdmin = isSuperAdmin,
                SearchTerm = search ?? string.Empty,
                TotalReports = rows.Count,
                ActiveReports = rows.Count(r => r.IsActive),
                Rows = rows
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetReportActive(string reportKey, bool isActive, string? returnUrl)
        {
            var updated = await _reportCatalogVisibilityService.SetReportActiveStateAsync(reportKey, isActive);

            if (!updated)
            {
                _logger.LogWarning("Failed to update report active state. Key: {ReportKey}, IsActive: {IsActive}", reportKey, isActive);
                TempData["ErrorMessage"] = "Could not update report state.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Report {reportKey} marked {(isActive ? "Active" : "Inactive")}.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(ReportManagement));
        }
    }
}
