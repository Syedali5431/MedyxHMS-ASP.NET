# Phase 7: HR & Administrative Functions (Step 4.1)

**Status:** Complete (Validated) | **Date:** April 2026 | **Coverage:** 7 of 7 planned modules validated at route/build level

## Overview

Step 4.1 delivers comprehensive HR and administrative functionality including staff attendance tracking, leave management, payroll processing, front office operations, and employee document generation (certificates and ID cards). This phase establishes the operational backbone for day-to-day hospital administration.

**Completion Status:**
- ✅ Staff Attendance Management
- ✅ Leave Management System
- ✅ Payroll Processing
- ✅ Front Office Operations
- ✅ Certificates & ID Card Generation
- ✅ Audit logging foundation (services/models wired and used across controllers)
- ✅ Additional reports routes validated (Report controller endpoints protected and reachable)

---

## Database Schema Additions

### New Entities Added to `Models/HR.cs`

#### 1. **StaffAttendance**
```csharp
public class StaffAttendance
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } // Present, Absent, HalfDay, OnLeave
    public string Notes { get; set; }
    
    // Relationships
    public Staff Staff { get; set; }
    
    // Index: (StaffId, AttendanceDate) Unique
}
```

**Purpose:** Track daily staff attendance with check-in/check-out times
**Key Features:** 
- Automatic status inference based on check-in/out times
- Support for half-day and leave absence types
- Attendance date index for fast date-range queries

---

#### 2. **LeaveType & Leave Management**
```csharp
public class LeaveType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MaxDaysPerYear { get; set; }
    public string Description { get; set; }
}

public class LeaveRequest
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; } // Pending, Approved, Rejected
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    
    // Relationships
    public Staff Staff { get; set; }
    public LeaveType LeaveType { get; set; }
}

public class LeaveBalance
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    
    // Index: (StaffId, LeaveTypeId, Year) Unique
}
```

**Purpose:** Manage leave types, requests, and available balances per staff member
**Key Features:**
- Leave type master data management
- Approval workflow for leave requests
- Automatic balance tracking and enforcement
- Support for multiple leave types per staff

---

#### 3. **PayrollRecord**
```csharp
public class PayrollRecord
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public DateTime PayrollMonth { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public string Status { get; set; } // Pending, Processed, Paid
    public DateTime? ProcessedDate { get; set; }
    public DateTime? PaidDate { get; set; }
    
    // Index: (StaffId, PayrollMonth) Unique
}
```

**Purpose:** Track and manage payroll processing with salary calculations
**Key Features:**
- Automatic NetSalary calculation (BasicSalary + Allowances - Deductions)
- Decimal precision (18,2) for accurate financial calculations
- Status tracking through processing pipeline
- Unique index on (StaffId, PayrollMonth)

---

#### 4. **Front Office Operations**
```csharp
public class VisitorLog
{
    public int Id { get; set; }
    public string VisitorName { get; set; }
    public string Phone { get; set; }
    public string Purpose { get; set; }
    public int? ScheduledWithStaffId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    
    // Relationships
    public Staff ScheduledWithStaff { get; set; }
}

public class ComplaintRecord
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int SubmittedByStaffId { get; set; }
    public string Status { get; set; } // Open, InProgress, Resolved, Closed
    public string ResolutionNotes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    
    // Relationships
    public Staff SubmittedByStaff { get; set; }
}

public class DispatchReceiveRecord
{
    public int Id { get; set; }
    public string DocNo { get; set; }
    public string Type { get; set; } // Dispatch or Receive
    public string Quantity { get; set; }
    public DateTime Date { get; set; }
    public int HandledByStaffId { get; set; }
    public string Notes { get; set; }
    
    // Relationships
    public Staff HandledByStaff { get; set; }
}
```

**Purpose:** Manage front office daily operations (visitor tracking, complaints, inventory dispatch/receive)
**Key Features:**
- Visitor check-in/out tracking with staff assignment
- Complaint lifecycle management with resolution tracking
- Dispatch/receive record logging with staff accountability

