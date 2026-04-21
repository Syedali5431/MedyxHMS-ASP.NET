using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            IReportTemplateService reportTemplateService,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _reportTemplateService = reportTemplateService;
            _logger = logger;
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> DepartmentReport(int? departmentId, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddMonths(-1);

            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            try
            {
                var report = await _reportService.GenerateDepartmentReportAsync(departmentId, startDate.Value, endDate.Value);

                ViewData["StartDate"] = startDate;
                ViewData["EndDate"] = endDate;
                ViewData["DepartmentId"] = departmentId;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating department report");
                return View(new List<dynamic>());
            }
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> FinancialReport(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddMonths(-1);

            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            try
            {
                var report = await _reportService.GenerateFinancialReportAsync(startDate.Value, endDate.Value);

                ViewData["StartDate"] = startDate;
                ViewData["EndDate"] = endDate;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial report");
                return View(new Dictionary<string, decimal>());
            }
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> OccupancyReport(DateTime? date)
        {
            if (!date.HasValue)
                date = DateTime.UtcNow;

            try
            {
                var occupancyData = await _reportService.GenerateOccupancyReportAsync(date.Value);
                var averageRate = await _reportService.GetAverageOccupancyRateAsync(date.Value.AddMonths(-1), date.Value);

                ViewData["Date"] = date;
                ViewData["AverageOccupancyRate"] = averageRate;

                return View(occupancyData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating occupancy report");
                return View(new Dictionary<string, int>());
            }
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> StaffReport(string staffId, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddMonths(-1);

            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            try
            {
                var report = await _reportService.GenerateStaffAttendanceReportAsync(staffId, startDate.Value, endDate.Value);

                ViewData["StaffId"] = staffId;
                ViewData["StartDate"] = startDate;
                ViewData["EndDate"] = endDate;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating staff report");
                return View(new List<dynamic>());
            }
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> PayrollReport(DateTime? month)
        {
            if (!month.HasValue)
                month = DateTime.UtcNow;

            try
            {
                var report = await _reportService.GeneratePayrollReportAsync(month.Value);

                ViewData["Month"] = month;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payroll report");
                return View(new List<dynamic>());
            }
        }

        [Authorize(Policy = "Permission:*")]
        public async Task<IActionResult> GeneratedReports(string reportType, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var reports = await _reportService.GetGeneratedReportsAsync(reportType, startDate, endDate);

                ViewData["ReportType"] = reportType;
                ViewData["StartDate"] = startDate;
                ViewData["EndDate"] = endDate;

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading generated reports");
                return View(new List<GeneratedReport>());
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> ScheduleReport()
        {
            try
            {
                var schedules = await _reportService.GetReportSchedulesAsync(true);
                return View(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report schedules");
                return View(new List<ReportSchedule>());
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> ScheduleReport(ReportSchedule schedule)
        {
            try
            {
                var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                schedule.CreatedBy = userId;

                var result = await _reportService.CreateReportScheduleAsync(schedule);

                return RedirectToAction(nameof(ScheduleReport)).WithSuccessMessage("Report schedule created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report schedule");
                ModelState.AddModelError("", "Error creating report schedule");
                return View(schedule);
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> DeleteReport(int id)
        {
            try
            {
                var success = await _reportService.DeleteGeneratedReportAsync(id);
                if (success)
                {
                    return Ok("Report deleted successfully");
                }
                return BadRequest("Report not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report");
                return BadRequest("Error deleting report");
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
            {
                var success = await _reportService.DeleteReportScheduleAsync(id);
                if (success)
                {
                    return Ok("Report schedule deleted successfully");
                }
                return BadRequest("Schedule not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report schedule");
                return BadRequest("Error deleting report schedule");
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> Builder(string? reportType)
        {
            var templates = string.IsNullOrWhiteSpace(reportType)
                ? await _reportTemplateService.GetAllTemplatesAsync()
                : await _reportTemplateService.GetTemplatesByTypeAsync(reportType);

            ViewData["ReportType"] = reportType;
            ViewData["AvailableTypes"] = await _reportTemplateService.GetAvailableReportTypesAsync();
            return View(templates);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTemplate(string name, string reportType, string description)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(reportType))
            {
                return RedirectToAction(nameof(Builder)).WithSuccessMessage("Template name and report type are required");
            }

            var template = new ReportTemplate
            {
                Name = name.Trim(),
                ReportType = reportType.Trim(),
                Description = description?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            var created = await _reportTemplateService.CreateTemplateAsync(template);
            return RedirectToAction(nameof(Design), new { id = created.Id });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> Design(int id)
        {
            var template = await _reportTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return RedirectToAction(nameof(Builder)).WithSuccessMessage("Template not found");
            }

            return View(template);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTemplate(ReportTemplate formTemplate)
        {
            var existing = await _reportTemplateService.GetTemplateByIdAsync(formTemplate.Id);
            if (existing == null)
            {
                return RedirectToAction(nameof(Builder)).WithSuccessMessage("Template not found");
            }

            existing.Name = formTemplate.Name;
            existing.ReportType = formTemplate.ReportType;
            existing.Description = formTemplate.Description;
            existing.IsActive = formTemplate.IsActive;
            existing.IsDefault = formTemplate.IsDefault;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = User.Identity?.Name;

            await _reportTemplateService.UpdateTemplateAsync(existing);
            return RedirectToAction(nameof(Design), new { id = existing.Id }).WithSuccessMessage("Template updated successfully");
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(int templateId, ReportField field)
        {
            if (string.IsNullOrWhiteSpace(field.FieldName))
            {
                return RedirectToAction(nameof(Design), new { id = templateId }).WithSuccessMessage("Field name is required");
            }

            field.ColumnName = string.IsNullOrWhiteSpace(field.ColumnName) ? field.FieldName : field.ColumnName;

            await _reportTemplateService.AddFieldAsync(templateId, field);
            return RedirectToAction(nameof(Design), new { id = templateId }).WithSuccessMessage("Field added successfully");
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveField(int templateId, int fieldId)
        {
            await _reportTemplateService.RemoveFieldAsync(fieldId);
            return RedirectToAction(nameof(Design), new { id = templateId }).WithSuccessMessage("Field removed successfully");
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloneTemplate(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return RedirectToAction(nameof(Builder)).WithSuccessMessage("New template name is required");
            }

            await _reportTemplateService.CloneTemplateAsync(id, newName.Trim());
            return RedirectToAction(nameof(Builder)).WithSuccessMessage("Template cloned successfully");
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var deleted = await _reportTemplateService.DeleteTemplateAsync(id);
            return RedirectToAction(nameof(Builder))
                .WithSuccessMessage(deleted ? "Template deleted successfully" : "Template not found");
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var template = await _reportTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return RedirectToAction(nameof(Builder)).WithSuccessMessage("Template not found");
            }

            var result = await _reportTemplateService.ExecuteSavedReportAsync(id);
            ViewData["Template"] = template;
            return View(result);
        }
    }

    // Extension method for redirect with message (if not already defined)
    public static class ControllerExtensions
    {
        public static RedirectToActionResult WithSuccessMessage(this RedirectToActionResult redirect, string message)
        {
            // This would be implemented with TempData in actual usage
            return redirect;
        }
    }
}
