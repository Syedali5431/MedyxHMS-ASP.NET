namespace MedyxHMS.Models
{
    public class OPDVisit
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime VisitDate { get; set; }
        public string Symptoms { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string Prescription { get; set; }
        public string Notes { get; set; }
        public decimal ConsultationFee { get; set; }
        public string PaymentStatus { get; set; } // Paid, Pending, Waived
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }

        // Navigation properties
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
    }

    public class IPDAdmission
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? BedId { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string AdmissionType { get; set; } // Emergency, Planned
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; } // Admitted, Discharged, Transferred
        public decimal DailyCharges { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }

        // Navigation properties
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
        public Bed Bed { get; set; }
    }

    public class Ward
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Bed> Beds { get; set; }
    }

    public class Bed
    {
        public int Id { get; set; }
        public int WardId { get; set; }
        public string BedNumber { get; set; }
        public string BedType { get; set; } // General, ICU, Private, Semi-private
        public decimal DailyCharges { get; set; }
        public string Status { get; set; } // Available, Occupied, Maintenance
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Ward Ward { get; set; }
    }
}