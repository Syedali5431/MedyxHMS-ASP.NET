// Purpose: Contains application code for SpecializedServices and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class BloodInventory
    {
        public int Id { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsAvailable { get; set; }
        public int UnitsReserved { get; set; }
        public int MinimumLevel { get; set; } = 5;
        public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class BloodIssue
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsIssued { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public string RequestedBy { get; set; }
        public string CrossMatchStatus { get; set; } = "Pending";
        public string Notes { get; set; }
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Patient Patient { get; set; }
        public Bill Bill { get; set; }
    }

    public class OTSchedule : IHospitalScoped
    {
        public int Id { get; set; }
        // Hospital of the group this record belongs to (set automatically from the active hospital).
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }
        public int PatientId { get; set; }
        public string ProcedureName { get; set; }
        public string SurgeonName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public string OperationTheatreNumber { get; set; }
        public string Status { get; set; } = "Scheduled";
        public string Notes { get; set; }
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Theatre master link (OperationTheatreNumber keeps the theatre code for older screens and reports).
        public int? OperationTheatreId { get; set; }
        public OperationTheatre? Theatre { get; set; }
        // Surgeon from the doctors list; SurgeonName also holds visiting surgeons who are not in the list.
        public int? SurgeonDoctorId { get; set; }
        public Doctor? Surgeon { get; set; }
        // Emergency cases may be booked outside the theatre's opening hours.
        public bool IsEmergency { get; set; }
        public string CancelReason { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Patient Patient { get; set; }
        public Bill Bill { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime EndsAt => ScheduledDate.AddMinutes(EstimatedDurationMinutes);
    }

    public class Referral
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string ReferralType { get; set; } = "External"; // External, Internal, TPA
        public string ReferredTo { get; set; }
        public string ReferralReason { get; set; }
        public DateTime ReferralDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        public string TpaProvider { get; set; }
        public string TpaPolicyNumber { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string Notes { get; set; }
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Patient Patient { get; set; }
        public Bill Bill { get; set; }
    }
}
