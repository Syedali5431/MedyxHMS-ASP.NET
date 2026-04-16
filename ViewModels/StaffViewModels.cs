using MedyxHMS.DTOs;
using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    public class StaffIndexViewModel
    {
        public List<StaffDto> Staff { get; set; } = new List<StaffDto>();

        // Search and filter properties
        public string SearchTerm { get; set; }
        public string DepartmentFilter { get; set; }
        public string RoleFilter { get; set; }
        public bool? IsActiveFilter { get; set; }

        // Dropdown options
        public List<string> DepartmentOptions { get; set; } = new List<string>();
        public List<string> RoleOptions { get; set; } = new List<string>();

        // Statistics
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }
        public int StaffThisMonth { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class StaffCreateViewModel
    {
        public StaffCreateDto Staff { get; set; } = new StaffCreateDto();

        // Available roles for assignment
        public List<RoleSelectionViewModel> AvailableRoles { get; set; } = new List<RoleSelectionViewModel>();

        // Department options
        public List<string> DepartmentOptions { get; set; } = new List<string>
        {
            "Administration",
            "Medical",
            "Nursing",
            "Pharmacy",
            "Laboratory",
            "Radiology",
            "IT",
            "Finance",
            "HR",
            "Maintenance"
        };
    }

    public class StaffEditViewModel
    {
        public StaffUpdateDto Staff { get; set; } = new StaffUpdateDto();

        // Available roles for assignment
        public List<RoleSelectionViewModel> AvailableRoles { get; set; } = new List<RoleSelectionViewModel>();

        // Department options
        public List<string> DepartmentOptions { get; set; } = new List<string>
        {
            "Administration",
            "Medical",
            "Nursing",
            "Pharmacy",
            "Laboratory",
            "Radiology",
            "IT",
            "Finance",
            "HR",
            "Maintenance"
        };

        // Current role assignments
        public List<StaffRoleDto> CurrentRoles { get; set; } = new List<StaffRoleDto>();
    }

    public class StaffDetailsViewModel
    {
        public StaffDto Staff { get; set; } = new StaffDto();

        // Role information
        public List<StaffRoleDto> Roles { get; set; } = new List<StaffRoleDto>();

        // Permissions
        public List<string> Permissions { get; set; } = new List<string>();

        // Recent activity (last 10 actions)
        public List<AuditLog> RecentActivity { get; set; } = new List<AuditLog>();

        // Related data
        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalBills { get; set; }
    }

    public class StaffDeleteViewModel
    {
        public StaffDto Staff { get; set; } = new StaffDto();

        // Impact assessment
        public int PatientsManaged { get; set; }
        public int AppointmentsScheduled { get; set; }
        public int BillsCreated { get; set; }
        public int RecentActivities { get; set; }

        // Deletion options
        public bool TransferData { get; set; }
        public string TransferToStaffId { get; set; }
        public List<StaffDto> AvailableStaffForTransfer { get; set; } = new List<StaffDto>();
    }

    public class StaffPasswordChangeViewModel
    {
        public StaffPasswordChangeDto PasswordChange { get; set; } = new StaffPasswordChangeDto();
        public string StaffName { get; set; }
        public string StaffEmail { get; set; }
    }

    public class StaffProfileViewModel
    {
        public StaffProfileUpdateDto Profile { get; set; } = new StaffProfileUpdateDto();
        public string CurrentProfileImageUrl { get; set; }
    }

    public class StaffRoleManagementViewModel
    {
        public string StaffId { get; set; }
        public string StaffName { get; set; }
        public List<RoleSelectionViewModel> AvailableRoles { get; set; } = new List<RoleSelectionViewModel>();
        public List<StaffRoleDto> CurrentRoles { get; set; } = new List<StaffRoleDto>();
    }

    public class RoleSelectionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class StaffDashboardViewModel
    {
        // Staff statistics
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int NewStaffThisMonth { get; set; }
        public int StaffByDepartment { get; set; }

        // Department breakdown
        public Dictionary<string, int> StaffByDepartment { get; set; } = new Dictionary<string, int>();

        // Role distribution
        public Dictionary<string, int> StaffByRole { get; set; } = new Dictionary<string, int>();

        // Recent staff additions
        public List<StaffDto> RecentStaff { get; set; } = new List<StaffDto>();

        // Staff activity summary
        public int TotalLoginsToday { get; set; }
        public int ActiveUsersNow { get; set; }
        public List<string> TopActiveUsers { get; set; } = new List<string>();
    }

    public class StaffImportViewModel
    {
        public IFormFile ImportFile { get; set; }
        public bool HasHeaders { get; set; } = true;
        public string Delimiter { get; set; } = ",";
        public List<string> ImportResults { get; set; } = new List<string>();
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class StaffExportViewModel
    {
        public string ExportFormat { get; set; } = "csv"; // csv, excel, pdf
        public List<string> SelectedFields { get; set; } = new List<string>();
        public string DateRange { get; set; } // last7days, last30days, last90days, custom
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DepartmentFilter { get; set; }
        public string RoleFilter { get; set; }
        public bool IncludeInactive { get; set; }

        // Available fields for export
        public List<string> AvailableFields { get; set; } = new List<string>
        {
            "EmployeeId", "FirstName", "LastName", "Email", "Phone",
            "Department", "Designation", "DateOfJoining", "Salary",
            "Address", "IsActive", "Roles", "LastLoginDate"
        };
    }
}