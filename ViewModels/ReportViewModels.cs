using MedyxHMS.Models;

// Purpose: Contains application code for ReportViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    public class ReportIndexViewModel
    {
        public int TotalReportsGenerated { get; set; }
        public int TotalScheduledReports { get; set; }
        public DateTime LastReportGenerated { get; set; }
    }

    public class DepartmentReportViewModel
    {
        public List<dynamic> ReportData { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalStaff { get; set; }
        public decimal AverageAttendance { get; set; }
    }

    public class FinancialReportViewModel
    {
        public decimal TotalPayroll { get; set; }
        public decimal TotalBills { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal NetRevenue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class OccupancyReportViewModel
    {
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public double OccupancyPercentage { get; set; }
        public double AverageOccupancyRate { get; set; }
        public DateTime ReportDate { get; set; }
    }

    public class StaffReportViewModel
    {
        public List<dynamic> AttendanceReport { get; set; }
        public string StaffId { get; set; }
        public string StaffName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PayrollReportViewModel
    {
        public List<dynamic> PayrollData { get; set; }
        public DateTime PayrollMonth { get; set; }
        public decimal TotalSalary { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNetSalary { get; set; }
        public int TotalEmployees { get; set; }
    }

    public class GeneratedReportViewModel
    {
        public IEnumerable<GeneratedReport> Reports { get; set; }
        public string ReportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalReports { get; set; }
    }

    public class ReportScheduleViewModel
    {
        public IEnumerable<ReportSchedule> Schedules { get; set; }
        public ReportSchedule NewSchedule { get; set; }
        public List<string> RecurrencePatterns { get; set; }
        public List<string> ReportTypes { get; set; }
    }

    public class CreateReportScheduleViewModel
    {
        public string ReportName { get; set; }
        public string ReportType { get; set; }
        public string RecurrencePattern { get; set; }
        public int? DayOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
        public string TimeOfDay { get; set; }
        public string EmailRecipients { get; set; }
        public bool IsActive { get; set; } = true;

        public List<string> RecurrencePatterns { get; set; } = new()
        {
            "Daily",
            "Weekly",
            "Monthly",
            "Quarterly",
            "Yearly"
        };

        public List<string> ReportTypes { get; set; } = new()
        {
            "Department",
            "Financial",
            "Occupancy",
            "Staff",
            "Attendance",
            "Payroll"
        };
    }

    public class DashboardReportViewModel
    {
        public int TotalAuditLogs { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int DataModifications { get; set; }
        public int ReportsGenerated { get; set; }
        public Dictionary<string, int> TopActions { get; set; }
        public DateTime LastAuditLogDate { get; set; }
    }
}
