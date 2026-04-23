// Purpose: Contains application code for OPD and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class OPDVisit
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime VisitDate { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        public string Prescription { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string PaymentStatus { get; set; } = string.Empty; // Paid, Pending, Waived
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
        public ICollection<VisitNoteHistory> NoteHistory { get; set; } = new List<VisitNoteHistory>();
    }

    public class IPDAdmission
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? BedId { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string AdmissionType { get; set; } = string.Empty; // Emergency, Planned
        public string Diagnosis { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Admitted, Discharged, Transferred
        public decimal DailyCharges { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
        public Bed Bed { get; set; } = null!;
    }

    public class Ward
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Bed> Beds { get; set; } = new List<Bed>();
    }

    public class Bed
    {
        public int Id { get; set; }
        public int WardId { get; set; }
        public string BedNumber { get; set; } = string.Empty;
        public string BedType { get; set; } = string.Empty; // General, ICU, Private, Semi-private, Isolation, Emergency
        public decimal DailyCharges { get; set; }
        // Status: Available, Occupied, Cleaning, Maintenance
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsIsolation { get; set; } = false;
        // ICU beds require admin approval before assignment
        public bool RequiresAdminApproval { get; set; } = false;
        // Assigned patient (nullable — set when Occupied)
        public int? PatientId { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Ward Ward { get; set; } = null!;
        public Patient? Patient { get; set; }
    }
}
