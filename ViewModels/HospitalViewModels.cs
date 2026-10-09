using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class HospitalListItemViewModel
    {
        public Hospital Hospital { get; set; } = null!;
        public int StaffAssigned { get; set; }
        public int Appointments { get; set; }
        public int CurrentAdmissions { get; set; }
        public int Bills { get; set; }
        public int Wards { get; set; }
    }

    public class HospitalAccessViewModel
    {
        public Hospital Hospital { get; set; } = null!;
        public bool CanEditSuperAdmins { get; set; }

        /// <summary>Only a SuperAdmin assigns hospitals to Admin and SuperAdmin accounts.</summary>
        public bool CanEditAdmins { get; set; }
        public List<HospitalAccessUserRow> Users { get; set; } = new();
    }

    public class HospitalAccessUserRow
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public bool IsAdmin { get; set; }
        public bool HasAccess { get; set; }
        public bool IsDefault { get; set; }
        public List<string> OtherHospitals { get; set; } = new();

        /// <summary>No hospital assigned yet, so the user works in the default hospital.</summary>
        public bool ImplicitDefaultAccess { get; set; }
    }

    /// <summary>Hospitals one user can work in (User Management → Hospitals).</summary>
    public class HospitalUserAccessViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public bool IsAdmin { get; set; }
        public bool CanEdit { get; set; }
        public List<HospitalUserAccessRow> Hospitals { get; set; } = new();
    }

    public class HospitalUserAccessRow
    {
        public int HospitalId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsGroupDefault { get; set; }
        public bool HasAccess { get; set; }
        public bool IsDefault { get; set; }
        public bool CanEdit { get; set; }
    }
}
