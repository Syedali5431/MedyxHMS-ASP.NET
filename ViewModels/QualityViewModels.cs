using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class QualityDashboardViewModel
    {
        public int OpenIncidents { get; set; }
        public int CriticalOpenIncidents { get; set; }
        public int IncidentsThisMonth { get; set; }
        public int OpenCapa { get; set; }
        public int OverdueCapa { get; set; }
        public int CapaAwaitingVerification { get; set; }
        public int DocumentsDueForReview { get; set; }
        public int DocumentsPendingApproval { get; set; }
        public int UpcomingAudits { get; set; }
        public int OpenFindings { get; set; }
        public int TrainingExpiringSoon { get; set; }
        public int TrainingExpired { get; set; }
        public int EquipmentOverdue { get; set; }
        public int EquipmentDueSoon { get; set; }
        public int EquipmentOutOfService { get; set; }
        public List<QualityIncident> RecentIncidents { get; set; } = new();
        public List<CapaAction> OverdueCapaList { get; set; } = new();
    }

    public class ControlledDocumentCreateViewModel
    {
        public ControlledDocument Document { get; set; } = new();
        public string ChangeSummary { get; set; } = "First issue";
        public IFormFile? File { get; set; }
    }

    public class TrainingIndexViewModel
    {
        public List<TrainingRecord> Records { get; set; } = new();
        public bool IsManager { get; set; }
        public string? Filter { get; set; }
        public string? StaffId { get; set; }
        public List<MedyxHMS.Services.Implementations.StaffOption> Staff { get; set; } = new();
    }
}
