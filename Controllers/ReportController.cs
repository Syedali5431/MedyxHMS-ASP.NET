using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Purpose: Contains application code for ReportController and its related runtime behavior.
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

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
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

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
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

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
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

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
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

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
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

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
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
        [HttpGet]
        public async Task<IActionResult> EditReport(int id)
        {
            var report = await _reportService.GetGeneratedReportByIdAsync(id);
            if (report == null)
            {
                return RedirectToAction(nameof(GeneratedReports)).WithSuccessMessage("Report not found");
            }

            return View(report);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReport(GeneratedReport formReport)
        {
            if (!ModelState.IsValid)
            {
                return View(formReport);
            }

            var existing = await _reportService.GetGeneratedReportByIdAsync(formReport.Id);
            if (existing == null)
            {
                return RedirectToAction(nameof(GeneratedReports)).WithSuccessMessage("Report not found");
            }

            existing.ReportName = formReport.ReportName;
            existing.ReportType = formReport.ReportType;
            existing.Description = formReport.Description;
            existing.FromDate = formReport.FromDate;
            existing.ToDate = formReport.ToDate;
            existing.DepartmentId = formReport.DepartmentId;
            existing.FileFormat = formReport.FileFormat;
            existing.Status = formReport.Status;

            await _reportService.SaveReportAsync(existing);
            return RedirectToAction(nameof(GeneratedReports)).WithSuccessMessage("Report updated successfully");
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportLegacyPhpReports()
        {
            var existing = await _reportTemplateService.GetAllTemplatesAsync();
            var existingKeys = existing
                .Select(t => BuildLegacyTemplateName(t.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var createdCount = 0;
            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "System";

            foreach (var report in LegacyPhpReportCatalog)
            {
                var templateName = BuildLegacyTemplateName(report.Name);
                if (existingKeys.Contains(templateName))
                    continue;

                var template = new ReportTemplate
                {
                    Name = templateName,
                    ReportType = report.Type,
                    Description = $"Migrated from PHP report view: {report.SourcePath}",
                    IsActive = true,
                    IsDefault = false,
                    CreatedBy = actor,
                    CreatedDate = now
                };

                await _reportTemplateService.CreateTemplateAsync(template);
                existingKeys.Add(templateName);
                createdCount++;
            }

            return RedirectToAction(nameof(Builder), new { reportType = "LegacyPHP" })
                .WithSuccessMessage($"Legacy PHP report import complete. Added {createdCount} editable template(s).");
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

        private static string BuildLegacyTemplateName(string name)
        {
            var safeName = (name ?? string.Empty).Trim();
            return safeName.StartsWith("PHP:", StringComparison.OrdinalIgnoreCase)
                ? safeName
                : $"PHP: {safeName}";
        }

        private static readonly (string Name, string Type, string SourcePath)[] LegacyPhpReportCatalog =
        {
            ("Appointment Report", "LegacyPHP", "application/views/admin/appointment/appointmentReport.php"),
            ("Bill Report (Print)", "LegacyPHP", "application/views/admin/bill/_print_bill_report.php"),
            ("Birth Report", "LegacyPHP", "application/views/admin/birthordeath/birthReport.php"),
            ("Death Report", "LegacyPHP", "application/views/admin/birthordeath/deathReport.php"),
            ("Birth Report (Legacy 1)", "LegacyPHP", "application/views/admin/birthreport/birthreport.php"),
            ("Birth Report (Legacy 2)", "LegacyPHP", "application/views/admin/birthreport/birth_report.php"),
            ("Birth Print Bill", "LegacyPHP", "application/views/admin/birthreport/printBill.php"),
            ("Birth Search Report", "LegacyPHP", "application/views/admin/birthreport/search.php"),
            ("Blood Donor Report", "LegacyPHP", "application/views/admin/bloodbank/blooddonorreport.php"),
            ("Blood Issue Report", "LegacyPHP", "application/views/admin/bloodbank/bloodissuereport.php"),
            ("Blood Component Issue Report", "LegacyPHP", "application/views/admin/bloodbank/componentissuereport.php"),
            ("Consultation Report", "LegacyPHP", "application/views/admin/conference/consult_report.php"),
            ("Meeting Report", "LegacyPHP", "application/views/admin/conference/meeting_report.php"),
            ("Death Report (Legacy 1)", "LegacyPHP", "application/views/admin/deathreport/deathreport.php"),
            ("Death Report (Legacy 2)", "LegacyPHP", "application/views/admin/deathreport/death_report.php"),
            ("Death Print Bill", "LegacyPHP", "application/views/admin/deathreport/printBill.php"),
            ("Death Search Report", "LegacyPHP", "application/views/admin/deathreport/search.php"),
            ("Group Expense Report", "LegacyPHP", "application/views/admin/expense/groupexpenseReport.php"),
            ("Medicine Expense Report", "LegacyPHP", "application/views/admin/expmedicine/expmedicinereport.php"),
            ("All Transactions Report", "LegacyPHP", "application/views/admin/income/alltransactionReport.php"),
            ("Group Income Report", "LegacyPHP", "application/views/admin/income/groupincomeReport.php"),
            ("Transaction Report", "LegacyPHP", "application/views/admin/income/transactionReport.php"),
            ("Issue Inventory Report", "LegacyPHP", "application/views/admin/issueitem/issueinventoryreport.php"),
            ("Add Item Report", "LegacyPHP", "application/views/admin/item/additemreport.php"),
            ("Item Report", "LegacyPHP", "application/views/admin/item/itemreport.php"),
            ("Operation Theatre Report", "LegacyPHP", "application/views/admin/operationtheatre/otReport.php"),
            ("Pathology Report", "LegacyPHP", "application/views/admin/pathology/pathologyReport.php"),
            ("Pathology Print Report", "LegacyPHP", "application/views/admin/pathology/printReport.php"),
            ("Pathology Report Detail", "LegacyPHP", "application/views/admin/pathology/reportDetail.php"),
            ("Pathology Report Details Partial", "LegacyPHP", "application/views/admin/pathology/_getPathologyReportDetails.php"),
            ("Pathology Patient Print Partial", "LegacyPHP", "application/views/admin/pathology/_printPatientReportDetail.php"),
            ("Discharge Patient Report", "LegacyPHP", "application/views/admin/patient/dischargePatientReport.php"),
            ("IPD Report", "LegacyPHP", "application/views/admin/patient/ipdReport.php"),
            ("IPD Balance Report", "LegacyPHP", "application/views/admin/patient/ipdReportbalance.php"),
            ("OPD Discharge Report", "LegacyPHP", "application/views/admin/patient/opddischargepatientReport.php"),
            ("OPD Report", "LegacyPHP", "application/views/admin/patient/opdReport.php"),
            ("OPD Balance Report", "LegacyPHP", "application/views/admin/patient/opdReportbalance.php"),
            ("Patient Bill Report", "LegacyPHP", "application/views/admin/patient/patientBillReport.php"),
            ("Patient Credential Report", "LegacyPHP", "application/views/admin/patient/patientcredentialreport.php"),
            ("Patient Visit Report", "LegacyPHP", "application/views/admin/patient/patientVisitReport.php"),
            ("Patient Visit Report Partial", "LegacyPHP", "application/views/admin/patient/_patientvisitreport.php"),
            ("Visit Report Print Bill", "LegacyPHP", "application/views/admin/patient/visitreport/printBill.php"),
            ("Visit Bill Detail Partial", "LegacyPHP", "application/views/admin/patient/visitreport/_getBillDetails.php"),
            ("Visit Blood Issue Detail Partial", "LegacyPHP", "application/views/admin/patient/visitreport/_getBloodIssueDetail.php"),
            ("Visit Component Issue Detail Partial", "LegacyPHP", "application/views/admin/patient/visitreport/_getcomponentIssueDetail.php"),
            ("Visit Pathology Detail Partial", "LegacyPHP", "application/views/admin/patient/visitreport/_getPatientPathologyDetails.php"),
            ("Visit Radiology Detail Partial", "LegacyPHP", "application/views/admin/patient/visitreport/_getPatientRadiologyDetails.php"),
            ("Payroll Report", "LegacyPHP", "application/views/admin/payroll/payrollreport.php"),
            ("Pharmacy Bill Report", "LegacyPHP", "application/views/admin/pharmacy/billReport.php"),
            ("Radiology Print Report", "LegacyPHP", "application/views/admin/radio/printReport.php"),
            ("Radiology Report", "LegacyPHP", "application/views/admin/radio/radiologyReport.php"),
            ("Radiology Report Detail", "LegacyPHP", "application/views/admin/radio/reportDetail.php"),
            ("Radiology Report Details Partial", "LegacyPHP", "application/views/admin/radio/_getRadiologyReportDetails.php"),
            ("Radiology Patient Print Partial", "LegacyPHP", "application/views/admin/radio/_printPatientReportDetail.php"),
            ("Referral Report", "LegacyPHP", "application/views/admin/referral/report.php"),
            ("Staff Attendance Report", "LegacyPHP", "application/views/admin/staffattendance/attendancereport.php"),
            ("TPA Report", "LegacyPHP", "application/views/admin/tpamanagement/tpareport.php"),
            ("Transaction Report (Admin)", "LegacyPHP", "application/views/admin/transaction/transactionreport.php"),
            ("Ambulance Report", "LegacyPHP", "application/views/admin/vehicle/ambulancereport.php"),
            ("Patient Pathology Print Partial", "LegacyPHP", "application/views/patient/pathology/_printPatientReportDetail.php"),
            ("Patient Radiology Print Partial", "LegacyPHP", "application/views/patient/radiology/_printPatientReportDetail.php")
        };
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
