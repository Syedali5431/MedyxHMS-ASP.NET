// Purpose: Contains application code for AccountApprovalRequest and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class AccountApprovalRequest
    {
        public int Id { get; set; }
        public string RequestedUserId { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
    }
}
