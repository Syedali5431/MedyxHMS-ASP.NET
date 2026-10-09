using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private static readonly (string Name, string Type, string Description)[] CertificateTemplates = new[]
        {
            ("Birth Certificate", "Certificate", "Birth certificate template for Report Editor integration"),
            ("Death Certificate", "Certificate", "Death certificate template for Report Editor integration")
        };

        private readonly IReportService _reportService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly IReportCatalogVisibilityService _reportCatalogVisibilityService;
        private readonly IExportService _exportService;
        private readonly IReportEngine _reportEngine;
        private readonly INetworkPrintService _networkPrint;
        private readonly IHospitalContext _hospitalContext;
        private readonly IAuditService _audit;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            IReportTemplateService reportTemplateService,
            IReportCatalogVisibilityService reportCatalogVisibilityService,
            IExportService exportService,
            IReportEngine reportEngine,
            INetworkPrintService networkPrint,
            IHospitalContext hospitalContext,
            IAuditService audit,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _reportTemplateService = reportTemplateService;
            _reportCatalogVisibilityService = reportCatalogVisibilityService;
            _exportService = exportService;
            _reportEngine = reportEngine;
            _networkPrint = networkPrint;
            _hospitalContext = hospitalContext;
            _audit = audit;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,SuperAdmin,Accountant,Doctor,Nurse")]
        public async Task<IActionResult> Index(string? reportKey, DateTime? reportDate, DateTime? startDate, DateTime? endDate, string? month, string? staffId)
        {
            var canManageTemplates = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var items = await GetVisibleReportsAsync();

            var selected = string.IsNullOrWhiteSpace(reportKey)
                ? null
                : items.FirstOrDefault(item => item.Key.Equals(reportKey, StringComparison.OrdinalIgnoreCase));

            await EnsureCertificateTemplatesRegistered();

            var templates = await _reportTemplateService.GetAllTemplatesAsync();
            var legacyTemplates = templates
                .Where(t => string.Equals(t.ReportType, "LegacyPHP", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .ToList();

            var matchingTemplate = selected?.TemplateLookupName == null
                ? null
                : legacyTemplates.FirstOrDefault(t => t.Name.Equals(selected.TemplateLookupName, StringComparison.OrdinalIgnoreCase));

            var vm = new ReportsWorkspaceViewModel
            {
                Items = items,
                SelectedReport = selected,
                CanManageTemplates = canManageTemplates,
                ImportedLegacyTemplateCount = legacyTemplates.Count,
                MatchingTemplateId = matchingTemplate?.Id,
                MatchingTemplateName = matchingTemplate?.Name,
                PreviewableTemplates = legacyTemplates
                    .Take(12)
                    .Select(t => new ReportTemplateOption { Id = t.Id, Name = t.Name })
                    .ToList()
            };

            // Data reports are built from live data and shown in the detail panel (same figures as the PDF / Excel export).
            if (selected != null && _reportEngine.GetDefinition(selected.Key) != null)
            {
                try
                {
                    var document = await _reportEngine.BuildAsync(selected.Key, Parameters(reportDate, startDate, endDate, month, staffId));
                    ViewData["ReportDocument"] = document;
                    ViewData["ReportFormAction"] = Url.Action(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Report {Key} could not be built", selected.Key);
                    ViewData["ReportError"] = "The report could not be built. Please try again or choose another period.";
                }
            }

            return View(vm);
        }

        // Old links to the converted reports open them in the workspace.
        [HttpGet]
        public IActionResult DailyTransactionReport(DateTime? reportDate) => RedirectToAction(nameof(Index), new { reportKey = "R1", reportDate });

        [HttpGet]
        public IActionResult AllTransactionReport(DateTime? startDate, DateTime? endDate) => RedirectToAction(nameof(Index), new { reportKey = "R2", startDate, endDate });

        [HttpGet]
        public IActionResult AppointmentReport(DateTime? startDate, DateTime? endDate) => RedirectToAction(nameof(Index), new { reportKey = "R3", startDate, endDate });

        [HttpGet]
        public IActionResult OPDLegacyReport(DateTime? startDate, DateTime? endDate) => RedirectToAction(nameof(Index), new { reportKey = "R4", startDate, endDate });

        [HttpGet]
        public IActionResult IPDLegacyReport(DateTime? startDate, DateTime? endDate) => RedirectToAction(nameof(Index), new { reportKey = "R5", startDate, endDate });

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public IActionResult ExportLegacyReport(string reportKey, string format = "pdf", DateTime? reportDate = null, DateTime? startDate = null, DateTime? endDate = null)
            => RedirectToAction(nameof(Export), new { reportKey, format, reportDate, startDate, endDate });

        // GET /Report/Export?reportKey=R6&format=pdf|excel&startDate=…&endDate=… – the report as a PDF or Excel file.
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant,Doctor,Nurse")]
        public async Task<IActionResult> Export(string reportKey, string format = "pdf", DateTime? reportDate = null, DateTime? startDate = null, DateTime? endDate = null, string? month = null, string? staffId = null)
        {
            if (!await CanSeeReportAsync(reportKey))
            {
                return NotFound();
            }

            var doc = await _reportEngine.BuildAsync(reportKey, Parameters(reportDate, startDate, endDate, month, staffId));
            if (doc == null)
            {
                return NotFound();
            }

            var excel = string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase);
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "EXPORT", "Report", doc.Key, null, $"{doc.Title} ({doc.PeriodText}) as {(excel ? "Excel" : "PDF")}");
            return excel
                ? File(_exportService.BuildReportExcel(doc), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", doc.FileName() + ".xlsx")
                : File(_exportService.BuildReportPdf(doc), "application/pdf", doc.FileName() + ".pdf");
        }

        // POST /Report/PrintToPrinter – the report sent straight to a network document printer (Settings → Printers).
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant,Doctor,Nurse")]
        public async Task<IActionResult> PrintToPrinter(string reportKey, int printerId, string? returnUrl, DateTime? reportDate = null, DateTime? startDate = null, DateTime? endDate = null, string? month = null, string? staffId = null)
        {
            var back = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index), new { reportKey })!;
            if (!await CanSeeReportAsync(reportKey))
            {
                return NotFound();
            }

            var doc = await _reportEngine.BuildAsync(reportKey, Parameters(reportDate, startDate, endDate, month, staffId));
            var hospitalId = _hospitalContext.ActiveHospitalId ?? _hospitalContext.HospitalIdForNewRecords;
            var printer = (await _networkPrint.GetPrintersForHospitalAsync(hospitalId, Printer.TypeDocument)).FirstOrDefault(p => p.Id == printerId);
            if (doc == null || printer == null)
            {
                TempData["ErrorMessage"] = "That printer is not available for your hospital.";
                return LocalRedirect(back);
            }

            var section = doc.Sections.FirstOrDefault() ?? new ReportSection();
            var data = _networkPrint.BuildDocument(printer, _exportService.BuildReportPdf(doc), $"{doc.Title} - {doc.PeriodText}",
                section.Columns.Select(c => c.Header).ToList(),
                section.Rows.Select(r => (IReadOnlyList<string>)section.Columns.Select((c, i) => doc.Format(i < r.Length ? r[i] : null, c.Kind)).ToList()).ToList());
            var result = await _networkPrint.SendAsync(printer, data, HttpContext.RequestAborted);
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "PRINT", "Report", doc.Key, null, $"{doc.Title} → {printer.Name}: {result.Message}");
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? $"{doc.Title} sent to {printer.Name}." : result.Message;
            return LocalRedirect(back);
        }

        // ── Standalone report pages (also listed in the Reports workspace) ──

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public Task<IActionResult> DepartmentReport(DateTime? startDate, DateTime? endDate) => ReportPageAsync("R41", Parameters(null, startDate, endDate, null, null));

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public Task<IActionResult> FinancialReport(DateTime? startDate, DateTime? endDate) => ReportPageAsync("R42", Parameters(null, startDate, endDate, null, null));

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public Task<IActionResult> OccupancyReport(DateTime? date, DateTime? reportDate) => ReportPageAsync("R43", Parameters(reportDate ?? date, null, null, null, null));

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public Task<IActionResult> StaffReport(string? staffId, DateTime? startDate, DateTime? endDate) => ReportPageAsync("R44", Parameters(null, startDate, endDate, null, staffId));

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public Task<IActionResult> PayrollReport(string? month) => ReportPageAsync("R28", Parameters(null, null, null, month, null));

        private async Task<IActionResult> ReportPageAsync(string key, ReportParameters parameters)
        {
            var doc = await _reportEngine.BuildAsync(key, parameters);
            if (doc == null) return NotFound();
            ViewData["ReportFormAction"] = Request.Path.Value;
            return View("ReportPage", doc);
        }

        private static ReportParameters Parameters(DateTime? reportDate, DateTime? startDate, DateTime? endDate, string? month, string? staffId)
        {
            DateTime? monthValue = null;
            if (!string.IsNullOrWhiteSpace(month)
                && DateTime.TryParseExact(month.Trim(), new[] { "yyyy-MM", "yyyy-MM-dd", "MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                monthValue = parsed;
            }

            return new ReportParameters { Date = reportDate, StartDate = startDate, EndDate = endDate, Month = monthValue, StaffId = staffId };
        }

        private async Task<List<ReportCatalogItem>> GetVisibleReportsAsync()
        {
            var canManageTemplates = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var userRoles = User.Claims
                .Where(c => c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (await _reportCatalogVisibilityService.GetVisibleItemsForUserAsync(
                canManageTemplates,
                User.IsInRole("SuperAdmin"),
                userRoles,
                includeInactiveForSuperAdmin: false)).ToList();
        }

        /// <summary>Reports hidden from the user's roles (System Management → Report Management) cannot be exported either.</summary>
        private async Task<bool> CanSeeReportAsync(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || _reportEngine.GetDefinition(key) == null) return false;
            var items = await GetVisibleReportsAsync();
            return items.Any(i => i.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Builder(string? reportType)
        {
            var templates = await _reportTemplateService.GetAllTemplatesAsync();
            var filtered = string.IsNullOrWhiteSpace(reportType)
                ? templates
                : templates.Where(t => t.ReportType.Equals(reportType, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewData["ReportType"] = reportType ?? string.Empty;
            ViewData["AvailableTypes"] = templates
                .Select(t => t.ReportType)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();

            return View(filtered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateTemplate(string name, string reportType, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(reportType))
            {
                TempData["ErrorMessage"] = "Template name and report type are required.";
                return RedirectToAction(nameof(Builder));
            }

            var template = new ReportTemplate
            {
                Name = name.Trim(),
                ReportType = reportType.Trim(),
                Description = description?.Trim() ?? string.Empty,
                CreatedDate = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "System",
                IsActive = true
            };

            await _reportTemplateService.CreateTemplateAsync(template);
            TempData["SuccessMessage"] = "Template created successfully.";
            return RedirectToAction(nameof(Builder));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var removed = await _reportTemplateService.DeleteTemplateAsync(id);
            TempData[removed ? "SuccessMessage" : "ErrorMessage"] = removed
                ? "Template deleted successfully."
                : "Unable to delete template.";
            return RedirectToAction(nameof(Builder));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CloneTemplate(int templateId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["ErrorMessage"] = "Clone name is required.";
                return RedirectToAction(nameof(Builder));
            }

            await _reportTemplateService.CloneTemplateAsync(templateId, newName.Trim());
            TempData["SuccessMessage"] = "Template cloned successfully.";
            return RedirectToAction(nameof(Builder));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Design(int id)
        {
            var template = await _reportTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> SaveTemplate(ReportTemplate model)
        {
            var existing = await _reportTemplateService.GetTemplateByIdAsync(model.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.ReportType = model.ReportType;
            existing.IsActive = model.IsActive;
            existing.IsDefault = model.IsDefault;
            existing.ModifiedDate = DateTime.Now;
            existing.ModifiedBy = User.Identity?.Name ?? "System";

            await _reportTemplateService.UpdateTemplateAsync(existing);
            TempData["SuccessMessage"] = "Template updated successfully.";
            return RedirectToAction(nameof(Design), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddField(int templateId, ReportField field)
        {
            field.TemplateId = templateId;
            if (string.IsNullOrWhiteSpace(field.FieldName))
            {
                TempData["ErrorMessage"] = "Field name is required.";
                return RedirectToAction(nameof(Design), new { id = templateId });
            }

            field.ColumnName = string.IsNullOrWhiteSpace(field.ColumnName) ? field.FieldName : field.ColumnName;
            await _reportTemplateService.AddFieldAsync(templateId, field);
            TempData["SuccessMessage"] = "Field added.";
            return RedirectToAction(nameof(Design), new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RemoveField(int templateId, int fieldId)
        {
            var removed = await _reportTemplateService.RemoveFieldAsync(fieldId);
            TempData[removed ? "SuccessMessage" : "ErrorMessage"] = removed ? "Field removed." : "Field not found.";
            return RedirectToAction(nameof(Design), new { id = templateId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Preview(int id)
        {
            if (await _reportTemplateService.GetTemplateByIdAsync(id) == null)
            {
                return NotFound();
            }

            var result = await _reportTemplateService.ExecuteSavedReportAsync(id);
            return View(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GeneratedReports(string? reportType, DateTime? startDate, DateTime? endDate)
        {
            var model = await _reportService.GetGeneratedReportsAsync(reportType, startDate, endDate);
            ViewData["ReportType"] = reportType;
            ViewData["StartDate"] = startDate;
            ViewData["EndDate"] = endDate;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> EditReport(int id)
        {
            var model = await _reportService.GetGeneratedReportByIdAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> EditReport(GeneratedReport model)
        {
            await _reportService.SaveReportAsync(model);
            TempData["SuccessMessage"] = "Report updated successfully.";
            return RedirectToAction(nameof(GeneratedReports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var deleted = await _reportService.DeleteGeneratedReportAsync(id);
            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                ? "Report deleted successfully."
                : "Unable to delete report.";
            return RedirectToAction(nameof(GeneratedReports));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ScheduleReport()
        {
            var model = await _reportService.GetReportSchedulesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ScheduleReport(ReportSchedule schedule)
        {
            // CreatedBy is a foreign key to Staff.Id, and a staff record shares its user's Id
            // (the login name used previously never matched, so every save failed).
            schedule.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            schedule.CreatedDate = DateTime.Now;
            try
            {
                await _reportService.CreateReportScheduleAsync(schedule);
                TempData["SuccessMessage"] = "Schedule created successfully.";
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to save report schedule {ReportName}", schedule.ReportName);
                TempData["ErrorMessage"] = "The schedule could not be saved. Your account must be linked to a staff record.";
            }
            return RedirectToAction(nameof(ScheduleReport));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var deleted = await _reportService.DeleteReportScheduleAsync(id);
            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                ? "Schedule deleted."
                : "Unable to delete schedule.";
            return RedirectToAction(nameof(ScheduleReport));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ImportLegacyPhpReports()
        {
            var existing = await _reportTemplateService.GetTemplatesByTypeAsync("LegacyPHP");
            var existingNames = existing.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var now = DateTime.Now;
            var actor = User?.Identity?.Name ?? "System";
            var added = 0;

            foreach (var item in ReportCatalogRegistry.All.Where(r => r.IsLegacy))
            {
                var name = string.IsNullOrWhiteSpace(item.TemplateLookupName)
                    ? $"PHP: {item.Name}"
                    : item.TemplateLookupName!;

                if (existingNames.Contains(name))
                {
                    continue;
                }

                await _reportTemplateService.CreateTemplateAsync(new ReportTemplate
                {
                    Name = name,
                    ReportType = "LegacyPHP",
                    Description = item.Summary,
                    IsActive = true,
                    IsDefault = false,
                    CreatedBy = actor,
                    CreatedDate = now
                });

                existingNames.Add(name);
                added++;
            }

            TempData["SuccessMessage"] = added == 0
                ? "All legacy templates are already imported."
                : $"Imported {added} legacy template(s).";

            return RedirectToAction(nameof(Index), new { reportKey = "R48" });
        }

        private async Task EnsureCertificateTemplatesRegistered()
        {
            var existing = await _reportTemplateService.GetTemplatesByTypeAsync("Certificate");
            var existingNames = existing.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;
            var actor = User?.Identity?.Name ?? "System";

            foreach (var tpl in CertificateTemplates)
            {
                if (existingNames.Contains(tpl.Name))
                {
                    continue;
                }

                await _reportTemplateService.CreateTemplateAsync(new ReportTemplate
                {
                    Name = tpl.Name,
                    ReportType = tpl.Type,
                    Description = tpl.Description,
                    IsActive = true,
                    IsDefault = false,
                    CreatedBy = actor,
                    CreatedDate = now
                });
            }
        }
    }
}
