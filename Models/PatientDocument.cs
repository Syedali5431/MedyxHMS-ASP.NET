using System.ComponentModel.DataAnnotations;

// Purpose: Documents in a patient's file (scanned reports, referral letters, consent forms, insurance papers and
// documents uploaded by the patient). Files are stored outside wwwroot and only served through access-checked actions.
namespace MedyxHMS.Models
{
    public class PatientDocument
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        [Required, StringLength(50)]
        public string Category { get; set; } = "Other";

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Date on the document itself (e.g. report date), if known.</summary>
        public DateTime? DocumentDate { get; set; }

        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [StringLength(100)]
        public string StoredFileName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        /// <summary>SHA-256 of the file, to show it has not changed since upload.</summary>
        [StringLength(64)]
        public string Sha256 { get; set; } = string.Empty;

        public bool UploadedByPatient { get; set; }

        /// <summary>Visible to the patient in the portal (always true for documents the patient uploaded).</summary>
        public bool SharedWithPatient { get; set; }

        public string UploadedByUserId { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Documents are never physically deleted from the record; removal hides them and keeps who/why.
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }

        [StringLength(300)]
        public string DeleteReason { get; set; } = string.Empty;
    }
}
