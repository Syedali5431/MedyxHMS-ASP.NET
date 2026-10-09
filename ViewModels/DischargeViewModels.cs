using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

// Purpose: Discharge Patient form, discharge report editing and the Discharge Reports list.
namespace MedyxHMS.ViewModels
{
    /// <summary>Discharge Patient (new report for an admission) and Edit discharge report.</summary>
    public class DischargeFormViewModel
    {
        public int AdmissionId { get; set; }

        /// <summary>Set when an existing report is edited; empty on the Discharge Patient form.</summary>
        public int? SummaryId { get; set; }

        // Shown on the form (not posted back as data).
        public string PatientName { get; set; } = string.Empty;
        public string PatientCode { get; set; } = string.Empty;
        public string WardBed { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public string ReportNumber { get; set; } = string.Empty;

        [Required, Display(Name = "Discharge date and time")]
        public DateTime DischargeDate { get; set; } = DateTime.Now;

        [Required, StringLength(60), Display(Name = "Condition at discharge")]
        public string ConditionAtDischarge { get; set; } = "Improved";

        [StringLength(1000), Display(Name = "Reason for admission")]
        public string? ReasonForAdmission { get; set; }

        [Required, StringLength(1000), Display(Name = "Final diagnosis")]
        public string? FinalDiagnosis { get; set; }

        [StringLength(4000), Display(Name = "Course in hospital and treatment given")]
        public string? HospitalCourse { get; set; }

        [StringLength(2000), Display(Name = "Procedures / operations")]
        public string? ProceduresPerformed { get; set; }

        [StringLength(2000), Display(Name = "Investigations and results")]
        public string? InvestigationsSummary { get; set; }

        [StringLength(2000), Display(Name = "Medicines on discharge")]
        public string? DischargeMedications { get; set; }

        [StringLength(2000), Display(Name = "Advice (diet, activity, wound care)")]
        public string? AdviceOnDischarge { get; set; }

        [StringLength(1000), Display(Name = "Follow-up instructions")]
        public string? FollowUpInstructions { get; set; }

        [Display(Name = "Follow-up date")]
        public DateTime? FollowUpDate { get; set; }

        [StringLength(150), Display(Name = "Attending doctor")]
        public string? AttendingDoctor { get; set; }

        /// <summary>"Save and complete" instead of "Save draft".</summary>
        public bool Complete { get; set; }

        public bool IsNewDischarge => !SummaryId.HasValue;
    }

    /// <summary>Discharge Reports: all reports (staff), filtered by patient, name, period and status.</summary>
    public class DischargeReportListViewModel
    {
        public List<DischargeSummary> Reports { get; set; } = new();

        /// <summary>Patients still admitted, with a Discharge Patient button.</summary>
        public List<IPDAdmission> Admitted { get; set; } = new();

        public int? PatientId { get; set; }
        public string? Search { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Status { get; set; }
        public List<SelectListItem> PatientOptions { get; set; } = new();
    }
}
