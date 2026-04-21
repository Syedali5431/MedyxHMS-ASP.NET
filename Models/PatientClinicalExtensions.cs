using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.Models
{
    public class PatientInsurance
    {
        public int Id { get; set; }
        public int PatientId { get; set; }

        [StringLength(100)]
        public string ProviderName { get; set; } = string.Empty;

        [StringLength(100)]
        public string PolicyNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string InsurancePlan { get; set; } = string.Empty;

        [StringLength(100)]
        public string HolderName { get; set; } = string.Empty;

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        [StringLength(100)]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public Patient Patient { get; set; } = null!;
    }

    public class VisitNoteHistory
    {
        public int Id { get; set; }
        public int OPDVisitId { get; set; }

        [StringLength(4000)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(256)]
        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public OPDVisit OPDVisit { get; set; } = null!;
    }

    public class LabNoteHistory
    {
        public int Id { get; set; }
        public int LabResultId { get; set; }

        [StringLength(4000)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(256)]
        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public LabResult LabResult { get; set; } = null!;
    }

    public class SystemNotification
    {
        public long Id { get; set; }

        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        public int? PatientId { get; set; }

        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "General";

        [StringLength(100)]
        public string RelatedEntityType { get; set; } = string.Empty;

        [StringLength(100)]
        public string RelatedEntityId { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAtUtc { get; set; }

        public ApplicationUser User { get; set; } = null!;
        public Patient? Patient { get; set; }
    }
}
