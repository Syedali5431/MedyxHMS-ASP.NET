using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using MedyxHMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Pharmacist,Accountant,Receptionist,LabTechnician,Radiologist,Staff")]
    public class SystemManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportCatalogVisibilityService _reportCatalogVisibilityService;
        private readonly IReportService _reportService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly IExportService _exportService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<SystemManagementController> _logger;
        private readonly IWebHostEnvironment _env;
        private static readonly IReadOnlyList<ThemeOptionViewModel> StaffThemes = new List<ThemeOptionViewModel>
        {
            new() { ThemeId = "dark", Name = "Dark", Description = "Pure black workspace theme with high-contrast text and controls.", PreviewClass = "theme-dark" },
            new() { ThemeId = "light", Name = "Light", Description = "Clean white workspace theme with bright surfaces and crisp contrast.", PreviewClass = "theme-light" },
            new() { ThemeId = "sunflower", Name = "Sunflower", Description = "Warm and optimistic with bright highlights.", PreviewClass = "theme-sunflower" },
            new() { ThemeId = "snowflake", Name = "Snowflake", Description = "Clean, crisp and high-clarity clinical palette.", PreviewClass = "theme-snowflake" },
            new() { ThemeId = "ocean", Name = "Ocean", Description = "Cool blue-green tones for calm focus.", PreviewClass = "theme-ocean" },
            new() { ThemeId = "forest", Name = "Forest", Description = "Balanced green palette for long shifts.", PreviewClass = "theme-forest" },
            new() { ThemeId = "midnight", Name = "Midnight", Description = "Low-light dark scheme with high contrast text.", PreviewClass = "theme-midnight" },
            new() { ThemeId = "sunset", Name = "Sunset", Description = "Soft orange-red accent style with warm depth.", PreviewClass = "theme-sunset" },
            new() { ThemeId = "lavender", Name = "Lavender", Description = "Gentle purple tones for a relaxing workspace.", PreviewClass = "theme-lavender" },
            new() { ThemeId = "graphite", Name = "Graphite", Description = "Modern grayscale for minimal distraction.", PreviewClass = "theme-graphite" },
            new() { ThemeId = "emerald", Name = "Emerald", Description = "Vivid green with energetic accents.", PreviewClass = "theme-emerald" },
            new() { ThemeId = "peach", Name = "Peach", Description = "Soft peach and coral for a gentle look.", PreviewClass = "theme-peach" },
            new() { ThemeId = "sky", Name = "Sky", Description = "Bright sky blue for clarity and focus.", PreviewClass = "theme-sky" },
            new() { ThemeId = "rose", Name = "Rose", Description = "Elegant rose pink with subtle highlights.", PreviewClass = "theme-rose" },
            new() { ThemeId = "sand", Name = "Sand", Description = "Earthy sand and beige for a neutral palette.", PreviewClass = "theme-sand" },
            new() { ThemeId = "plum", Name = "Plum", Description = "Deep plum and violet for a bold look.", PreviewClass = "theme-plum" },
            new() { ThemeId = "aqua", Name = "Aqua", Description = "Fresh aqua and teal for a modern feel.", PreviewClass = "theme-aqua" },
            new() { ThemeId = "crimson", Name = "Crimson", Description = "Bold red tones for a strong, energetic workspace.", PreviewClass = "theme-crimson" },
            new() { ThemeId = "amber", Name = "Amber", Description = "Warm amber and golden orange for a rich palette.", PreviewClass = "theme-amber" },
            new() { ThemeId = "arctic", Name = "Arctic", Description = "Icy blue tones for a crisp, cool clinical look.", PreviewClass = "theme-arctic" },
            new() { ThemeId = "chocolate", Name = "Chocolate", Description = "Warm cocoa and golden accents for a cozy feel.", PreviewClass = "theme-chocolate" },
            new() { ThemeId = "indigo", Name = "Indigo", Description = "Deep indigo and violet for a bold, immersive look.", PreviewClass = "theme-indigo" },
            new() { ThemeId = "lime", Name = "Lime", Description = "Vibrant lime green for a fresh, energetic workspace.", PreviewClass = "theme-lime" }
        };

        public SystemManagementController(
            ApplicationDbContext context,
            IReportCatalogVisibilityService reportCatalogVisibilityService,
            IReportService reportService,
            IReportTemplateService reportTemplateService,
            IExportService exportService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<SystemManagementController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _reportCatalogVisibilityService = reportCatalogVisibilityService;
            _reportService = reportService;
            _reportTemplateService = reportTemplateService;
            _exportService = exportService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> ReportManagement(string? search)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var canSeeAdminOnly = User.IsInRole("Admin") || isSuperAdmin;

            var inactiveKeys = await _reportCatalogVisibilityService.GetInactiveKeysAsync();
            var roleMap = await _reportCatalogVisibilityService.GetReportRoleMapAsync();
            var allRoles = await _roleManager.Roles
                .Select(r => r.Name)
                .Where(r => r != null)
                .OrderBy(r => r)
                .ToListAsync();

            var catalogItems = ReportCatalogRegistry.GetVisibleItems(canSeeAdminOnly);

            var filtered = string.IsNullOrWhiteSpace(search)
                ? catalogItems
                : catalogItems
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
                    IsActive = !inactiveKeys.Contains(item.Key),
                    AssignedRoles = roleMap.TryGetValue(item.Key, out var roles)
                        ? roles
                        : Array.Empty<string>()
                })
                .ToList();

            var vm = new SystemManagementReportListViewModel
            {
                IsSuperAdmin = isSuperAdmin,
                SearchTerm = search ?? string.Empty,
                TotalReports = rows.Count,
                ActiveReports = rows.Count(r => r.IsActive),
                AvailableRoles = allRoles,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetReportRoles(string reportKey, List<string>? roles, string? returnUrl)
        {
            var updated = await _reportCatalogVisibilityService.SetReportRolesAsync(reportKey, roles ?? new List<string>());

            if (!updated)
            {
                _logger.LogWarning("Failed to update report roles. Key: {ReportKey}", reportKey);
                TempData["ErrorMessage"] = "Could not update report roles.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Report {reportKey} roles updated.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(ReportManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateReportAccess(string reportKey, bool isActive, List<string>? roles, string? returnUrl)
        {
            var normalizedRoles = roles ?? new List<string>();

            var stateUpdated = await _reportCatalogVisibilityService.SetReportActiveStateAsync(reportKey, isActive);
            var rolesUpdated = await _reportCatalogVisibilityService.SetReportRolesAsync(reportKey, normalizedRoles);

            if (!stateUpdated || !rolesUpdated)
            {
                _logger.LogWarning(
                    "Failed to update report access. Key: {ReportKey}, IsActive: {IsActive}, RolesCount: {RolesCount}",
                    reportKey,
                    isActive,
                    normalizedRoles.Count);
                TempData["ErrorMessage"] = "Could not update report access settings.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Report {reportKey} access updated successfully.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(ReportManagement));
        }

        // ── B. Create Report ──────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public IActionResult CreateReport()
        {
            return View(new CreateReportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateReport(CreateReportViewModel vm)
        {
            // Deserialise fields from hidden JSON input
            if (!string.IsNullOrWhiteSpace(vm.FieldsJson))
            {
                try
                {
                    vm.Fields = JsonSerializer.Deserialize<List<CreateReportFieldViewModel>>(vm.FieldsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch
                {
                    ModelState.AddModelError(nameof(vm.FieldsJson), "Invalid field data submitted.");
                }
            }

            // Keep only meaningful rows from the builder.
            vm.Fields = vm.Fields
                .Where(f => !string.IsNullOrWhiteSpace(f.FieldName) || !string.IsNullOrWhiteSpace(f.ColumnName))
                .ToList();

            if (vm.Fields.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.FieldsJson), "Add at least one field before saving the report.");
            }
            else
            {
                if (!vm.Fields.Any(f => f.IsVisible))
                {
                    ModelState.AddModelError(nameof(vm.FieldsJson), "At least one field must be visible.");
                }

                for (var i = 0; i < vm.Fields.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(vm.Fields[i].FieldName))
                    {
                        ModelState.AddModelError(nameof(vm.FieldsJson), $"Field name is required for row {i + 1}.");
                    }

                    if (string.IsNullOrWhiteSpace(vm.Fields[i].ColumnName))
                    {
                        vm.Fields[i].ColumnName = vm.Fields[i].FieldName;
                    }
                }
            }

            vm.FieldsJson = JsonSerializer.Serialize(vm.Fields);

            if (!ModelState.IsValid)
                return View(vm);

            // Uniqueness check
            var isUnique = await _reportService.IsReportNameUniqueAsync(vm.ReportName);
            if (!isUnique)
            {
                ModelState.AddModelError(nameof(vm.ReportName), "A report with this name already exists.");
                return View(vm);
            }

            var createdBy = User.Identity?.Name ?? "System";

            var template = new ReportTemplate
            {
                Name = vm.ReportName,
                Description = vm.Description,
                ReportType = vm.ReportType,
                CreatedBy = createdBy,
                IsActive = true,
                Fields = vm.Fields.Select((f, i) => new ReportField
                {
                    FieldName = f.FieldName,
                    ColumnName = f.ColumnName,
                    DataType = f.DataType,
                    Alignment = f.Alignment,
                    IsVisible = f.IsVisible,
                    IsSortable = f.IsSortable,
                    SortOrder = f.SortOrder > 0 ? f.SortOrder : i + 1
                }).ToList(),
                Design = new ReportDesign
                {
                    HeaderText = vm.Title,
                    ColorScheme = vm.ColorScheme,
                    PageOrientation = vm.PageOrientation,
                    ShowGridLines = vm.ShowGridLines,
                    ShowAlternatingRows = vm.ShowAlternatingRows,
                    ShowTotals = vm.ShowTotals,
                    IncludeTimestamp = vm.IncludeTimestamp,
                    CustomCss = $"--header-font:{vm.HeaderFont};--body-font:{vm.BodyFont};"
                }
            };

            var created = await _reportTemplateService.CreateTemplateAsync(template);
            var id = created.Id;
            _logger.LogInformation("Report template '{Name}' created with id {Id} by {User}.", vm.ReportName, id, createdBy);

            TempData["SuccessMessage"] = $"Report \"{vm.ReportName}\" created successfully.";
            return RedirectToAction(nameof(ReportManagement));
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> EditReport(int? templateId)
        {
            var templates = await _reportTemplateService.GetAllTemplatesAsync();
            var options = templates
                .OrderBy(t => t.Name)
                .Select(t => new ReportTemplateOptionViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    ReportType = t.ReportType,
                    IsActive = t.IsActive
                })
                .ToList();

            if (options.Count == 0)
            {
                TempData["ErrorMessage"] = "No editable reports found. Create a report first.";
                return RedirectToAction(nameof(CreateReport));
            }

            var selectedId = templateId ?? options.First().Id;
            var template = await _reportTemplateService.GetTemplateByIdAsync(selectedId);
            if (template == null)
            {
                TempData["ErrorMessage"] = "Selected report template was not found.";
                return RedirectToAction(nameof(EditReport), new { templateId = options.First().Id });
            }

            var vm = new EditReportViewModel
            {
                TemplateId = template.Id,
                ReportName = template.Name,
                Title = template.Design?.HeaderText ?? string.Empty,
                Description = template.Description,
                ReportType = template.ReportType,
                ColorScheme = template.Design?.ColorScheme ?? "default",
                PageOrientation = template.Design?.PageOrientation ?? "portrait",
                HeaderFont = ExtractFontToken(template.Design?.CustomCss, "--header-font") ?? "Arial",
                BodyFont = ExtractFontToken(template.Design?.CustomCss, "--body-font") ?? "Arial",
                ShowGridLines = template.Design?.ShowGridLines ?? true,
                ShowAlternatingRows = template.Design?.ShowAlternatingRows ?? true,
                ShowTotals = template.Design?.ShowTotals ?? false,
                IncludeTimestamp = template.Design?.IncludeTimestamp ?? true,
                Fields = template.Fields
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new CreateReportFieldViewModel
                    {
                        FieldName = f.FieldName,
                        ColumnName = f.ColumnName,
                        DataType = f.DataType,
                        Alignment = f.Alignment ?? "left",
                        IsVisible = f.IsVisible,
                        IsSortable = f.IsSortable,
                        SortOrder = f.SortOrder
                    })
                    .ToList(),
                AvailableReports = options
            };

            vm.FieldsJson = JsonSerializer.Serialize(vm.Fields);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> EditReport(EditReportViewModel vm)
        {
            var templates = await _reportTemplateService.GetAllTemplatesAsync();
            vm.AvailableReports = templates
                .OrderBy(t => t.Name)
                .Select(t => new ReportTemplateOptionViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    ReportType = t.ReportType,
                    IsActive = t.IsActive
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(vm.FieldsJson))
            {
                try
                {
                    vm.Fields = JsonSerializer.Deserialize<List<CreateReportFieldViewModel>>(vm.FieldsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch
                {
                    ModelState.AddModelError(nameof(vm.FieldsJson), "Invalid field data submitted.");
                }
            }

            vm.Fields = vm.Fields
                .Where(f => !string.IsNullOrWhiteSpace(f.FieldName) || !string.IsNullOrWhiteSpace(f.ColumnName))
                .ToList();

            if (vm.Fields.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.FieldsJson), "Add at least one field before saving the report.");
            }
            else if (!vm.Fields.Any(f => f.IsVisible))
            {
                ModelState.AddModelError(nameof(vm.FieldsJson), "At least one field must be visible.");
            }

            for (var i = 0; i < vm.Fields.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(vm.Fields[i].FieldName))
                {
                    ModelState.AddModelError(nameof(vm.FieldsJson), $"Field name is required for row {i + 1}.");
                }

                if (string.IsNullOrWhiteSpace(vm.Fields[i].ColumnName))
                {
                    vm.Fields[i].ColumnName = vm.Fields[i].FieldName;
                }
            }

            vm.FieldsJson = JsonSerializer.Serialize(vm.Fields);

            if (!ModelState.IsValid)
                return View(vm);

            var isUnique = await _reportService.IsReportNameUniqueAsync(vm.ReportName, vm.TemplateId);
            if (!isUnique)
            {
                ModelState.AddModelError(nameof(vm.ReportName), "A report with this name already exists.");
                return View(vm);
            }

            var existing = await _reportTemplateService.GetTemplateByIdAsync(vm.TemplateId);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Report template not found.";
                return RedirectToAction(nameof(EditReport));
            }

            var oldValues = JsonSerializer.Serialize(new
            {
                existing.Name,
                existing.ReportType,
                existing.Description,
                Fields = existing.Fields.OrderBy(f => f.SortOrder).Select(f => new
                {
                    f.FieldName,
                    f.ColumnName,
                    f.DataType,
                    f.Alignment,
                    f.IsVisible,
                    f.IsSortable,
                    f.SortOrder
                })
            });

            existing.Name = vm.ReportName;
            existing.ReportType = vm.ReportType;
            existing.Description = vm.Description;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = User.Identity?.Name ?? "System";

            existing.Fields.Clear();
            foreach (var field in vm.Fields.OrderBy(f => f.SortOrder))
            {
                existing.Fields.Add(new ReportField
                {
                    FieldName = field.FieldName,
                    ColumnName = field.ColumnName,
                    DataType = field.DataType,
                    Alignment = field.Alignment,
                    IsVisible = field.IsVisible,
                    IsSortable = field.IsSortable,
                    SortOrder = field.SortOrder
                });
            }

            existing.Design ??= new ReportDesign();
            existing.Design.HeaderText = vm.Title;
            existing.Design.ColorScheme = vm.ColorScheme;
            existing.Design.PageOrientation = vm.PageOrientation;
            existing.Design.ShowGridLines = vm.ShowGridLines;
            existing.Design.ShowAlternatingRows = vm.ShowAlternatingRows;
            existing.Design.ShowTotals = vm.ShowTotals;
            existing.Design.IncludeTimestamp = vm.IncludeTimestamp;
            existing.Design.CustomCss = $"--header-font:{vm.HeaderFont};--body-font:{vm.BodyFont};";

            await _reportTemplateService.UpdateTemplateAsync(existing);

            var newValues = JsonSerializer.Serialize(new
            {
                vm.ReportName,
                vm.ReportType,
                vm.Description,
                Fields = vm.Fields.OrderBy(f => f.SortOrder)
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                Action = "UPDATE",
                EntityName = "ReportTemplate",
                EntityId = existing.Id.ToString(),
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                UserAgent = Request.Headers.UserAgent.ToString(),
                SessionId = HttpContext.Session?.Id ?? string.Empty,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Report \"{vm.ReportName}\" updated successfully.";
            return RedirectToAction(nameof(EditReport), new { templateId = vm.TemplateId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReport(int? templateId)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var allTemplates = await _reportTemplateService.GetAllTemplatesAsync();
            var filtered = isSuperAdmin ? allTemplates : allTemplates.Where(t => t.IsActive).ToList();

            var vm = new DownloadReportViewModel
            {
                TemplateId = templateId ?? filtered.FirstOrDefault()?.Id,
                StartDate = DateTime.UtcNow.Date.AddMonths(-1),
                EndDate = DateTime.UtcNow.Date,
                AvailableReports = filtered
                    .OrderBy(t => t.Name)
                    .Select(t => new ReportTemplateOptionViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        ReportType = t.ReportType,
                        IsActive = t.IsActive
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadReport(DownloadReportViewModel vm)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var allTemplates = await _reportTemplateService.GetAllTemplatesAsync();
            var filtered = isSuperAdmin ? allTemplates : allTemplates.Where(t => t.IsActive).ToList();

            vm.AvailableReports = filtered
                .OrderBy(t => t.Name)
                .Select(t => new ReportTemplateOptionViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    ReportType = t.ReportType,
                    IsActive = t.IsActive
                })
                .ToList();

            if (!vm.TemplateId.HasValue)
            {
                ModelState.AddModelError(nameof(vm.TemplateId), "Select a report template.");
                return View(vm);
            }

            var template = filtered.FirstOrDefault(t => t.Id == vm.TemplateId.Value);
            if (template == null)
            {
                ModelState.AddModelError(nameof(vm.TemplateId), "Selected report is not available for your role.");
                return View(vm);
            }

            var preview = await BuildTabularPreviewAsync(template, vm.StartDate, vm.EndDate, vm.Department, vm.CustomFilters);
            vm.PreviewHeaders = preview.Headers;
            vm.PreviewRows = preview.Rows.Take(25).ToList();
            vm.GeneratedRowCount = preview.Rows.Count;
            vm.PreviewMessage = preview.Message;
            vm.IsGenerated = true;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReportFile(
            int templateId,
            string format,
            DateTime? startDate,
            DateTime? endDate,
            string? department,
            string? customFilters)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var template = await _reportTemplateService.GetTemplateByIdAsync(templateId);
            if (template == null || (!isSuperAdmin && !template.IsActive))
            {
                TempData["ErrorMessage"] = "Selected report is not available.";
                return RedirectToAction(nameof(DownloadReport), new { templateId });
            }

            var preview = await BuildTabularPreviewAsync(template, startDate, endDate, department, customFilters);
            if (preview.Headers.Count == 0)
            {
                TempData["ErrorMessage"] = "No report data available for export.";
                return RedirectToAction(nameof(DownloadReport), new { templateId });
            }

            var title = string.IsNullOrWhiteSpace(template.Name) ? "Custom Report" : template.Name;
            var safeName = string.Join("_", title.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');

            if (string.Equals(format, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                var pdf = _exportService.BuildPdfTable(title, preview.Headers, preview.Rows);
                return File(pdf, "application/pdf", $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmm}.pdf");
            }

            var csv = _exportService.BuildCsv(title, preview.Headers, preview.Rows);
            return File(csv, "text/csv", $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> PreviewReportData(string reportType)
        {
            var rows = await GetPreviewRowsAsync(reportType);
            return Json(new
            {
                success = true,
                source = string.IsNullOrWhiteSpace(reportType) ? "Custom" : reportType,
                rows
            });
        }

        private async Task<List<Dictionary<string, string>>> GetPreviewRowsAsync(string? reportType)
        {
            var type = (reportType ?? string.Empty).Trim();

            switch (type.ToLowerInvariant())
            {
                case "patient":
                    var patients = await _context.Patients
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(10)
                        .ToListAsync();
                    return patients.Select(p => new Dictionary<string, string>
                    {
                        ["patientid"] = p.PatientId,
                        ["fullname"] = (p.FirstName + " " + p.LastName).Trim(),
                        ["gender"] = p.Gender,
                        ["phone"] = p.Phone,
                        ["city"] = p.City,
                        ["createddate"] = p.CreatedDate.ToString("yyyy-MM-dd")
                    }).ToList();

                case "billing":
                    var bills = await _context.Bills
                        .AsNoTracking()
                        .OrderByDescending(b => b.BillDate)
                        .Take(10)
                        .ToListAsync();
                    return bills.Select(b => new Dictionary<string, string>
                    {
                        ["billnumber"] = b.BillNumber,
                        ["billtype"] = b.BillType,
                        ["status"] = b.Status,
                        ["totalamount"] = b.TotalAmount.ToString("0.00"),
                        ["paidamount"] = b.PaidAmount.ToString("0.00"),
                        ["pendingamount"] = b.PendingAmount.ToString("0.00"),
                        ["billdate"] = b.BillDate.ToString("yyyy-MM-dd")
                    }).ToList();

                case "department":
                    var departments = await _context.Departments
                        .AsNoTracking()
                        .OrderBy(d => d.Name)
                        .Take(10)
                        .ToListAsync();
                    return departments.Select(d => new Dictionary<string, string>
                    {
                        ["department"] = d.Name,
                        ["description"] = d.Description,
                        ["headofdepartment"] = d.HeadOfDepartment,
                        ["isactive"] = d.IsActive ? "Active" : "Inactive",
                        ["createddate"] = d.CreatedDate.ToString("yyyy-MM-dd")
                    }).ToList();

                case "hr":
                case "hr/staff":
                    var staff = await _context.Staff
                        .AsNoTracking()
                        .OrderByDescending(s => s.CreatedDate)
                        .Take(10)
                        .ToListAsync();
                    return staff.Select(s => new Dictionary<string, string>
                    {
                        ["employeeid"] = s.EmployeeId,
                        ["fullname"] = (s.FirstName + " " + s.LastName).Trim(),
                        ["department"] = s.Department,
                        ["designation"] = s.Designation,
                        ["salary"] = s.Salary.ToString("0.00"),
                        ["dateofjoining"] = s.DateOfJoining.ToString("yyyy-MM-dd"),
                        ["isactive"] = s.IsActive ? "Active" : "Inactive"
                    }).ToList();

                case "inventory":
                    var inventory = await _context.InventoryItems
                        .AsNoTracking()
                        .OrderBy(i => i.Name)
                        .Take(10)
                        .ToListAsync();
                    return inventory.Select(i => new Dictionary<string, string>
                    {
                        ["itemcode"] = i.ItemCode,
                        ["name"] = i.Name,
                        ["category"] = i.Category,
                        ["currentstock"] = i.CurrentStock.ToString("0.##"),
                        ["minimumstock"] = i.MinimumStock.ToString("0.##"),
                        ["unit"] = i.Unit,
                        ["unitcost"] = i.UnitCost.ToString("0.00"),
                        ["supplier"] = i.Supplier
                    }).ToList();

                case "occupancy":
                    var beds = await _context.Beds
                        .AsNoTracking()
                        .OrderBy(b => b.WardId)
                        .ThenBy(b => b.BedNumber)
                        .Take(10)
                        .ToListAsync();
                    return beds.Select(b => new Dictionary<string, string>
                    {
                        ["bednumber"] = b.BedNumber,
                        ["block"] = b.Block,
                        ["floor"] = b.Floor,
                        ["roomnumber"] = b.RoomNumber,
                        ["bedtype"] = b.BedType,
                        ["status"] = b.Status,
                        ["dailycharges"] = b.DailyCharges.ToString("0.00"),
                        ["isisolation"] = b.IsIsolation ? "Yes" : "No"
                    }).ToList();

                case "financial":
                    var financialRows = await _context.Bills
                        .AsNoTracking()
                        .OrderByDescending(b => b.CreatedDate)
                        .Take(10)
                        .ToListAsync();
                    return financialRows.Select(b => new Dictionary<string, string>
                    {
                        ["billnumber"] = b.BillNumber,
                        ["status"] = b.Status,
                        ["totalamount"] = b.TotalAmount.ToString("0.00"),
                        ["paidamount"] = b.PaidAmount.ToString("0.00"),
                        ["pendingamount"] = b.PendingAmount.ToString("0.00"),
                        ["createddate"] = b.CreatedDate.ToString("yyyy-MM-dd")
                    }).ToList();

                default:
                    var fallback = await _context.Patients
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(10)
                        .ToListAsync();
                    return fallback.Select(p => new Dictionary<string, string>
                    {
                        ["patientid"] = p.PatientId,
                        ["fullname"] = (p.FirstName + " " + p.LastName).Trim(),
                        ["phone"] = p.Phone,
                        ["createddate"] = p.CreatedDate.ToString("yyyy-MM-dd")
                    }).ToList();
            }
        }

        private static string? ExtractFontToken(string? customCss, string token)
        {
            if (string.IsNullOrWhiteSpace(customCss))
                return null;

            var parts = customCss.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split(':', 2, StringSplitOptions.TrimEntries);
                if (kv.Length == 2 && string.Equals(kv[0], token, StringComparison.OrdinalIgnoreCase))
                {
                    return kv[1];
                }
            }

            return null;
        }

        private async Task<(List<string> Headers, List<List<string>> Rows, string Message)> BuildTabularPreviewAsync(
            ReportTemplate template,
            DateTime? startDate,
            DateTime? endDate,
            string? department,
            string? customFilters)
        {
            var parameters = new Dictionary<string, object>();
            if (startDate.HasValue) parameters["StartDate"] = startDate.Value;
            if (endDate.HasValue) parameters["EndDate"] = endDate.Value;
            if (!string.IsNullOrWhiteSpace(department)) parameters["Department"] = department;

            foreach (var entry in ParseCustomFilters(customFilters))
            {
                parameters[entry.Key] = entry.Value;
            }

            var visibleFields = template.Fields
                .Where(f => f.IsVisible)
                .OrderBy(f => f.SortOrder)
                .ToList();

            var execution = await _reportTemplateService.ExecuteSavedReportAsync(template.Id, parameters);
            if (execution.Success && execution.Data.Count > 0)
            {
                var headers = visibleFields.Count > 0
                    ? visibleFields.Select(f => f.FieldName).ToList()
                    : execution.Data.First().Keys.ToList();

                var rows = execution.Data
                    .Select(row => headers.Select(h => ResolveRowValue(row, h, visibleFields)).ToList())
                    .ToList();

                return (headers, rows, execution.Message ?? "Report generated successfully.");
            }

            var fallback = await GetPreviewRowsAsync(template.ReportType);
            if (fallback.Count == 0)
            {
                return (new List<string>(), new List<List<string>>(), "No preview data available.");
            }

            var fallbackHeaders = visibleFields.Count > 0
                ? visibleFields.Select(f => f.FieldName).ToList()
                : fallback.First().Keys.ToList();

            var fallbackRows = fallback
                .Select(row => fallbackHeaders.Select(h => ResolveFallbackValue(row, h, visibleFields)).ToList())
                .ToList();

            var msg = string.IsNullOrWhiteSpace(execution.Message)
                ? "Live execution unavailable; fallback data shown."
                : execution.Message + " Fallback data shown.";

            return (fallbackHeaders, fallbackRows, msg);
        }

        private static string ResolveRowValue(
            Dictionary<string, object> row,
            string header,
            IReadOnlyList<ReportField> visibleFields)
        {
            var mapped = visibleFields.FirstOrDefault(f => string.Equals(f.FieldName, header, StringComparison.OrdinalIgnoreCase));
            var candidateKeys = new List<string>
            {
                mapped?.ColumnName ?? header,
                mapped?.FieldName ?? header,
                header
            };

            foreach (var key in candidateKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                var direct = row.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(direct.Key))
                    return direct.Value?.ToString() ?? string.Empty;

                var normalized = NormalizeKey(key);
                var loose = row.FirstOrDefault(r => NormalizeKey(r.Key) == normalized);
                if (!string.IsNullOrWhiteSpace(loose.Key))
                    return loose.Value?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string ResolveFallbackValue(
            Dictionary<string, string> row,
            string header,
            IReadOnlyList<ReportField> visibleFields)
        {
            var mapped = visibleFields.FirstOrDefault(f => string.Equals(f.FieldName, header, StringComparison.OrdinalIgnoreCase));
            var source = mapped?.ColumnName ?? mapped?.FieldName ?? header;
            var key = NormalizeKey(source);
            return row.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static string NormalizeKey(string? value)
        {
            return string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit)).ToLowerInvariant();
        }

        private static Dictionary<string, string> ParseCustomFilters(string? customFilters)
        {
            var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(customFilters))
                return filters;

            var lines = customFilters.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    filters[parts[0]] = parts[1];
                }
            }

            return filters;
        }

        // ── C. User Management ────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> UserManagement(string? search, string statusFilter = "All", string roleFilter = "")
        {
            var canManage = User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
            var allUsers = await _userManager.Users.ToListAsync();
            var availableRoles = await _roleManager.Roles.Where(r => r.Name != "Patient").Select(r => r.Name).ToListAsync();

            var filtered = allUsers.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                filtered = filtered.Where(u =>
                    u.Email?.ToLowerInvariant().Contains(searchLower) == true ||
                    u.EmployeeId?.ToLowerInvariant().Contains(searchLower) == true ||
                    (u.FirstName + " " + u.LastName).ToLowerInvariant().Contains(searchLower) ||
                    u.PhoneNumber?.ToLowerInvariant().Contains(searchLower) == true);
            }

            // Apply status filter
            if (statusFilter == "Active")
                filtered = filtered.Where(u => u.IsActive);
            else if (statusFilter == "Inactive")
                filtered = filtered.Where(u => !u.IsActive);

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
                filtered = filtered.Where(u => userIdsInRole.Contains(u.Id));
            }

            var orderedUsers = filtered.OrderByDescending(u => u.CreatedDate).ToList();
            var rows = new List<UserManagementRowViewModel>();

            foreach (var (user, index) in orderedUsers.Select((u, i) => (u, i)))
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new UserManagementRowViewModel
                {
                    SerialNo = index + 1,
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    EmployeeId = user.EmployeeId ?? string.Empty,
                    FullName = (user.FirstName + " " + user.LastName).Trim(),
                    RolesList = string.Join(", ", roles),
                    IsActive = user.IsActive,
                    LastLoginDate = user.LastLoginDate,
                    CreatedDate = user.CreatedDate
                });
            }

            var vm = new UserManagementViewVM
            {
                IsSuperAdminOrAdmin = canManage,
                SearchTerm = search ?? string.Empty,
                StatusFilter = statusFilter,
                RoleFilter = roleFilter,
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.IsActive),
                InactiveUsers = allUsers.Count(u => !u.IsActive),
                AvailableRoles = availableRoles,
                Rows = rows
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> MarkUserInactive(string userId, string? reason = "")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            // Log audit entry
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                Action = "DEACTIVATE",
                EntityName = "ApplicationUser",
                EntityId = user.Id,
                OldValues = JsonSerializer.Serialize(new { IsActive = true }),
                NewValues = JsonSerializer.Serialize(new { IsActive = false, Reason = reason }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                UserAgent = Request.Headers.UserAgent.ToString(),
                SessionId = HttpContext.Session?.Id ?? string.Empty,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} ({Email}) deactivated by {Actor}.", userId, user.Email, User.Identity?.Name);
            return Json(new { success = true, message = $"User '{user.Email}' has been deactivated." });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> MarkUserActive(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Generate temporary password and password reset token
            var tempPassword = GenerateTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetCode = System.Web.HttpUtility.UrlEncode(resetToken);

            // Activate user
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            // Log audit entry
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                Action = "ACTIVATE",
                EntityName = "ApplicationUser",
                EntityId = user.Id,
                OldValues = JsonSerializer.Serialize(new { IsActive = false }),
                NewValues = JsonSerializer.Serialize(new { IsActive = true, PasswordResetRequired = true }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                UserAgent = Request.Headers.UserAgent.ToString(),
                SessionId = HttpContext.Session?.Id ?? string.Empty,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} ({Email}) activated by {Actor}. Reset token generated.", userId, user.Email, User.Identity?.Name);

            // Return reset token and temp password for display/email
            return Json(new
            {
                success = true,
                message = $"User '{user.Email}' has been activated. Password reset required.",
                tempPassword = tempPassword,
                resetCode = resetCode,
                email = user.Email
            });
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var loginCount = await _context.AuditLogs
                .CountAsync(a => a.UserId == userId && a.Action == "LOGIN");

            var vm = new UserDetailsViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                EmployeeId = user.EmployeeId ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                RolesList = string.Join(", ", roles),
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                FirstLoginDate = user.FirstLoginDate,
                LastLoginDate = user.LastLoginDate,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                TotalLogins = loginCount
            };

            return PartialView("_UserDetailsModal", vm);
        }

        // ── D. Theme Management ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ThemeManagement()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var preference = await _context.UserThemePreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var currentTheme = NormalizeThemeId(preference?.ThemeId);
            var vm = new ThemeManagementViewModel
            {
                CurrentUserTheme = currentTheme,
                SelectedTheme = currentTheme,
                PreferenceSince = preference?.PreferenceSince,
                AvailableThemes = StaffThemes.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectTheme(string themeId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var normalizedTheme = NormalizeThemeId(themeId);
            if (!StaffThemes.Any(t => string.Equals(t.ThemeId, normalizedTheme, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = "Selected theme is not supported.";
                return RedirectToAction(nameof(ThemeManagement));
            }

            var preference = await _context.UserThemePreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            var previousTheme = preference?.ThemeId ?? "sunflower";

            if (preference == null)
            {
                preference = new UserThemePreference
                {
                    UserId = userId,
                    ThemeId = normalizedTheme,
                    PreferenceSince = DateTime.UtcNow,
                    IsDefault = false
                };
                _context.UserThemePreferences.Add(preference);
            }
            else
            {
                preference.ThemeId = normalizedTheme;
                preference.PreferenceSince = DateTime.UtcNow;
                preference.IsDefault = false;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "THEME_UPDATE",
                EntityName = "UserThemePreference",
                EntityId = preference.Id.ToString(),
                OldValues = JsonSerializer.Serialize(new { ThemeId = previousTheme }),
                NewValues = JsonSerializer.Serialize(new { ThemeId = normalizedTheme }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                UserAgent = Request.Headers.UserAgent.ToString(),
                SessionId = HttpContext.Session?.Id ?? string.Empty,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Theme updated successfully.";

            return RedirectToAction(nameof(ThemeManagement));
        }

        [HttpGet]
        public async Task<IActionResult> ThemeStylesheet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var themeId = "sunflower";

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var preference = await _context.UserThemePreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                themeId = NormalizeThemeId(preference?.ThemeId);
            }

            var cssPath = Path.Combine(_env.WebRootPath, "css", "themes", $"{themeId}.css");
            if (!System.IO.File.Exists(cssPath))
                cssPath = Path.Combine(_env.WebRootPath, "css", "themes", "sunflower.css");

            var cssContent = await System.IO.File.ReadAllTextAsync(cssPath);
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            return Content(cssContent, "text/css");
        }

        private static string NormalizeThemeId(string? input)
        {
            var candidate = (input ?? string.Empty).Trim().ToLowerInvariant();
            return candidate switch
            {
                "dark" => "dark",
                "light" => "light",
                "sunflower" => "sunflower",
                "snowflake" => "snowflake",
                "ocean" => "ocean",
                "forest" => "forest",
                "midnight" => "midnight",
                "sunset" => "sunset",
                "lavender" => "lavender",
                "graphite" => "graphite",
                "emerald" => "emerald",
                "peach" => "peach",
                "sky" => "sky",
                "rose" => "rose",
                "sand" => "sand",
                "plum" => "plum",
                "aqua" => "aqua",
                "crimson" => "crimson",
                "amber" => "amber",
                "arctic" => "arctic",
                "chocolate" => "chocolate",
                "indigo" => "indigo",
                "lime" => "lime",
                _ => "sunflower"
            };
        }

        private static string GenerateTemporaryPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%";
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[length];
                rng.GetBytes(randomBytes);
                var result = new StringBuilder(length);
                foreach (byte b in randomBytes)
                {
                    result.Append(validChars[b % validChars.Length]);
                }
                return result.ToString();
            }
        }
    }
}

