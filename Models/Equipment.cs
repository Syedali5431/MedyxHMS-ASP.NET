using System.ComponentModel.DataAnnotations;

// Purpose: Medical equipment register (ISO 9001 7.1.5 / ISO 15189 6.4 / NABH FMS): assets with preventive
// maintenance and calibration schedules, due/overdue tracking and service history.
namespace MedyxHMS.Models
{
    public class Equipment : IHospitalScoped
    {
        public int Id { get; set; }
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        [StringLength(30)]
        [Display(Name = "Asset tag")]
        public string AssetTag { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = "Patient monitoring";

        [StringLength(100)]
        public string Manufacturer { get; set; } = string.Empty;

        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Serial no.")]
        public string SerialNumber { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Location / department")]
        public string Location { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Supplier / service vendor")]
        public string Supplier { get; set; } = string.Empty;

        [Display(Name = "Purchase / commissioning date")]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Warranty until")]
        public DateTime? WarrantyExpiry { get; set; }

        /// <summary>Low, Medium, High (life-support / critical devices).</summary>
        [StringLength(10)]
        [Display(Name = "Risk class")]
        public string RiskClass { get; set; } = "Medium";

        /// <summary>In service, Under maintenance, Out of service, Condemned.</summary>
        [StringLength(30)]
        public string Status { get; set; } = "In service";

        [Display(Name = "Preventive maintenance every (days)")]
        [Range(0, 3650)]
        public int MaintenanceIntervalDays { get; set; }

        [Display(Name = "Calibration every (days)")]
        [Range(0, 3650)]
        public int CalibrationIntervalDays { get; set; }

        [Display(Name = "Last maintenance")]
        public DateTime? LastMaintenanceDate { get; set; }
        [Display(Name = "Next maintenance due")]
        public DateTime? NextMaintenanceDue { get; set; }

        [Display(Name = "Last calibration")]
        public DateTime? LastCalibrationDate { get; set; }
        [Display(Name = "Next calibration due")]
        public DateTime? NextCalibrationDue { get; set; }

        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;

        public ICollection<EquipmentServiceRecord> ServiceRecords { get; set; } = new List<EquipmentServiceRecord>();
    }

    /// <summary>Maintenance, calibration, repair, inspection or a staff fault report.</summary>
    public class EquipmentServiceRecord
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public Equipment Equipment { get; set; } = null!;

        /// <summary>Preventive maintenance, Calibration, Repair, Inspection, Fault report.</summary>
        [Required, StringLength(30)]
        public string ServiceType { get; set; } = "Preventive maintenance";

        public DateTime ServiceDate { get; set; } = DateTime.Today;

        [StringLength(150)]
        [Display(Name = "Performed by")]
        public string PerformedBy { get; set; } = string.Empty;

        /// <summary>Pass, Adjusted, Repaired, Fail, Reported.</summary>
        [StringLength(20)]
        public string Result { get; set; } = "Pass";

        public decimal Cost { get; set; }

        [StringLength(100)]
        [Display(Name = "Certificate / job no.")]
        public string CertificateNumber { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
