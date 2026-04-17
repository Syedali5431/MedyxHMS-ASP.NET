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
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
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

        [Authorize(Policy = "Permission:*")]
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

        [Authorize(Policy = "Permission:*")]
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

        [Authorize(Policy = "Permission:*")]
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

        [Authorize(Policy = "Permission:*")]
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
