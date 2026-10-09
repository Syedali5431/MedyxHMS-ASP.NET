using System.ComponentModel.DataAnnotations;

// Purpose: Discharge report of an in-patient admission. Created when the patient is discharged (pre-filled from the
// admission), completed by a doctor or nurse, printed/downloaded as PDF, and shown to the patient in the portal.
namespace MedyxHMS.Models
{
    public class DischargeSummary : IHospitalScoped
    {
        public const string StatusDraft = "Draft";
        public const string StatusCompleted = "Completed";

        /// <summary>Choices for the patient's condition at discharge.</summary>
        public static readonly string[] Conditions =
        {
            "Recovered", "Improved", "Stable", "Referred to another hospital", "Discharged against medical advice", "Expired"
        };

        public int Id { get; set; }

        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public int IPDAdmissionId { get; set; }
        public IPDAdmission Admission { get; set; } = null!;

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public DateTime AdmissionDate { get; set; }
        public DateTime DischargeDate { get; set; }

        [StringLength(60)]
        public string ConditionAtDischarge { get; set; } = "Improved";

        [StringLength(1000)]
        public string ReasonForAdmission { get; set; } = string.Empty;

        [StringLength(1000)]
        public string FinalDiagnosis { get; set; } = string.Empty;

        /// <summary>Course in hospital: treatment given and how the patient progressed.</summary>
        [StringLength(4000)]
        public string HospitalCourse { get; set; } = string.Empty;

        [StringLength(2000)]
        public string ProceduresPerformed { get; set; } = string.Empty;

        [StringLength(2000)]
        public string InvestigationsSummary { get; set; } = string.Empty;

        [StringLength(2000)]
        public string DischargeMedications { get; set; } = string.Empty;

        /// <summary>Diet, activity and wound-care advice for home.</summary>
        [StringLength(2000)]
        public string AdviceOnDischarge { get; set; } = string.Empty;

        [StringLength(1000)]
        public string FollowUpInstructions { get; set; } = string.Empty;

        public DateTime? FollowUpDate { get; set; }

        [StringLength(150)]
        public string AttendingDoctor { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = StatusDraft;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [StringLength(150)]
        public string CreatedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(150)]
        public string? CompletedByName { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool IsCompleted => Status == StatusCompleted;

        /// <summary>Report number shown on the document, e.g. DS-2026-000012.</summary>
        public string ReportNumber => $"DS-{DischargeDate:yyyy}-{Id:D6}";
    }
}
