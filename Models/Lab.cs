// Purpose: Contains application code for Lab and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class LabTest
    {
        public int Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Hematology, Biochemistry, Microbiology, etc.
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string NormalRange { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int PreparationTimeHours { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LabResult
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int LabTestId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string ResultValue { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty; // Normal, High, Low, Abnormal
        public string Status { get; set; } = string.Empty; // Ordered, In Progress, Completed, Cancelled
        public string PerformedBy { get; set; } = string.Empty;
        public string VerifiedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public LabTest LabTest { get; set; } = null!;
        public ICollection<LabNoteHistory> NoteHistory { get; set; } = new List<LabNoteHistory>();
    }
}
