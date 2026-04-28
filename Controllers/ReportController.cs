using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            IReportTemplateService reportTemplateService,
            IReportCatalogVisibilityService reportCatalogVisibilityService,
            IExportService exportService,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _reportTemplateService = reportTemplateService;
            _reportCatalogVisibilityService = reportCatalogVisibilityService;
            _exportService = exportService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> Index(string? reportKey, DateTime? reportDate, DateTime? startDate, DateTime? endDate)
        {
            var canManageTemplates = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var userRoles = User.Claims
                .Where(c => c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var items = await _reportCatalogVisibilityService.GetVisibleItemsForUserAsync(
                canManageTemplates,
                isSuperAdmin,
                userRoles,
                includeInactiveForSuperAdmin: false);

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

            if (selected != null)
            {
                var now = DateTime.UtcNow;
                var reportDateValue = reportDate?.Date ?? now.Date;
                var startDateValue = startDate?.Date ?? now.AddMonths(-1).Date;
                var endDateValue = endDate?.Date ?? now.Date;

                try
                {
                    switch (selected.Key)
                    {
                        case "R1":
                            ViewData["R1Model"] = await _reportService.GenerateDailyTransactionReportAsync(reportDateValue);
                            break;
                        case "R2":
                            ViewData["R2Model"] = await _reportService.GenerateAllTransactionReportAsync(startDateValue, endDateValue);
                            break;
                        case "R3":
                            ViewData["R3Model"] = await _reportService.GenerateAppointmentReportAsync(startDateValue, endDateValue);
                            break;
                        case "R4":
                            ViewData["R4Model"] = await _reportService.GenerateOPDReportAsync(startDateValue, endDateValue);
                            break;
                        case "R5":
                            ViewData["R5Model"] = await _reportService.GenerateIPDReportAsync(startDateValue, endDateValue);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error pre-loading report model for key {Key} in workspace", selected.Key);
                    ViewData["R1Model"] = new DailyTransactionReportViewModel { ReportDate = reportDateValue };
                    ViewData["R2Model"] = new AllTransactionReportViewModel { StartDate = startDateValue, EndDate = endDateValue };
                    ViewData["R3Model"] = new AppointmentReportViewModel { StartDate = startDateValue, EndDate = endDateValue };
                    ViewData["R4Model"] = new OPDReportViewModel { StartDate = startDateValue, EndDate = endDateValue };
                    ViewData["R5Model"] = new IPDReportViewModel { StartDate = startDateValue, EndDate = endDateValue };
                }
            }

            return View(vm);
        }

        [HttpGet]
        public IActionResult DailyTransactionReport(DateTime? reportDate)
        {
            return RedirectToAction(nameof(Index), new { reportKey = "R1", reportDate });
        }

        [HttpGet]
        public IActionResult AllTransactionReport(DateTime? startDate, DateTime? endDate)
        {
            return RedirectToAction(nameof(Index), new { reportKey = "R2", startDate, endDate });
        }

        [HttpGet]
        public IActionResult AppointmentReport(DateTime? startDate, DateTime? endDate)
        {
            return RedirectToAction(nameof(Index), new { reportKey = "R3", startDate, endDate });
        }

        [HttpGet]
        public IActionResult OPDLegacyReport(DateTime? startDate, DateTime? endDate)
        {
            return RedirectToAction(nameof(Index), new { reportKey = "R4", startDate, endDate });
        }

        [HttpGet]
        public IActionResult IPDLegacyReport(DateTime? startDate, DateTime? endDate)
        {
            return RedirectToAction(nameof(Index), new { reportKey = "R5", startDate, endDate });
        }

        [HttpGet]
        public async Task<IActionResult> ExportLegacyReport(string reportKey, string format = "pdf", DateTime? reportDate = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var now = DateTime.UtcNow;
            var normalizedFormat = (format ?? "pdf").Trim().ToLowerInvariant();

            string title;
            IReadOnlyList<string> headers;
            IReadOnlyList<IReadOnlyList<string>> rows;

            switch ((reportKey ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "R1":
                {
                    var model = await _reportService.GenerateDailyTransactionReportAsync(reportDate?.Date ?? now.Date);
                    title = "Daily Transaction Report";
                    headers = new[] { "Transaction ID", "Type", "Amount", "Description", "Reference", "Time", "Processed By", "Status" };
                    rows = model.TransactionData.Select(tx =>
                    {
                        var type = tx.GetType();
                        return (IReadOnlyList<string>)new[]
                        {
                            type.GetProperty("TransactionId")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("TransactionType")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("Amount")?.GetValue(tx)?.ToString() ?? "0",
                            type.GetProperty("Description")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("ReferenceNumber")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("TransactionDate")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("ProcessedBy")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("Status")?.GetValue(tx)?.ToString() ?? "-"
                        };
                    }).ToList();
                    break;
                }
                case "R2":
                {
                    var model = await _reportService.GenerateAllTransactionReportAsync(startDate?.Date ?? now.AddMonths(-1).Date, endDate?.Date ?? now.Date);
                    title = "All Transaction Report";
                    headers = new[] { "Transaction ID", "Type", "Amount", "Description", "Reference", "Date", "Processed By", "Status" };
                    rows = model.TransactionData.Select(tx =>
                    {
                        var type = tx.GetType();
                        return (IReadOnlyList<string>)new[]
                        {
                            type.GetProperty("TransactionId")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("TransactionType")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("Amount")?.GetValue(tx)?.ToString() ?? "0",
                            type.GetProperty("Description")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("ReferenceNumber")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("TransactionDate")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("ProcessedBy")?.GetValue(tx)?.ToString() ?? "-",
                            type.GetProperty("Status")?.GetValue(tx)?.ToString() ?? "-"
                        };
                    }).ToList();
                    break;
                }
                case "R3":
                {
                    var model = await _reportService.GenerateAppointmentReportAsync(startDate?.Date ?? now.AddMonths(-1).Date, endDate?.Date ?? now.Date);
                    title = "Appointment Report";
                    headers = new[] { "ID", "Patient", "Doctor", "Date", "Time", "Type", "Priority", "Status" };
                    rows = model.AppointmentData.Select(item =>
                    {
                        var type = item.GetType();
                        return (IReadOnlyList<string>)new[]
                        {
                            type.GetProperty("AppointmentId")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("PatientName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("DoctorName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("AppointmentDate")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("AppointmentTime")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("AppointmentType")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("Priority")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("Status")?.GetValue(item)?.ToString() ?? "-"
                        };
                    }).ToList();
                    break;
                }
                case "R4":
                {
                    var model = await _reportService.GenerateOPDReportAsync(startDate?.Date ?? now.AddMonths(-1).Date, endDate?.Date ?? now.Date);
                    title = "OPD Report";
                    headers = new[] { "ID", "Patient", "Doctor", "Visit Date", "Diagnosis", "Consultation Fee", "Payment Status", "Created By" };
                    rows = model.OPDVisitData.Select(item =>
                    {
                        var type = item.GetType();
                        return (IReadOnlyList<string>)new[]
                        {
                            type.GetProperty("Id")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("PatientName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("DoctorName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("VisitDate")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("Diagnosis")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("ConsultationFee")?.GetValue(item)?.ToString() ?? "0",
                            type.GetProperty("PaymentStatus")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("CreatedBy")?.GetValue(item)?.ToString() ?? "-"
                        };
                    }).ToList();
                    break;
                }
                case "R5":
                {
                    var model = await _reportService.GenerateIPDReportAsync(startDate?.Date ?? now.AddMonths(-1).Date, endDate?.Date ?? now.Date);
                    title = "IPD Report";
                    headers = new[] { "ID", "Patient", "Doctor", "Ward", "Bed", "Admission Date", "Discharge Date", "LOS (days)", "Admission Type", "Status" };
                    rows = model.IPDAdmissionData.Select(item =>
                    {
                        var type = item.GetType();
                        return (IReadOnlyList<string>)new[]
                        {
                            type.GetProperty("Id")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("PatientName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("DoctorName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("WardName")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("BedNumber")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("AdmissionDate")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("DischargeDate")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("LengthOfStay")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("AdmissionType")?.GetValue(item)?.ToString() ?? "-",
                            type.GetProperty("Status")?.GetValue(item)?.ToString() ?? "-"
                        };
                    }).ToList();
                    break;
                }
                default:
                    return NotFound();
            }

            var safeDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            if (normalizedFormat == "excel" || normalizedFormat == "xlsx")
            {
                var bytes = _exportService.BuildExcel(reportKey, headers, rows);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportKey}_{safeDate}.xlsx");
            }

            var pdf = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdf, "application/pdf", $"{reportKey}_{safeDate}.pdf");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> DepartmentReport(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddMonths(-1).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var model = await _reportService.GenerateDepartmentReportAsync(null, start, end);
            ViewData["StartDate"] = start;
            ViewData["EndDate"] = end;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> FinancialReport(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddMonths(-1).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var model = await _reportService.GenerateFinancialReportAsync(start, end);
            ViewData["StartDate"] = start;
            ViewData["EndDate"] = end;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> OccupancyReport(DateTime? date)
        {
            var reportDate = date?.Date ?? DateTime.UtcNow.Date;
            var model = await _reportService.GenerateOccupancyReportAsync(reportDate);
            var avg = await _reportService.GetAverageOccupancyRateAsync(reportDate.AddDays(-29), reportDate);
            ViewData["Date"] = reportDate;
            ViewData["AverageOccupancyRate"] = avg;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> StaffReport(string? staffId, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddMonths(-1).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var model = await _reportService.GenerateStaffAttendanceReportAsync(staffId ?? string.Empty, start, end);
            ViewData["StaffId"] = staffId ?? string.Empty;
            ViewData["StartDate"] = start;
            ViewData["EndDate"] = end;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Accountant")]
        public async Task<IActionResult> PayrollReport(DateTime? month)
        {
            var m = month?.Date ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var model = await _reportService.GeneratePayrollReportAsync(m);
            ViewData["PayrollMonth"] = m;
            return View(model);
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
                CreatedDate = DateTime.UtcNow,
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
            existing.ModifiedDate = DateTime.UtcNow;
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
            schedule.CreatedBy = User.Identity?.Name ?? "System";
            schedule.CreatedDate = DateTime.UtcNow;
            await _reportService.CreateReportScheduleAsync(schedule);
            TempData["SuccessMessage"] = "Schedule created successfully.";
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

            var now = DateTime.UtcNow;
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
            var now = DateTime.UtcNow;
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
