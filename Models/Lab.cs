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
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    /// <summary>Chain-of-custody entry for a lab order: ordered, label printed, collected, received, rejected, result entered, signed off.</summary>
    public class LabSampleEvent
    {
        public int Id { get; set; }
        public int LabResultId { get; set; }
        public LabResult LabResult { get; set; } = null!;
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.Now;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
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
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public LabTest LabTest { get; set; } = null!;
        public ICollection<LabNoteHistory> NoteHistory { get; set; } = new List<LabNoteHistory>();

        // ── Specimen traceability (ISO 15189): accession number, chain of custody, electronic sign-off ──
        /// <summary>Unique specimen number printed as a barcode on the sample label.</summary>
        public string? AccessionNumber { get; set; }
        public string SampleType { get; set; } = string.Empty;
        /// <summary>Awaiting collection, Collected, Received, Rejected, Cancelled.</summary>
        public string SampleStatus { get; set; } = "Awaiting collection";
        public DateTime? CollectedAt { get; set; }
        public string CollectedBy { get; set; } = string.Empty;
        public DateTime? ReceivedAt { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime? ResultEnteredAt { get; set; }
        public string ResultEnteredBy { get; set; } = string.Empty;
        /// <summary>Set only by the electronic sign-off of a user with the Pathologist role (password re-entered).</summary>
        public DateTime? SignedOffAt { get; set; }
        public string SignedOffBy { get; set; } = string.Empty;
        public string SignedOffByUserId { get; set; } = string.Empty;
        /// <summary>SHA-256 of the signed content; any later change to the result makes it invalid.</summary>
        public string SignatureHash { get; set; } = string.Empty;
        public ICollection<LabSampleEvent> SampleEvents { get; set; } = new List<LabSampleEvent>();
    }
}
