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
        public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class BloodIssue
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsIssued { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; }
        public string CrossMatchStatus { get; set; } = "Pending";
        public string Notes { get; set; }
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Patient Patient { get; set; }
        public Bill Bill { get; set; }
    }

    public class OTSchedule
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string ProcedureName { get; set; }
        public string SurgeonName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public string OperationTheatreNumber { get; set; }
        public string Status { get; set; } = "Scheduled";
        public string Notes { get; set; }
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Patient Patient { get; set; }
        public Bill Bill { get; set; }
    }

    public class Referral
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string ReferralType { get; set; } = "External"; // External, Internal, TPA
        public string ReferredTo { get; set; }
        public string ReferralReason { get; set; }
        public DateTime ReferralDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public string TpaProvider { get; set; }
        public string TpaPolicyNumber { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string Notes { get; set; }
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Patient Patient { get; set; }
        public Bill Bill { get; set; }
    }
}
