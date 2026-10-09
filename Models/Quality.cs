using System.ComponentModel.DataAnnotations;

// Purpose: Quality management (ISO 9001 / ISO 15189 / NABH-style): incidents with CAPA, controlled documents,
// internal audits and staff training records.
namespace MedyxHMS.Models
{
    /// <summary>An adverse event, near miss or complaint reported by staff (belongs to one hospital).</summary>
    public class QualityIncident : IHospitalScoped
    {
        public int Id { get; set; }
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        [StringLength(30)]
        public string IncidentNumber { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = "Patient safety";

        [Required, StringLength(20)]
        public string Severity { get; set; } = "Low";

        [Display(Name = "Date and time it happened")]
        public DateTime OccurredAt { get; set; } = DateTime.Now;
        public DateTime ReportedAt { get; set; } = DateTime.Now;

        [StringLength(150)]
        [Display(Name = "Location / department")]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Immediate action taken")]
        public string ImmediateAction { get; set; } = string.Empty;

        /// <summary>Optional: the patient involved (shared across hospitals).</summary>
        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }

        public string ReportedByUserId { get; set; } = string.Empty;
        public string ReportedByName { get; set; } = string.Empty;

        /// <summary>Open, Under investigation, CAPA in progress, Closed.</summary>
        [StringLength(30)]
        public string Status { get; set; } = "Open";

        [Display(Name = "Investigation / root cause")]
        public string RootCause { get; set; } = string.Empty;

        public DateTime? ClosedAt { get; set; }
        public string ClosedBy { get; set; } = string.Empty;
        public string ClosureNotes { get; set; } = string.Empty;

        public ICollection<CapaAction> CapaActions { get; set; } = new List<CapaAction>();
    }

    /// <summary>Corrective or preventive action, raised from an incident or an audit finding.</summary>
    public class CapaAction : IHospitalScoped
    {
        public int Id { get; set; }
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public int? IncidentId { get; set; }
        public QualityIncident? Incident { get; set; }
        public int? AuditFindingId { get; set; }
        public AuditFinding? AuditFinding { get; set; }

        /// <summary>Corrective or Preventive.</summary>
        [Required, StringLength(20)]
        public string ActionType { get; set; } = "Corrective";

        [Required]
        public string Description { get; set; } = string.Empty;

        public string OwnerUserId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        /// <summary>Open, In progress, Completed, Verified.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public string CompletionNotes { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public string VerifiedBy { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        /// <summary>Was the action effective? Recorded when verifying.</summary>
        public string EffectivenessNotes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>A policy, procedure (SOP), work instruction, form or manual under document control.</summary>
    public class ControlledDocument
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Document no.")]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = "Procedure (SOP)";

        [StringLength(100)]
        [Display(Name = "Owner department")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "Review every (months)")]
        [Range(1, 60)]
        public int ReviewIntervalMonths { get; set; } = 12;

        /// <summary>Draft (no approved version yet), Effective, Obsolete.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        public DateTime? NextReviewDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;

        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    }

    public class DocumentVersion
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public ControlledDocument Document { get; set; } = null!;

        public int VersionNumber { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;

        /// <summary>Stored file name (outside the public web folder) and the original name shown to users.</summary>
        public string StoredFileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }

        /// <summary>Draft, Pending approval, Approved, Rejected, Superseded.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? SubmittedAt { get; set; }

        public string ApprovedByUserId { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public string ApprovalComments { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
    }

    /// <summary>Planned internal audit (belongs to one hospital).</summary>
    public class InternalAudit : IHospitalScoped
    {
        public int Id { get; set; }
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        [StringLength(30)]
        public string AuditNumber { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(200)]
        [Display(Name = "Area / process audited")]
        public string Scope { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Standard / criteria")]
        public string Standard { get; set; } = "ISO 9001";

        [Display(Name = "Planned date")]
        public DateTime PlannedDate { get; set; } = DateTime.Today.AddDays(14);

        [StringLength(100)]
        [Display(Name = "Lead auditor")]
        public string LeadAuditor { get; set; } = string.Empty;

        /// <summary>Planned, In progress, Completed.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Planned";

        public DateTime? CompletedDate { get; set; }
        public string Summary { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;

        public ICollection<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
    }

    public class AuditFinding
    {
        public int Id { get; set; }
        public int AuditId { get; set; }
        public InternalAudit Audit { get; set; } = null!;

        /// <summary>Major non-conformity, Minor non-conformity, Observation, Opportunity for improvement.</summary>
        [Required, StringLength(40)]
        public string FindingType { get; set; } = "Minor non-conformity";

        [StringLength(50)]
        [Display(Name = "Clause / requirement")]
        public string Clause { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>Open, CAPA raised, Closed.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>A course or certification completed by a staff member.</summary>
    public class TrainingRecord
    {
        public int Id { get; set; }

        // No foreign key on purpose: training history is kept even if the staff record is later removed.
        [Required]
        [Display(Name = "Staff member")]
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string StaffDepartment { get; set; } = string.Empty;

        [Required, StringLength(200)]
        [Display(Name = "Course / certificate")]
        public string CourseTitle { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = "Induction";

        [StringLength(150)]
        [Display(Name = "Trainer / provider")]
        public string Provider { get; set; } = string.Empty;

        [Display(Name = "Completed on")]
        public DateTime CompletedDate { get; set; } = DateTime.Today;

        [Display(Name = "Valid until")]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Certificate no.")]
        public string CertificateNumber { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Result / score")]
        public string Result { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
