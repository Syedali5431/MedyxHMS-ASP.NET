namespace MedyxHMS.Models
{
    public class StaffAttendance
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = "Present"; // Present, Absent, HalfDay, OnLeave
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; }
    }

    public class LeaveType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DefaultDaysPerYear { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LeaveRequest
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        public string Reason { get; set; }
        public string ApproverId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApproverRemarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; }
        public LeaveType LeaveType { get; set; }
    }

    public class LeaveBalance
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public int LeaveTypeId { get; set; }
        public int Year { get; set; }
        public int AllocatedDays { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; }
        public LeaveType LeaveType { get; set; }
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public DateTime PayrollMonth { get; set; } // Store as first day of month
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Processed, Paid
        public DateTime? PaymentDate { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Staff Staff { get; set; }
    }

    public class VisitorLog
    {
        public int Id { get; set; }
        public string VisitorName { get; set; }
        public string Phone { get; set; }
        public string Purpose { get; set; }
        public string PersonToMeet { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.Today;
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = "CheckedIn"; // CheckedIn, CheckedOut, Cancelled
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ComplaintRecord
    {
        public int Id { get; set; }
        public string ComplainantName { get; set; }
        public string Phone { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
        public string ResolutionNotes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedDate { get; set; }
    }

    public class DispatchReceiveRecord
    {
        public int Id { get; set; }
        public string RecordType { get; set; } // Dispatch, Receive
        public string ReferenceNumber { get; set; }
        public string PartyName { get; set; }
        public string ContactNumber { get; set; }
        public string ContentSummary { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Logged";
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class CertificateRecord
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public string CertificateType { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Staff Staff { get; set; }
    }

    public class IdCardRecord
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public string CardNumber { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Replaced
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Staff Staff { get; set; }
    }

    // User Action Logging
    public class UserActionLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ActionType { get; set; } // Login, Logout, AccessDenied, PasswordChange, PermissionChange
        public string Details { get; set; }
        public string IPAddress { get; set; }
        public DateTime LoggedDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Success"; // Success, Failed

        public Staff Staff { get; set; }
    }

    // Report Entities
    public class GeneratedReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; }
        public string ReportType { get; set; } // Department, Financial, Occupancy, Staff, Attendance, Payroll
        public string Description { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; } // Nullable for system-wide reports
        public string FilePath { get; set; }
        public string FileFormat { get; set; } = "PDF"; // PDF, Excel, HTML
        public long FileSize { get; set; }
        public string GeneratedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Available"; // Available, Archived, Deleted

        public Staff StaffGenerated { get; set; }
    }

    public class ReportSchedule
    {
        public int Id { get; set; }
        public string ReportName { get; set; }
        public string ReportType { get; set; }
        public string RecurrencePattern { get; set; } // Daily, Weekly, Monthly, Quarterly, Yearly
        public int? DayOfWeek { get; set; } // 0=Sunday, 1=Monday, etc. (for weekly)
        public int? DayOfMonth { get; set; } // 1-31 (for monthly/quarterly/yearly)
        public string TimeOfDay { get; set; } // HH:mm format
        public bool IsActive { get; set; } = true;
        public string EmailRecipients { get; set; } // Comma-separated emails
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastRunDate { get; set; }
        public DateTime? NextRunDate { get; set; }

        public Staff StaffCreated { get; set; }
    }
}