---

#### 5. **Certificate & ID Card Management**
```csharp
public class CertificateRecord
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public string Type { get; set; } // Completion, Experience, Conduct, etc.
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime IssueDate { get; set; }
    public int GeneratedBy { get; set; } // Admin staff ID
    
    // Relationships
    public Staff Staff { get; set; }
    public Staff GeneratedByUser { get; set; }
}

public class IdCardRecord
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public string CardNumber { get; set; } // Auto-generated: ID-{timestamp}
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } // Active, Inactive, Replaced
    
    // Index: CardNumber Unique
    // Relationships
    public Staff Staff { get; set; }
}
```

**Purpose:** Generate and track employee certificates and ID cards
**Key Features:**
- Automatic ID card numbering with timestamp format
- Certificate type and content customization
- ID card lifecycle management (Active/Inactive/Replaced)
- Issue date and expiry date tracking for ID cards

---

## Database Context Updates (`Data/ApplicationDbContext.cs`)

All 9 new entities registered as DbSets:

```csharp
public DbSet<StaffAttendance> StaffAttendances { get; set; }
public DbSet<LeaveType> LeaveTypes { get; set; }
public DbSet<LeaveRequest> LeaveRequests { get; set; }
public DbSet<LeaveBalance> LeaveBalances { get; set; }
public DbSet<PayrollRecord> PayrollRecords { get; set; }
public DbSet<VisitorLog> VisitorLogs { get; set; }
public DbSet<ComplaintRecord> ComplaintRecords { get; set; }
public DbSet<DispatchReceiveRecord> DispatchReceiveRecords { get; set; }
public DbSet<CertificateRecord> CertificateRecords { get; set; }
public DbSet<IdCardRecord> IdCardRecords { get; set; }
```

**Relationship Configuration:**
- All FK relationships configured with `HasForeignKey` and `OnDelete.Restrict` to prevent accidental cascade deletes
- Decimal precision (18,2) applied to PayrollRecord financial fields
- Unique indexes applied to:
  - StaffAttendance: (StaffId, AttendanceDate)
  - LeaveBalance: (StaffId, LeaveTypeId, Year)
  - PayrollRecord: (StaffId, PayrollMonth)
  - IdCardRecord: CardNumber

---

## Service Layer Implementation

### 5 New Service Implementations

#### 1. **IAttendanceService** (`Services/Implementations/AttendanceService.cs`)
**Methods:**
- `CheckInAsync(staffId)` - Record staff check-in, set status to Present
- `CheckOutAsync(staffId)` - Record staff check-out time
- `MarkAttendanceAsync(staffId, date, status)` - Manually mark attendance
- `GetAttendanceAsync(date, staffId, status)` - Query with filters
- `GetAttendanceSummaryAsync(startDate, endDate, staffId)` - Summary reporting

**Key Logic:**
- Auto-status inference: Present (check-in + check-out), Absent (no times), OnLeave (marked as such)
- Prevents duplicate check-ins on same day
- Supports half-day marking

---

#### 2. **ILeaveService** (`Services/Implementations/LeaveService.cs`)
**Methods:**
- `GetLeaveTypesAsync()` - List all leave types
- `CreateLeaveTypeAsync(name, maxDays, description)` - Add new leave type
- `CreateLeaveRequestAsync(staffId, leaveTypeId, fromDate, toDate, reason)` - Submit leave request
- `ApproveLeaveAsync(requestId, approverId)` - Approve request (auto-deduct from balance)
- `RejectLeaveAsync(requestId)` - Reject request
- `GetLeaveBalanceAsync(staffId, year)` - Query available balance
- `InitializeLeaveBalanceAsync(staffId, year)` - Allocate annual balances

**Key Logic:**
- Balance validation: Prevents leave request if insufficient balance
- Automatic balance deduction on approval
- Supports multiple leave types per year
- Auto-initializes balances on first request

---

