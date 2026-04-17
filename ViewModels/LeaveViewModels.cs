using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class LeaveIndexViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string StaffIdFilter { get; set; }
        public string StatusFilter { get; set; }
        public List<Staff> StaffOptions { get; set; } = new();
        public List<LeaveRequest> LeaveRequests { get; set; } = new();
    }

    public class LeaveRequestCreateViewModel
    {
        public LeaveRequest LeaveRequest { get; set; } = new LeaveRequest
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today
        };

        public List<Staff> StaffOptions { get; set; } = new();
        public List<LeaveType> LeaveTypes { get; set; } = new();
    }

    public class LeaveTypeViewModel
    {
        public List<LeaveType> LeaveTypes { get; set; } = new();
        public LeaveType NewLeaveType { get; set; } = new LeaveType { IsActive = true, DefaultDaysPerYear = 12 };
    }

    public class LeaveBalanceViewModel
    {
        public int Year { get; set; } = DateTime.Today.Year;
        public string StaffIdFilter { get; set; }
        public List<Staff> StaffOptions { get; set; } = new();
        public List<LeaveBalance> Balances { get; set; } = new();
    }
}
