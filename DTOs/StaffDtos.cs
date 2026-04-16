using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.DTOs
{
    // Staff DTOs for staff management operations

    public class StaffDto
    {
        public string Id { get; set; }
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public DateTime DateOfJoining { get; set; }
        public decimal Salary { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string StatusBadgeClass => IsActive ? "badge-success" : "badge-danger";
        public string FormattedLastLogin => LastLoginDate?.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "Never";
        public List<StaffRoleDto> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class StaffCreateDto
    {
        [Required(ErrorMessage = "Employee ID is required")]
        [StringLength(20, ErrorMessage = "Employee ID cannot exceed 20 characters")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        public string Designation { get; set; }

        [Required(ErrorMessage = "Date of joining is required")]
        public DateTime DateOfJoining { get; set; }

        [Required(ErrorMessage = "Salary is required")]
        [Range(0, 999999.99, ErrorMessage = "Salary must be between 0 and 999999.99")]
        public decimal Salary { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public List<int> RoleIds { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class StaffUpdateDto
    {
        [Required(ErrorMessage = "Staff ID is required")]
        public string Id { get; set; }

        [StringLength(20, ErrorMessage = "Employee ID cannot exceed 20 characters")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        public string Designation { get; set; }

        [Required(ErrorMessage = "Date of joining is required")]
        public DateTime DateOfJoining { get; set; }

        [Required(ErrorMessage = "Salary is required")]
        [Range(0, 999999.99, ErrorMessage = "Salary must be between 0 and 999999.99")]
        public decimal Salary { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        public List<int> RoleIds { get; set; } = new();
        public bool? IsActive { get; set; }
    }

    public class StaffPasswordChangeDto
    {
        public string StaffId { get; set; }

        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }

    public class StaffProfileUpdateDto
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }
    }

    public class StaffRoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public DateTime AssignedDate { get; set; }
        public string AssignedBy { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}