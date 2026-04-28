using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    // ── B. Create Report ─────────────────────────────────────────────────────

    public sealed class CreateReportFieldViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";   // string | int | decimal | datetime
        public string Alignment { get; set; } = "left";    // left | center | right
        public bool IsVisible { get; set; } = true;
        public bool IsSortable { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class CreateReportViewModel
    {
        [Required(ErrorMessage = "Report name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string ReportName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Category / report type (e.g., Billing, Patient, HR).</summary>
        [Required(ErrorMessage = "Report type is required.")]
        public string ReportType { get; set; } = string.Empty;

        // ── Style options ──────────────────────────────────────────────
        public string ColorScheme { get; set; } = "default";   // default | professional | colorful
        public string PageOrientation { get; set; } = "portrait"; // portrait | landscape
        public string HeaderFont { get; set; } = "Arial";
        public string BodyFont { get; set; } = "Arial";
        public bool ShowGridLines { get; set; } = true;
        public bool ShowAlternatingRows { get; set; } = true;
        public bool ShowTotals { get; set; } = false;
        public bool IncludeTimestamp { get; set; } = true;

        // ── Field builder ─────────────────────────────────────────────
        /// <summary>Serialized JSON of field rows submitted via the field-builder UI.</summary>
        public string FieldsJson { get; set; } = "[]";

        /// <summary>Deserialized fields — populated from FieldsJson before/after binding.</summary>
        public List<CreateReportFieldViewModel> Fields { get; set; } = new();
    }

    public sealed class ReportTemplateOptionViewModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ReportType { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    public sealed class EditReportViewModel : CreateReportViewModel
    {
        [Required]
        public int TemplateId { get; set; }
        public List<ReportTemplateOptionViewModel> AvailableReports { get; set; } = new();
    }

    public sealed class DownloadReportViewModel
    {
        public int? TemplateId { get; set; }
        public List<ReportTemplateOptionViewModel> AvailableReports { get; set; } = new();

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Department { get; set; }
        public string? CustomFilters { get; set; }
        public string ExportFormat { get; set; } = "Excel"; // Excel or PDF

        public bool IsGenerated { get; set; }
        public int GeneratedRowCount { get; set; }
        public List<string> PreviewHeaders { get; set; } = new();
        public List<List<string>> PreviewRows { get; set; } = new();
        public string PreviewMessage { get; set; } = string.Empty;
    }

    // ── Report Management (A) – existing view models ─────────────────────────

    public sealed class SystemManagementReportRowViewModel
    {
        public int SerialNo { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Purpose { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public IReadOnlyList<string> AssignedRoles { get; init; } = Array.Empty<string>();
        public string AssignedRolesText => AssignedRoles.Count == 0 ? "All Roles" : string.Join(", ", AssignedRoles);
    }

    public sealed class SystemManagementReportListViewModel
    {
        public bool IsSuperAdmin { get; init; }
        public string SearchTerm { get; init; } = string.Empty;
        public int TotalReports { get; init; }
        public int ActiveReports { get; init; }
        public IReadOnlyList<string> AvailableRoles { get; init; } = Array.Empty<string>();
        public IReadOnlyList<SystemManagementReportRowViewModel> Rows { get; init; } = Array.Empty<SystemManagementReportRowViewModel>();
    }

    // ── C. User Management ─────────────────────────────────────────────────

    public sealed class UserManagementRowViewModel
    {
        public int SerialNo { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string EmployeeId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string RolesList { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime? LastLoginDate { get; init; }
        public DateTime CreatedDate { get; init; }
    }

    public sealed class UserManagementViewVM
    {
        public bool IsSuperAdminOrAdmin { get; init; }
        public string SearchTerm { get; init; } = string.Empty;
        public string StatusFilter { get; init; } = "All"; // All, Active, Inactive
        public string RoleFilter { get; init; } = string.Empty;
        public int TotalUsers { get; init; }
        public int ActiveUsers { get; init; }
        public int InactiveUsers { get; init; }
        public List<string> AvailableRoles { get; init; } = new();
        public IReadOnlyList<UserManagementRowViewModel> Rows { get; init; } = Array.Empty<UserManagementRowViewModel>();
    }

    public sealed class UserDetailsViewModel
    {
        public string UserId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string EmployeeId { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string RolesList { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedDate { get; init; }
        public DateTime? FirstLoginDate { get; init; }
        public DateTime? LastLoginDate { get; init; }
        public string PhoneNumber { get; init; } = string.Empty;
        public int TotalLogins { get; init; }
    }

    // ── D. Theme Management ────────────────────────────────────────────────

    public sealed class ThemeOptionViewModel
    {
        public string ThemeId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string PreviewClass { get; init; } = string.Empty;
    }

    public sealed class ThemeManagementViewModel
    {
        public string CurrentUserTheme { get; init; } = "sunflower";
        public DateTime? PreferenceSince { get; init; }
        public List<ThemeOptionViewModel> AvailableThemes { get; init; } = new();
        public string SelectedTheme { get; set; } = "sunflower";
    }
}