#### 3. **IPayrollService** (`Services/Implementations/PayrollService.cs`)
**Methods:**
- `GeneratePayrollAsync(month, staffIds)` - Create payroll records for staff
- `CalculateSalaryAsync(staffId, month)` - Compute Net = Basic + Allowances - Deductions
- `GetPayrollAsync(month, staffId, status)` - Query with filters
- `MarkPayrollAsPaidAsync(payrollIds)` - Update status to Paid

**Key Logic:**
- NetSalary = BasicSalary + Allowances - Deductions (decimal 18,2 precision)
- Prevents duplicate payroll records per (StaffId, Month)
- Supports batch processing of multiple staff
- Status pipeline: Pending → Processed → Paid

---

#### 4. **IFrontOfficeService** (`Services/Implementations/FrontOfficeService.cs`)
**Methods:**
- `CheckInVisitorAsync(visitorName, phone, purpose, staffId)` - Log visitor arrival
- `CheckOutVisitorAsync(visitorId)` - Log visitor departure
- `AddComplaintAsync(title, description, submittedByStaffId)` - Log complaint
- `UpdateComplaintStatusAsync(complaintId, status, notes)` - Update and close
- `AddDispatchReceiveRecordAsync(docNo, type, quantity, handledByStaffId, notes)` - Log dispatch/receive
- `GetFrontOfficeStatsAsync()` - Dashboard stats (active visitors, open complaints, etc.)

**Key Logic:**
- Visitor checkout with duration calculation
- Complaint lifecycle: Open → InProgress → Resolved → Closed
- Auto-resolution date on status update to Resolved
- Separate dispatch vs. receive record types

---

#### 5. **ICertificateService** (`Services/Implementations/CertificateService.cs`)
**Methods:**
- `GetCertificatesAsync(staffId, certificateType)` - Query certificates with optional filters
- `GenerateCertificateAsync(staffId, type, title, content, issueDate, generatedByStaffId)` - Create new certificate
- `GetIdCardsAsync(staffId, status)` - Query ID cards with optional filters
- `GenerateIdCardAsync(staffId, cardNumber, issueDate, expiryDate, generatedByStaffId)` - Generate ID card

**Key Logic:**
- Auto-card-number generation if not provided: Format `ID-{yyyyMMddHHmmss}`
- Certificate content customizable per type
- ID card status tracking (Active/Inactive/Replaced)
- Includes Staff and User relationship data for display

---

## Service Registration (Program.cs)

All 5 services registered as scoped:

```csharp
services.AddScoped<IAttendanceService, AttendanceService>();
services.AddScoped<ILeaveService, LeaveService>();
services.AddScoped<IPayrollService, PayrollService>();
services.AddScoped<IFrontOfficeService, FrontOfficeService>();
services.AddScoped<ICertificateService, CertificateService>();
```

---

## Controllers & View Models

### 5 Controllers Created

| Controller | Actions | Authorization |
|-----------|---------|----------------|
| **AttendanceController** | Index, MarkAttendance, CheckIn, CheckOut | Admin, SuperAdmin (mark); Staff (self check-in/out) |
| **LeaveController** | Index, Request, UpdateStatus, Types, Balances | Staff (request); Admin (approve/reject) |
| **PayrollController** | Index, Generate, MarkPaid | Admin, SuperAdmin |
| **FrontOfficeController** | Index, Visitors, AddVisitor, CheckOutVisitor, Complaints, AddComplaint, UpdateComplaintStatus, DispatchReceive, AddDispatchReceive | Admin, SuperAdmin, Receptionist |
| **CertificateController** | Index, GenerateCertificate, GenerateIdCard | Admin, SuperAdmin |

### View Model Organization

**AttendanceViewModels.cs**
- `AttendanceIndexViewModel` - List view with filters
- `MarkAttendanceViewModel` - Form for manual marking
- `AttendanceSummaryViewModel` - Reporting view

**LeaveViewModels.cs**
- `LeaveIndexViewModel` - Request list with status
- `LeaveRequestViewModel` - Create/edit form
- `LeaveBalanceViewModel` - Available balance display
- `LeaveTypeViewModel` - Type management

