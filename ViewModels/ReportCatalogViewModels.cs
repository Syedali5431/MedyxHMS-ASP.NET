namespace MedyxHMS.ViewModels
{
    public sealed class ReportCatalogItem
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string? EmbeddedUrl { get; init; }
        public string? TemplateLookupName { get; init; }
        public bool AdminOnly { get; init; }
        public bool IsLegacy { get; init; }
    }

    public sealed class ReportTemplateOption
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class ReportsWorkspaceViewModel
    {
        public IReadOnlyList<ReportCatalogItem> Items { get; init; } = Array.Empty<ReportCatalogItem>();
        public ReportCatalogItem? SelectedReport { get; init; }
        public bool CanManageTemplates { get; init; }
        public int ImportedLegacyTemplateCount { get; init; }
        public int? MatchingTemplateId { get; init; }
        public string? MatchingTemplateName { get; init; }
        public IReadOnlyList<ReportTemplateOption> PreviewableTemplates { get; init; } = Array.Empty<ReportTemplateOption>();
    }

    public static class ReportCatalogRegistry
    {
        public static IReadOnlyList<ReportCatalogItem> All { get; } = Build();

        public static IReadOnlyList<ReportCatalogItem> GetVisibleItems(bool isAdminOrSuper)
        {
            return All.Where(item => isAdminOrSuper || !item.AdminOnly).ToList();
        }

        public static string ResolveCurrentKey(string currentPath, string? reportKey)
        {
            if (!string.IsNullOrWhiteSpace(reportKey))
            {
                return reportKey.Trim();
            }

            if (currentPath.StartsWith("/Audit", StringComparison.OrdinalIgnoreCase)) return "R37";
            if (currentPath.StartsWith("/Report/DepartmentReport", StringComparison.OrdinalIgnoreCase)) return "R41";
            if (currentPath.StartsWith("/Report/FinancialReport", StringComparison.OrdinalIgnoreCase)) return "R42";
            if (currentPath.StartsWith("/Report/OccupancyReport", StringComparison.OrdinalIgnoreCase)) return "R43";
            if (currentPath.StartsWith("/Report/StaffReport", StringComparison.OrdinalIgnoreCase)) return "R44";
            if (currentPath.StartsWith("/Report/Builder", StringComparison.OrdinalIgnoreCase)) return "R45";
            if (currentPath.StartsWith("/Report/ScheduleReport", StringComparison.OrdinalIgnoreCase)) return "R46";
            if (currentPath.StartsWith("/Report/GeneratedReports", StringComparison.OrdinalIgnoreCase)) return "R47";
            if (currentPath.StartsWith("/Report/Preview", StringComparison.OrdinalIgnoreCase)) return "R49";

            return string.Empty;
        }

        private static IReadOnlyList<ReportCatalogItem> Build()
        {
            return new List<ReportCatalogItem>
            {
                Feature("R1", "Daily Transaction Report", "ASP.NET Reports - Converted", "Daily transaction report showing payments and refunds for a selected date.", "/Report/DailyTransactionReport"),
                Feature("R2", "All Transaction Report", "ASP.NET Reports - Converted", "All transactions within a date range with breakdown by type and status.", "/Report/AllTransactionReport"),
                Feature("R3", "Appointment Report", "ASP.NET Reports - Converted", "Appointment statistics with status breakdown and doctor-wise analysis.", "/Report/AppointmentReport"),
                Feature("R4", "OPD Report", "ASP.NET Reports - Converted", "Out-patient visits with consultation fees and payment status tracking.", "/Report/OPDLegacyReport"),
                Feature("R5", "IPD Report", "ASP.NET Reports - Converted", "In-patient admissions with length of stay and discharge analysis.", "/Report/IPDLegacyReport"),
                Legacy("R6", "OPD Balance Report", "PHP: OPD Balance Report"),
                Legacy("R7", "IPD Balance Report", "PHP: IPD Balance Report"),
                Legacy("R8", "OPD Discharged Patient Report"),
                Legacy("R9", "IPD Discharged Patient Report"),
                Legacy("R10", "Pharmacy Balance Report"),
                Legacy("R11", "Expiry Medicine Report"),
                Legacy("R12", "Pathology Patient Report"),
                Legacy("R13", "Radiology Patient Report"),
                Legacy("R14", "Operation Theatre (OT) Report", "PHP: Operation Theatre Report"),
                Legacy("R15", "Blood Issue Report", "PHP: Blood Issue Report"),
                Legacy("R16", "Blood Component Issue Report", "PHP: Blood Component Issue Report"),
                Legacy("R17", "Blood Donor Report", "PHP: Blood Donor Report"),
                Legacy("R18", "Live Consultation Report"),
                Legacy("R19", "Live Meeting Report"),
                Legacy("R20", "TPA Report", "PHP: TPA Report"),
                Legacy("R21", "Income Report"),
                Legacy("R22", "Income Group Report"),
                Legacy("R23", "Expense Report"),
                Legacy("R24", "Expense Group Report", "PHP: Group Expense Report"),
                Legacy("R25", "Ambulance Report", "PHP: Ambulance Report"),
                Legacy("R26", "Birth Report", "PHP: Birth Report"),
                Legacy("R27", "Death Report", "PHP: Death Report"),
                Legacy("R28", "Payroll Month Report"),
                Legacy("R29", "Payroll Report", "PHP: Payroll Report"),
                Legacy("R30", "Staff Attendance Report", "PHP: Staff Attendance Report"),
                Legacy("R31", "User Log Report"),
                Legacy("R32", "Patient Login Credential Report"),
                Legacy("R33", "Email / SMS Log Report"),
                Legacy("R34", "Inventory Stock Report"),
                Legacy("R35", "Inventory Item Report", "PHP: Item Report"),
                Legacy("R36", "Inventory Issue Report", "PHP: Issue Inventory Report"),
                Feature("R37", "Audit Trail Report", "Platform Reports", "Review audit trail events and user activity logs.", "/Audit"),
                Legacy("R38", "Patient Visit Report", "PHP: Patient Visit Report"),
                Legacy("R39", "Patient Bill Report", "PHP: Patient Bill Report"),
                Legacy("R40", "Referral Report", "PHP: Referral Report"),
                Feature("R41", "Department Report", "ASP.NET Reports", "Department-level analytics and summarized report output.", "/Report/DepartmentReport"),
                Feature("R42", "Financial Report", "ASP.NET Reports", "Hospital financial data including income, expense, and payroll summary.", "/Report/FinancialReport"),
                Feature("R43", "Occupancy Report", "ASP.NET Reports", "Bed occupancy metrics and average occupancy calculations.", "/Report/OccupancyReport"),
                Feature("R44", "Staff Report", "ASP.NET Reports", "Staff attendance analytics for a selected staff member and date range.", "/Report/StaffReport"),
                Feature("R45", "Report Builder / Template", "ASP.NET Reports", "Create, design, clone, and manage report templates.", "/Report/Builder", true),
                Feature("R46", "Report Scheduler", "ASP.NET Reports", "Schedule automated report generation and recurring report jobs.", "/Report/ScheduleReport", true),
                Feature("R47", "Generated Reports Archive", "ASP.NET Reports", "View and manage previously generated reports.", "/Report/GeneratedReports"),
                Feature("R48", "Legacy PHP Report Import", "ASP.NET Reports", "Import PHP-era report definitions into editable ASP.NET report templates.", null, true),
                Feature("R49", "Report Preview", "ASP.NET Reports", "Preview imported or saved report templates before generating/exporting.", null, true)
            };
        }

        private static ReportCatalogItem Legacy(string key, string name, string? templateLookupName = null)
        {
            return new ReportCatalogItem
            {
                Key = key,
                Name = name,
                Category = "PHP-Originated Reports",
                Summary = "Available in the unified report catalog as a migrated legacy report definition.",
                TemplateLookupName = templateLookupName ?? $"PHP: {name}",
                IsLegacy = true
            };
        }

        private static ReportCatalogItem Feature(string key, string name, string category, string summary, string? embeddedUrl = null, bool adminOnly = false)
        {
            return new ReportCatalogItem
            {
                Key = key,
                Name = name,
                Category = category,
                Summary = summary,
                EmbeddedUrl = embeddedUrl,
                AdminOnly = adminOnly
            };
        }
    }
}