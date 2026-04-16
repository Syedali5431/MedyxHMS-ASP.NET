namespace MedyxHMS.Models
{
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