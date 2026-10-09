using System.ComponentModel.DataAnnotations;

// Purpose: Multi-hospital support (branches of one group). Patients, staff and master data are shared;
// records that implement IHospitalScoped belong to one hospital and are filtered by the active hospital.
namespace MedyxHMS.Models
{
    /// <summary>
    /// A record that belongs to one hospital of the group (appointments, OPD/IPD, bills, inventory, OT, wards).
    /// HospitalId is filled automatically from the active hospital when the record is created.
    /// </summary>
    public interface IHospitalScoped
    {
        int? HospitalId { get; set; }
    }

    public class Hospital
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(200)]
        [Display(Name = "Hospital name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        // Optional; the format is checked by HospitalController (a blank e-mail is allowed).
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Registration / licence no.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        /// <summary>The group's main hospital: receives existing data and records created without an active hospital.</summary>
        [Display(Name = "Default hospital")]
        public bool IsDefault { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }

    /// <summary>Which hospitals a staff login may work in. Users without rows work in the default hospital.</summary>
    public class UserHospitalAccess
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int HospitalId { get; set; }
        /// <summary>Hospital selected automatically after sign-in.</summary>
        public bool IsDefault { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Hospital Hospital { get; set; } = null!;
    }
}
