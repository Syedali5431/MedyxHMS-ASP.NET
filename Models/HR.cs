// Purpose: Contains application code for HR and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class StaffAttendance
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = "Present"; // Present, Absent, HalfDay, OnLeave
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; } = null!;
    }

    public class LeaveType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DefaultDaysPerYear { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LeaveRequest
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        public string Reason { get; set; } = string.Empty;
        public string ApproverId { get; set; } = string.Empty;
        public DateTime? ApprovedDate { get; set; }
        public string ApproverRemarks { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; } = null!;
        public LeaveType LeaveType { get; set; } = null!;
    }

    public class LeaveBalance
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public int Year { get; set; }
        public int AllocatedDays { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; } = null!;
        public LeaveType LeaveType { get; set; } = null!;
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public DateTime PayrollMonth { get; set; } // Store as first day of month
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Processed, Paid
        public DateTime? PaymentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; } = null!;
    }

    public class VisitorLog
    {
        public int Id { get; set; }
        public string VisitorName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string PersonToMeet { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; } = DateTime.Today;
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = "CheckedIn"; // CheckedIn, CheckedOut, Cancelled
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ComplaintRecord
    {
        public int Id { get; set; }
        public string ComplainantName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
        public string ResolutionNotes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedDate { get; set; }
    }

    public class DispatchReceiveRecord
    {
        public int Id { get; set; }
        public string RecordType { get; set; } = string.Empty; // Dispatch, Receive
        public string ReferenceNumber { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ContentSummary { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Logged";
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class CertificateRecord
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string CertificateType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Staff Staff { get; set; } = null!;
    }

    public class IdCardRecord
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Replaced
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Staff Staff { get; set; } = null!;
    }

    // User Action Logging
    public class UserActionLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // Login, Logout, AccessDenied, PasswordChange, PermissionChange
        public string Details { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public DateTime LoggedDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Success"; // Success, Failed

        public Staff Staff { get; set; } = null!;
    }

    // Report Entities
    public class GeneratedReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty; // Department, Financial, Occupancy, Staff, Attendance, Payroll
        public string Description { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; } // Nullable for system-wide reports
        public string FilePath { get; set; } = string.Empty;
        public string FileFormat { get; set; } = "PDF"; // PDF, Excel, HTML
        public long FileSize { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Available"; // Available, Archived, Deleted

        public Staff StaffGenerated { get; set; } = null!;
    }

    public class ReportSchedule
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string RecurrencePattern { get; set; } = string.Empty; // Daily, Weekly, Monthly, Quarterly, Yearly
        public int? DayOfWeek { get; set; } // 0=Sunday, 1=Monday, etc. (for weekly)
        public int? DayOfMonth { get; set; } // 1-31 (for monthly/quarterly/yearly)
        public string TimeOfDay { get; set; } = string.Empty; // HH:mm format
        public bool IsActive { get; set; } = true;
        public string EmailRecipients { get; set; } = string.Empty; // Comma-separated emails
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastRunDate { get; set; }
        public DateTime? NextRunDate { get; set; }

        public Staff StaffCreated { get; set; } = null!;
    }
}
