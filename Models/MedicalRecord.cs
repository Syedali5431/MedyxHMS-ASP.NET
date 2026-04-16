using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedyxHMS.Models
{
    [Table("MedicalRecords")]
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        [Required]
        [StringLength(100)]
        public string RecordType { get; set; } // "OPD", "IPD", "Lab", "Radiology", "Prescription"

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string Diagnosis { get; set; }

        [StringLength(2000)]
        public string Treatment { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        [StringLength(100)]
        public string DoctorName { get; set; }

        public string DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; }

        [NotMapped]
        public virtual Staff Staff { get; set; }

        [NotMapped]
        public virtual Prescription Prescription { get; set; }

        public DateTime RecordDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string ModifiedBy { get; set; }

        // Navigation properties for related records
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
        public virtual ICollection<OPDVisit> OPDVisits { get; set; } = new List<OPDVisit>();
        public virtual ICollection<IPDAdmission> IPDAdmissions { get; set; } = new List<IPDAdmission>();
    }

    [Table("TestResults")]
    public class TestResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        [Required]
        [StringLength(100)]
        public string TestType { get; set; } // "Lab", "Radiology"

        [Required]
        [StringLength(200)]
        public string TestName { get; set; }

        [StringLength(500)]
        public string TestDescription { get; set; }

        public string Result { get; set; }

        [StringLength(50)]
        public string Unit { get; set; }

        [StringLength(50)]
        public string ReferenceRange { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // "Normal", "Abnormal", "Critical", "Pending"

        [StringLength(100)]
        public string PerformedBy { get; set; }

        public string DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; }

        public DateTime TestDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string ModifiedBy { get; set; }

        // For linking to specific medical record
        public int? MedicalRecordId { get; set; }

        [ForeignKey("MedicalRecordId")]
        public virtual MedicalRecord MedicalRecord { get; set; }

        // Computed properties
        public string StatusBadgeClass => Status?.ToLower() switch
        {
            "normal" => "badge-success",
            "abnormal" => "badge-warning",
            "critical" => "badge-danger",
            "pending" => "badge-info",
            _ => "badge-secondary"
        };

        public string FormattedTestDate => TestDate.ToString("MMM dd, yyyy 'at' hh:mm tt");
        public string FormattedResult => $"{Result} {Unit}".Trim();
    }
}