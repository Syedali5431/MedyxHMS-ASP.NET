using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class FrontOfficeDashboardViewModel
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public List<VisitorLog> Visitors { get; set; } = new();
        public List<ComplaintRecord> Complaints { get; set; } = new();
        public List<DispatchReceiveRecord> DispatchReceiveRecords { get; set; } = new();
    }

    public class VisitorPageViewModel
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public List<VisitorLog> Visitors { get; set; } = new();
        public VisitorLog NewVisitor { get; set; } = new VisitorLog { VisitDate = DateTime.Today, Status = "CheckedIn" };
    }

    public class ComplaintPageViewModel
    {
        public string StatusFilter { get; set; }
        public List<ComplaintRecord> Complaints { get; set; } = new();
        public ComplaintRecord NewComplaint { get; set; } = new ComplaintRecord { Status = "Open" };
    }

    public class DispatchReceivePageViewModel
    {
        public string RecordTypeFilter { get; set; }
        public DateTime? DateFilter { get; set; }
        public List<DispatchReceiveRecord> Records { get; set; } = new();
        public DispatchReceiveRecord NewRecord { get; set; } = new DispatchReceiveRecord { RecordType = "Dispatch" };
    }
}
