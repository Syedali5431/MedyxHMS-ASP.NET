namespace MedyxHMS.Models
{
    public class LabTest
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestCode { get; set; }
        public string Category { get; set; } // Hematology, Biochemistry, Microbiology, etc.
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string NormalRange { get; set; }
        public string Unit { get; set; }
        public int PreparationTimeHours { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LabResult
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int LabTestId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string ResultValue { get; set; }
        public string NormalRange { get; set; }
        public string Unit { get; set; }
        public string Interpretation { get; set; } // Normal, High, Low, Abnormal
        public string Status { get; set; } // Ordered, In Progress, Completed, Cancelled
        public string PerformedBy { get; set; }
        public string VerifiedBy { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Patient Patient { get; set; }
        public LabTest LabTest { get; set; }
    }
}