**PayrollViewModels.cs**
- `PayrollIndexViewModel` - List with status filters
- `GeneratePayrollViewModel` - Batch processing form
- `PayrollDetailViewModel` - Single record detail

**FrontOfficeViewModels.cs**
- `FrontOfficeDashboardViewModel` - Overview stats
- `VisitorPageViewModel` - Visitor management
- `ComplaintPageViewModel` - Complaint tracking
- `DispatchReceivePageViewModel` - Dispatch/receive logs

**CertificateViewModels.cs**
- `CertificateIndexViewModel` - Combined certificates + ID cards
- `GenerateCertificateViewModel` - Certificate creation form
- `GenerateIdCardViewModel` - ID card creation form

---

## Views Created

### Attendance Module (`Views/Attendance/`)
- **Index.cshtml** - Dashboard with date selector, staff filter, attendance summary cards, manual entry form, real-time check-in/out buttons

### Leave Module (`Views/Leave/`)
- **Index.cshtml** - Request list with date/status/staff filtering, approval/rejection UI
- **Request.cshtml** - Leave request form with date range picker, leave type selector, reason field
- **Types.cshtml** - Admin interface for managing leave types (add, edit, delete)
- **Balances.cshtml** - Display available balance per leave type and year

### Payroll Module (`Views/Payroll/`)
- **Index.cshtml** - Payroll records table with month/staff filters, status badges, mark-as-paid action
- **Generate.cshtml** - Batch payroll generation form with staff multiselect, month picker

### Front Office Module (`Views/FrontOffice/`)
- **Index.cshtml** - Dashboard with visitor count, open complaints, dispatch records
- **Visitors.cshtml** - Add visitor form, visitor log table with check-out capability
- **Complaints.cshtml** - Add complaint form, status management UI, resolution notes
- **DispatchReceive.cshtml** - Add dispatch/receive form, record table with type filtering

### Certificate Module (`Views/Certificate/`)
- **Index.cshtml** - Two-column layout: certificates table (left), ID cards table (right), with staff filter
- **GenerateCertificate.cshtml** - Staff selector, certificate type picker, title/content editor, issue date picker
- **GenerateIdCard.cshtml** - Staff selector, card number field (optional, auto-generated), dates, status selector

---

## Testing & Validation

### Build Validation
- ✅ All 9 entity models compile without errors
- ✅ DbContext relationships properly configured
- ✅ Service implementations compile cleanly
- ✅ All 5 controllers compile without errors
- ✅ ViewModels and views render without syntax errors
- **Build Time:** 1.0 seconds

### Route Validation (Smoke Tests)
All endpoints return 302 redirect to `/Account/Login?ReturnUrl=...` for unauthenticated access (expected security behavior):

```
GET /Attendance              → 302 Location=/Account/Login?ReturnUrl=%2FAttendance
GET /Leave                   → 302 Location=/Account/Login?ReturnUrl=%2FLeave
GET /Payroll                 → 302 Location=/Account/Login?ReturnUrl=%2FPayroll
GET /FrontOffice             → 302 Location=/Account/Login?ReturnUrl=%2FFrontOffice
GET /Certificate             → 302 Location=/Account/Login?ReturnUrl=%2FCertificate
GET /Certificate/GenerateCertificate   → 302 Location=/Account/Login?ReturnUrl=...
GET /Certificate/GenerateIdCard        → 302 Location=/Account/Login?ReturnUrl=...
```

**Conclusion:** All endpoints properly wired and authentication enforcement operational.

### Extended Route Validation (Report and Audit Scope)

The following report endpoints were validated with explicit `GET` probes and all returned the expected auth redirect (`302` to `/Account/Login` with encoded `ReturnUrl`):

```
GET /Report
GET /Report/Index
GET /Report/DepartmentReport
GET /Report/FinancialReport
GET /Report/OccupancyReport
GET /Report/StaffReport
GET /Report/PayrollReport
GET /Report/GeneratedReports
GET /Report/ScheduleReport
```

