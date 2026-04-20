namespace MedyxHMS.Models
{
    /// <summary>
    /// Represents a system module (e.g. OPD, Lab, Ambulance).
    /// SuperAdmin can toggle IsGloballyEnabled.
    /// Admin and SuperAdmin can configure per-user access via UserModuleAccess.
    /// </summary>
    public class SystemModule
    {
        public int Id { get; set; }

        /// <summary>Short programmatic key, e.g. "OPD", "Lab", "Ambulance".</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Human-readable label shown in the UI.</summary>
        public string DisplayName { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Optional icon CSS class for navigation display.</summary>
        public string? Icon { get; set; }

        /// <summary>
        /// When false, the module is disabled globally for all users (except SuperAdmin).
        /// Only SuperAdmin can change this flag.
        /// </summary>
        public bool IsGloballyEnabled { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUserId { get; set; }

        public ICollection<UserModuleAccess> UserAccesses { get; set; } = new List<UserModuleAccess>();
    }

    /// <summary>
    /// Per-user module access override.
    /// Admin and SuperAdmin can disable a globally-enabled module for a specific user.
    /// A globally-disabled module cannot be re-enabled here.
    /// </summary>
    public class UserModuleAccess
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public int ModuleId { get; set; }

        /// <summary>False means this specific user cannot access this module.</summary>
        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUserId { get; set; }

        public ApplicationUser? User { get; set; }
        public SystemModule? Module { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<RoleFeature> RoleFeatures { get; set; }
        public ICollection<StaffRole> StaffRoles { get; set; }
    }

    public class Feature
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Module { get; set; } // Patient, Appointment, Billing, etc.
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<RoleFeature> RoleFeatures { get; set; }
    }

    public class RoleFeature
    {
        public int RoleId { get; set; }
        public int FeatureId { get; set; }
        public bool CanView { get; set; } = false;
        public bool CanAdd { get; set; } = false;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Role Role { get; set; }
        public Feature Feature { get; set; }
    }

    public class StaffRole
    {
        public string StaffId { get; set; } // ApplicationUser Id
        public int RoleId { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; }

        // Navigation properties
        public Staff Staff { get; set; }
        public Role Role { get; set; }
    }

    public class Staff
    {
        public string Id { get; set; } // Same as ApplicationUser Id
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public DateTime DateOfJoining { get; set; }
        public decimal Salary { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Email
        {
            get => User?.Email;
            set
            {
                if (User != null)
                {
                    User.Email = value;
                }
            }
        }
        public string About { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<StaffRole> StaffRoles { get; set; }
    }
}