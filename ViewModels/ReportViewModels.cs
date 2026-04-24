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

    /// <summary>
    /// R1: Daily Transaction Report
    /// Shows all financial transactions (payments, refunds, adjustments) for a given date
    /// </summary>
    public class DailyTransactionReportViewModel
    {
        public List<dynamic> TransactionData { get; set; } = new();
        public DateTime ReportDate { get; set; } = DateTime.UtcNow.Date;
        public decimal TotalTransactions { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalRefunds { get; set; }
        public int TransactionCount { get; set; }
    }

    /// <summary>
    /// R2: All Transaction Report
    /// Shows all financial transactions within a date range
    /// </summary>
    public class AllTransactionReportViewModel
    {
        public List<dynamic> TransactionData { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalRefunds { get; set; }
        public int TransactionCount { get; set; }
        public Dictionary<string, decimal> BreakdownByType { get; set; } = new();
    }

    /// <summary>
    /// R3: Appointment Report
    /// Shows all appointments within a date range with status breakdown
    /// </summary>
    public class AppointmentReportViewModel
    {
        public List<dynamic> AppointmentData { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int ScheduledAppointments { get; set; }
        public decimal CompletionRate { get; set; }
        public Dictionary<string, int> AppointmentsByType { get; set; } = new();
        public Dictionary<string, int> AppointmentsByDoctor { get; set; } = new();
    }

    /// <summary>
    /// R4: OPD Report
    /// Shows out-patient visits, diagnoses, and consultation fees
    /// </summary>
    public class OPDReportViewModel
    {
        public List<dynamic> OPDVisitData { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalVisits { get; set; }
        public int UniquePatients { get; set; }
        public decimal TotalConsultationFees { get; set; }
        public decimal AverageConsultationFee { get; set; }
        public int PaidVisits { get; set; }
        public int PendingPaymentVisits { get; set; }
        public Dictionary<string, int> VisitsByDoctor { get; set; } = new();
    }

    /// <summary>
    /// R5: IPD Report
    /// Shows in-patient admissions, length of stay, and discharge status
    /// </summary>
    public class IPDReportViewModel
    {
        public List<dynamic> IPDAdmissionData { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalAdmissions { get; set; }
        public int DischargedPatients { get; set; }
        public int CurrentlyAdmitted { get; set; }
        public double AverageLengthOfStay { get; set; }
        public decimal TotalDailyCharges { get; set; }
        public Dictionary<string, int> AdmissionsByType { get; set; } = new();
        public Dictionary<string, int> AdmissionsByWard { get; set; } = new();
    }
}