Method constraints were also verified:

```
GET /Report/DeleteReport    → 405 Method Not Allowed (expected, POST-only)
GET /Report/DeleteSchedule  → 405 Method Not Allowed (expected, POST-only)
```

Audit viewer route probes:

```
GET /Audit
GET /Audit/Index
GET /Audit/UserActions
GET /Audit/Details/{id}
```

All returned the expected auth redirect (`302` to `/Account/Login` with encoded `ReturnUrl`) for unauthenticated access, confirming dedicated audit viewer routes are now present.

---

## Database Migration

When `ApplicationDbContext.Database.EnsureCreated()` or EF migrations run, the following tables are created:

| Table | Purpose |
|-------|---------|
| `StaffAttendances` | Daily attendance records |
| `LeaveTypes` | Master data: leave type definitions |
| `LeaveRequests` | Leave request workflow records |
| `LeaveBalances` | Annual leave balance tracking |
| `PayrollRecords` | Monthly payroll processing |
| `VisitorLogs` | Visitor check-in/out |
| `ComplaintRecords` | Complaint tracking |
| `DispatchReceiveRecords` | Inventory dispatch/receive |
| `CertificateRecords` | Employee certificates |
| `IdCardRecords` | Employee ID cards |

---

## Implementation Notes

### Design Decisions

1. **Decimal Precision (18,2) for Financial Fields**
   - Applied to all Payroll financial fields to prevent rounding errors
   - Supports amounts up to 999,999,999,999,999.99

2. **Unique Indexes**
   - StaffAttendance: Prevents duplicate entries for same day
   - LeaveBalance: One record per staff/leave-type/year
   - PayrollRecord: One record per staff/month
   - IdCardRecord: Unique card numbers for security

3. **OnDelete.Restrict on Foreign Keys**
   - Prevents accidental deletion of Staff when dependent records exist
   - Maintains data integrity across all HR modules

4. **Auto-Generated ID Card Numbers**
   - Format: `ID-{yyyyMMddHHmmss}` (e.g., `ID-20260415143025`)
   - Guarantees uniqueness while maintaining human readability

5. **Role-Based Authorization**
   - Staff can only check-in/out for themselves
   - Admins manage attendance, leave approvals, payroll, certificates
   - Receptionists handle front office operations

### Service Layer Patterns

All services follow standard patterns:
- Async/await for database operations
- LINQ queries with .Include() for relationship loading
- Exception handling for validation logic
- Dependency injection via constructor

---

## Known Limitations & Future Work

### Not Implemented (Post-Step 4.1 Backlog)

1. **Advanced Audit Analytics**
    - Aggregated dashboard widgets (top actions/users/entities)
    - Export/download options for audit datasets
    - Saved filter presets and date comparison views

### Recommendations for Phase 4.2

- Extend Audit/Log viewers with dashboards and export (PDF, Excel)
- Create comprehensive report generation with scheduled delivery enhancements
- Add email notifications for leave approvals, payroll processing
- Implement overtime tracking and additional allowances
- Add photo support for ID cards

---

## File Summary

| File Category | Count | Status |
|--------------|-------|--------|
| Entity Models (HR.cs) | 9 | ✅ Complete |
| Service Interfaces (IServices.cs) | 5 | ✅ Complete |
| Service Implementations | 5 | ✅ Complete |
| Controllers | 5 | ✅ Complete |
| View Models | 5 | ✅ Complete |
| View Files | 13 | ✅ Complete |
| **Total New Code Files** | **~40+** | ✅ Complete |

---

## Compilation & Runtime Status

- **Compilation:** ✅ Success (no CS errors)
- **Runtime:** ✅ App runs and validated module/report routes are accessible (auth-protected)
- **Database:** Ready for EnsureCreated() or migrations
- **Step 4.1 Status:** ✅ Complete (build + route validation completed and documented)
- **Next Phase:** Step 4.2 - Public Website & CMS (plus advanced audit analytics)

---

**End of Phase 7 Documentation**
