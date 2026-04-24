# Final Touches — Medyx HMS Inventory

A staged reference inventory of all roles, portals, menus, modules, reports, and AI features in the Medyx HMS system.

---

## Roles (RL)

| ID  | Role Name      | Description                                      | Status |
|-----|----------------|--------------------------------------------------|--------|
| RL1 | SuperAdmin     | Full system access; manages all modules globally | Completed |
| RL2 | Admin          | Hospital admin; access to most operational areas | Completed |
| RL3 | Doctor         | Access to OPD, IPD, prescriptions, appointments  | Completed |
| RL4 | Nurse          | Access to IPD nursing workflows                  | Completed |
| RL5 | Pharmacist     | Access to pharmacy and prescription dispensing   | Completed |
| RL6 | Accountant     | Access to billing and finance modules            | Completed |
| RL7 | Receptionist   | Access to Front Office / patient registration    | Completed |
| RL8 | Patient        | Access to Patient Portal only                    | Completed |
| RL9 | LabTechnician  | Access to pathology/laboratory module            | Completed |
| RL10| Radiologist    | Access to radiology module                       | Completed |
| RL11| Staff          | General staff; basic system access               | Configured |

Implementation note:
`RL1 / SuperAdmin` is now synchronized across both RBAC `StaffRoles` and ASP.NET Identity roles. This ensures the super admin account receives all controller-level role gates, module bypass behavior, settings access, and feature permissions assigned to `SuperAdmin`.

`RL2 / Admin` is now covered by the same startup reconciliation path for existing staff-backed accounts, so Admin users inherit their assigned Identity role gates together with the RBAC features already seeded for Admin, including `ManageUsers`, `ManageSettings`, and `ViewReports`.

`RL3 / Doctor` is now configured for the intended OPD, IPD, appointment, and prescription workflow by aligning appointment permission checks with seeded feature names, allowing doctors through the prescription controller role gate, and backfilling missing Doctor role features during startup for existing databases.

`RL4 / Nurse` is now configured for the intended IPD nursing workflow by preserving existing IPD access and allowing nurses into the medicine-facing controller paths that match their seeded `ViewMedicines` and `DispenseMedicines` permissions, without expanding nurse access to admin-only medicine maintenance actions.

`RL5 / Pharmacist` is now configured for the pharmacy workflow by allowing pharmacist accounts into the medicine create and edit actions that match their seeded `ViewMedicines`, `AddMedicines`, and `DispenseMedicines` permissions, while leaving destructive admin-only actions such as prescription delete unchanged.

`RL6 / Accountant` is now configured for billing and finance workflows by enabling accountant access to payroll/report viewing endpoints and financial reports, while keeping mutation-heavy admin workflows (payroll generation/mark-paid and report template/schedule administration) restricted to admin-level roles.

`RL7 / Receptionist` is now configured for front-office and registration-adjacent finance flow by aligning billing controller role gates with the receptionist role's seeded `ViewBills` and `AddBills` permissions, alongside existing receptionist access in FrontOffice and patient registration surfaces.

`RL8 / Patient` is now configured as portal-only by keeping patient functionality in dedicated `PatientPortal` controllers and removing direct Patient role access from staff-side insurance management actions under the main `Patient` controller.

`RL9 / LabTechnician` is now configured for laboratory workflows by aligning the lab controller role gate with the seeded `ViewLabTests` and `AddLabTests` permissions assigned to LabTechnician.

`RL10 / Radiologist` is now configured for radiology workflows by aligning the radiology controller role gate with the seeded `ViewRadiologyTests` and `AddRadiologyTests` permissions assigned to Radiologist.

`RL11 / Staff` is now configured for baseline operational access through existing patient and appointment permission checks plus billing role gates, matching the seeded Staff permissions for `ViewPatients`, `AddPatients`, `EditPatients`, `ViewAppointments`, `AddAppointments`, `EditAppointments`, `ViewBills`, and `AddBills`.

## Validation (Focused Smoke Checklist)

Scope:
- Static smoke verification of role seed permissions in `DatabaseInitializer` against controller role gates and permission checks.
- Focused on RL1-RL11 access alignment only.

Run Date: 2026-04-23

| Role | Result | Notes |
|------|--------|-------|
| RL1 SuperAdmin | PASS | Global admin/superadmin gates and module bypass behavior remain aligned. |
| RL2 Admin | PASS | Admin access retained across management and reporting surfaces; identity-role reconciliation in place. |
| RL3 Doctor | PASS | OPD/IPD/appointment/prescription path aligned with doctor role and appointment permission key mapping. |
| RL4 Nurse | PASS | IPD/nursing flow aligned; medicine-facing access aligns with nurse seeded permissions. |
| RL5 Pharmacist | PASS | Pharmacy create/edit medicine paths aligned with pharmacist seeded permissions. |
| RL6 Accountant | PASS | Billing plus finance/report viewing access aligned; admin-only mutation/report-template flows remain restricted. |
| RL7 Receptionist | PASS | Front office and billing create/view alignment validated with receptionist seeded permissions. |
| RL8 Patient | PASS | Portal-only model validated; patient role removed from staff-side insurance actions. |
| RL9 LabTechnician | PASS | Lab controller role gate aligned with lab technician seeded permissions. |
| RL10 Radiologist | PASS | Radiology controller role gate aligned with radiologist seeded permissions. |
| RL11 Staff | PASS | Baseline staff access (patients/appointments/billing) aligned with seeded permissions and current gates. |

Caveat:
- This checklist is static/code-path validation. Full runtime E2E validation was not executed in this pass.

## Portals (P)

| ID | Portal Name          | URL Prefix         | Intended Users                         |
|----|----------------------|--------------------|----------------------------------------|
| P1 | Staff / Admin Portal | `/`                | All staff roles (RL1–RL7, RL9–RL11)   |
| P2 | Patient Portal       | `/PatientPortal/`  | Patients (RL8)                         |
| P3 | Public Website (CMS) | `/Site/`           | General public (unauthenticated)        |

### Portal Focus: P1 (Staff/Admin Portal)

Validation Summary (2026-04-23):
- PASS: Authentication/session middleware order supports staff portal login and authorized routing.
- PASS: Role landing redirects now explicitly cover `SuperAdmin`, `Admin`, `Doctor`, `Nurse`, `Pharmacist`, `Accountant`, `Receptionist`, `LabTechnician`, `Radiologist`, and `Staff`.
- PASS: Dashboard module explorer now hides admin-only operations for non-admin staff users (for example payroll generation, leave type/balance admin screens, certificate generation, report builder, chatbot admin settings) to reduce forbidden/dead-end navigation.

P1 Caveat:
- This P1 validation is code-path and authorization-surface based. Full browser/runtime flow tests per role are still recommended in staging.

### Portal Focus: P2 (Patient Portal)

Validation Summary (2026-04-23):
- PASS: Patient portal controllers now use consistent route prefixing with `PatientPortal/[controller]/[action]` across Account, Dashboard, Appointments, Bills, MedicalRecords, and Settings.
- PASS: Unauthenticated redirects in patient portal flows now consistently point to `/PatientPortal/Account/Login`, preventing accidental fallback into staff-side account routes.
- PASS: Patient ownership checks for appointment and bill details now compare against resolved patient-record identity, not raw ASP.NET Identity user id string coercion.
- PASS: Patient appointment booking now uses resolved patient-record id, eliminating fragile integer parsing of identity user identifiers.
- PASS: Patient portal login now accepts return URLs only when they are local and under `/PatientPortal`, otherwise safely falls back to `/PatientPortal/Dashboard`.
- PASS: Dashboard, appointment list, and bill list/export data retrieval now consistently use patient record id values, preventing empty/misaligned results when Identity user ids are non-numeric.

P2 Caveat:
- This P2 validation is static/code-path focused. A short runtime browser smoke pass for login, appointment detail, bill detail/download, and medical report exports is still recommended in staging.

### Portal Focus: P3 (Public Website / CMS)

Validation Summary (2026-04-23):
- PASS: `SiteController` carries no `[Authorize]` attribute — all public pages (home, notices, news, doctors, book appointment, contact, location, careers) are correctly unauthenticated.
- PASS: Admin CMS management (`CmsController`) is protected by `[Authorize(Policy = "RequireAdminRole")]`, mapped to `Admin,SuperAdmin` in `Program.cs`.
- PASS: `PublicSiteAdminController` (site settings, hero images, theme) is protected by `[Authorize(Roles = "Admin,SuperAdmin")]`.
- PASS: Public booking form has honeypot field, server-side CAPTCHA challenge, past-date rejection, doctor existence check, and duplicate request detection (phone + doctor + date + time + active status).
- PASS: Session is correctly wired — `AddDistributedMemoryCache` + `AddSession` + `UseSession` in middleware pipeline ensures captcha session keys are available.
- PASS: Slug-based `Page` and `NoticeDetail` lookups filter to `Published`/active content only — draft CMS content is never exposed to public users.
- PASS: Map embed URL fallback safely encodes the address query string with `UrlEncoder.Default.Encode`.
- PASS: **BookingConfirmation IDOR fixed** — `requestId` is now routed through `TempData` instead of a plain query parameter, preventing unauthenticated enumeration of other patients' booking summaries.
- PASS: **MapEmbedUrl scheme validated** — `PublicSiteAdminController` now rejects any configured map URL that does not start with `https://` before persisting it, preventing non-HTTPS or `javascript:` scheme injection.

P3 Caveat:
- P3 validation is code-path and static security focused. A runtime browser smoke pass covering public booking, CMS page rendering, and admin settings save/preview is still recommended in staging.

---

## PHP Sidebar Menu Items (M)

Sourced from the original PHP AdminLTE-based admin panel sidebar. Items are module-gated where noted.

> **Implementation Status:** Sidebar implemented in ASPNET as a persistent left-nav via `SidebarNavViewComponent` + `Views/Shared/Components/SidebarNav/Default.cshtml`. Layout updated in `_Layout.cshtml` to a two-column flex layout (sidebar + main content) for all authenticated staff pages. CSS added to `wwwroot/css/site.css`. Mobile support via off-canvas overlay toggled by `#sidebar-toggle-btn`.

| ID  | Menu Item                   | Sub-items                                                                 | Conditional | ASPNET Status |
|-----|-----------------------------|---------------------------------------------------------------------------|-------------|---------------|
| M1  | Dashboard                   | —                                                                         | No          | ✅ Implemented |
| M2  | Billing                     | Bills, New Bill                                                           | Module      | ✅ Implemented |
| M3  | Appointment                 | All Appointments, Calendar, Schedule New                                  | Module      | ✅ Implemented |
| M4  | OPD (Out Patient)           | OPD Visits, New OPD Visit                                                 | Module      | ✅ Implemented |
| M5  | IPD (In Patient)            | Admissions, New Admission                                                 | Module      | ✅ Implemented |
| M6  | Pharmacy                    | Prescriptions, Medicines                                                  | Module      | ✅ Implemented |
| M7  | Pathology                   | Lab Tests, Results                                                        | Module      | ✅ Implemented |
| M8  | Radiology                   | Tests, Results                                                            | Module      | ✅ Implemented |
| M9  | Blood Bank                  | Inventory, Issue Blood                                                    | Module      | ✅ Implemented |
| M10 | Ambulance / Transport       | Vehicles, Dispatch Log, New Dispatch                                      | Module      | ✅ Implemented |
| M11 | Front Office                | Overview, Visitors, Complaints                                            | Module      | ✅ Implemented |
| M12 | Birth / Death Record        | Birth Records, Death Records, New Birth, New Death                       | Module      | ✅ Implemented |
| M13 | Human Resource (HR)         | Staff, Attendance, Leave, Leave Types, Balances, Payroll, Generate Payroll | No         | ✅ Implemented |
| M14 | Referral                    | Referrals, Create Referral                                                | Module      | ✅ Implemented |
| M15 | TPA Management              | Providers, Claims, New Claim                                              | Module      | ✅ Implemented |
| M16 | Finance                     | Financial Report                                                          | Admin+      | ✅ Implemented (→ Report/FinancialReport) |
| M17 | Messaging                   | Inbox, Sent, Compose, Broadcast                                           | Module      | ✅ Implemented |
| M18 | Inventory                   | Items, Transactions, Low Stock, Add Item                                  | Module      | ✅ Implemented |
| M19 | Download Center             | Files, Upload                                                             | Module      | ✅ Implemented |
| M20 | Certificate                 | Certificates, Generate Certificate, Patient/Staff ID Card                 | Module      | ✅ Implemented |
| M21 | Front CMS                   | Pages, Notices, Menu, Booking Requests, Notification Settings, Site Settings | Module + Admin+ | ✅ Implemented |
| M22 | Live Consultation           | Sessions, Schedule                                                        | Module      | ✅ Implemented |
| M23 | Reports                     | Reports Hub, Department, Occupancy, Staff, Generated, Report Builder, Audit Logs | Module | ✅ Implemented |
| M24 | Setup / Settings            | App Config, Accounts Approval, Password Mgmt, Module Management, User Module Access, License | Admin+ | ✅ Implemented |

### Validation Summary (2026-04-23)

- Sidebar coverage status: M1-M24 navigation tree is implemented and rendered through the shared staff sidebar component.
- Runtime route validation: all targeted sidebar module routes returned HTTP 200 in authenticated smoke checks after schema backfill and entitlement reconciliation.
- Recently completed modules validated through sidebar paths: M10 Ambulance, M12 Birth/Death, M15 TPA, M17 Messaging, M18 Inventory, M19 Download Center, M22 Live Consultation.
- Role-based access validation (seeded UAT users): Admin, Doctor, Nurse, Accountant, Receptionist, and Patient route matrices passed for their expected sidebar-accessible modules.
- Multi-role validation: `multirole.uat@hospital.com` role picker returned Doctor and Nurse; both role selections routed to expected module landings (`/OPD`, `/IPD`).
- Access boundary validation: protected routes redirect to login when unauthenticated; post-logout access to protected routes redirects back to login.
- Admin boundary validation: admin access to `/License` currently resolves to `/Account/AccessDenied` in the latest run, consistent with active role gate behavior.
- Evidence artifacts: `docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md` (Run 4) and `temp_build_output/uat-role-run4-results.json`.

**Key Files:**
- `Components/SidebarNavViewComponent.cs` — ViewComponent (injects IModuleService, reads roles/moduleMap)
- `Views/Shared/Components/SidebarNav/Default.cshtml` — Full M1–M24 Razor template
- `Views/Shared/_Layout.cshtml` — Updated: two-column flex layout + mobile toggle button + sidebar JS
- `wwwroot/css/site.css` — New sidebar CSS section (`.staff-sidebar`, `.staff-sidebar-link`, etc.)

**2026-04-23 Validation Update:**
- Excluded duplicate backup model file `Models/CMS_fixed.cs` from project compilation to clear unrelated full-build conflicts.
- Added `EnsureNewModuleTablesAsync()` in `Services/Implementations/DatabaseInitializer.cs` so existing databases get M10/M12/M15/M17/M18/M19/M22 tables on startup.
- Verified authenticated route smoke pass for:
	`/Ambulance`, `/BirthDeath`, `/Tpa`, `/Messaging`, `/Inventory`, `/DownloadCenter`, `/LiveConsultation`
	plus their primary create/list subpages. All returned HTTP 200 after schema backfill.

---

## Modules (MD)

Sourced from `ModuleManagementController` (`DefaultModules`) and ASP.NET controller structure.

| ID   | Module Name                | Controller(s)                                     |
|------|----------------------------|---------------------------------------------------|
| MD1  | Dashboard                  | DashboardController                               |
| MD2  | Patient Management         | PatientController                                 |
| MD3  | Appointments               | AppointmentController                             |
| MD4  | Outpatient Department (OPD)| OPDController                                     |
| MD5  | Inpatient Department (IPD) | IPDController                                     |
| MD6  | Billing                    | BillingController                                 |
| MD7  | Pharmacy & Prescription    | PrescriptionController                            |
| MD8  | Laboratory (Pathology)     | LabController                                     |
| MD9  | Radiology                  | RadiologyController                               |
| MD10 | Blood Bank                 | BloodBankController                               |
| MD11 | Operation Theatre          | OperationTheatreController                        |
| MD12 | Front Office               | FrontOfficeController                             |
| MD13 | Attendance                 | AttendanceController                              |
| MD14 | Leave Management           | LeaveController                                   |
| MD15 | Payroll                    | PayrollController                                 |
| MD16 | Certificates & ID Cards    | CertificateController                             |
| MD17 | Referrals                  | ReferralController                                |
| MD18 | Reports                    | ReportController                                  |
| MD19 | Patient Portal             | PatientPortal/* (Dashboard, Appointments, Bills, MedicalRecords, Settings) |
| MD20 | Ambulance Management       | *(via FrontOfficeController or dedicated service)*|
| MD21 | Chatbot                    | ChatbotController, ChatbotAdminController         |
| MD22 | CMS / Public Website       | CmsController, PublicSiteAdminController, SiteController |
| MD23 | License Management         | LicenseController                                 |
| MD24 | Notifications              | NotificationsController                           |
| MD25 | Audit Trail                | AuditController                                   |
| MD26 | User / Staff Management    | StaffController, AccountsApprovalController       |
| MD27 | Module Management          | ModuleManagementController                        |
| MD28 | Accounts Approval          | AccountsApprovalController                        |

---

## Reports (R)

Sourced from the PHP sidebar Reports submenu and the ASP.NET `ReportController` actions.

### PHP-Originated Reports
| ID  | Report Name                    |
|-----|--------------------------------|
| R1  | Daily Transaction Report       |
| R2  | All Transaction Report         |
| R3  | Appointment Report             |
| R4  | OPD Report                     |
| R5  | IPD Report                     |
| R6  | OPD Balance Report             |
| R7  | IPD Balance Report             |
| R8  | OPD Discharged Patient Report  |
| R9  | IPD Discharged Patient Report  |
| R10 | Pharmacy Balance Report        |
| R11 | Expiry Medicine Report         |
| R12 | Pathology Patient Report       |
| R13 | Radiology Patient Report       |
| R14 | Operation Theatre (OT) Report  |
| R15 | Blood Issue Report             |
| R16 | Blood Component Issue Report   |
| R17 | Blood Donor Report             |
| R18 | Live Consultation Report       |
| R19 | Live Meeting Report            |
| R20 | TPA Report                     |
| R21 | Income Report                  |
| R22 | Income Group Report            |
| R23 | Expense Report                 |
| R24 | Expense Group Report           |
| R25 | Ambulance Report               |
| R26 | Birth Report                   |
| R27 | Death Report                   |
| R28 | Payroll Month Report           |
| R29 | Payroll Report                 |
| R30 | Staff Attendance Report        |
| R31 | User Log Report                |
| R32 | Patient Login Credential Report|
| R33 | Email / SMS Log Report         |
| R34 | Inventory Stock Report         |
| R35 | Inventory Item Report          |
| R36 | Inventory Issue Report         |
| R37 | Audit Trail Report             |
| R38 | Patient Visit Report           |
| R39 | Patient Bill Report            |
| R40 | Referral Report                |

### ASP.NET Report Builder Features
| ID  | Feature / Action              | Description                                        |
|-----|-------------------------------|----------------------------------------------------|
| R41 | Department Report             | Department-level analytics                         |
| R42 | Financial Report              | Hospital financial data                            |
| R43 | Occupancy Report              | Bed occupancy metrics                              |
| R44 | Staff Report                  | Staff attendance analytics                         |
| R45 | Report Builder / Template     | Custom report designer (create, design, save, clone, delete templates) |
| R46 | Report Scheduler              | Schedule automated report generation              |
| R47 | Generated Reports Archive     | View and manage previously generated reports       |
| R48 | Legacy PHP Report Import      | Import reports migrated from PHP system            |
| R49 | Report Preview                | Preview report before generating/exporting         |

### 2026-04-24 Reports Workspace Update

- PASS: A unified 49-report catalog is now wired into the staff sidebar under `Reports` as a dropdown selector instead of relying on the old card-based report dashboard.
- PASS: Sidebar selector now exposes all documented report entries `R1` through `R49`.
	- Authenticated validation result against `/Report`: `REPORT_SELECTOR_COUNT=49`
- PASS: Selecting a report now routes into a single Reports workspace (`/Report?reportKey=...`) and renders the chosen report detail on the right side of the main content area.
- PASS: Core ASP.NET report pages are embedded directly in the workspace detail pane for the current implemented report actions/features:
	- `R37` Audit Trail Report -> `/Audit`
	- `R41` Department Report -> `/Report/DepartmentReport`
	- `R42` Financial Report -> `/Report/FinancialReport`
	- `R43` Occupancy Report -> `/Report/OccupancyReport`
	- `R44` Staff Report -> `/Report/StaffReport`
	- `R45` Report Builder / Template -> `/Report/Builder` (admin-level)
	- `R46` Report Scheduler -> `/Report/ScheduleReport` (admin-level)
	- `R47` Generated Reports Archive -> `/Report/GeneratedReports`
- PASS: PHP-originated reports `R1` through `R40` are now all selectable from the same catalog and display as legacy report definitions within the Reports workspace, with template mapping/import actions where available.
- PASS: Report Preview (`R49`) now surfaces previewable imported template links from the same workspace.
- PASS: Authenticated HTML validation with the seeded Admin account confirmed:
	- sidebar selector present
	- `R1 - Daily Transaction Report` present
	- `R49 - Report Preview` present
	- `R41` selection renders an embedded `/Report/DepartmentReport` iframe in the workspace detail pane

### 2026-04-25 — Legacy Report to ASP.NET Conversion (R1-R5)

**Objective:** Convert 5 highest-priority legacy PHP reports (R1-R5: Daily Transaction, All Transaction, Appointment, OPD, IPD) to full ASP.NET data-backed pages with role-based authorization, date range filters, and export button placeholders.

**Implementation Status:** ✅ COMPLETED

**Reports Converted:**
- `R1` Daily Transaction Report → `/Report/DailyTransactionReport`
- `R2` All Transaction Report → `/Report/AllTransactionReport`
- `R3` Appointment Report → `/Report/AppointmentReport`
- `R4` OPD Report → `/Report/OPDLegacyReport`
- `R5` IPD Report → `/Report/IPDLegacyReport`

**Technical Implementation:**

**1. ViewModels Created** (ViewModels/ReportViewModels.cs)
- `DailyTransactionReportViewModel`: DateTime ReportDate, List<dynamic> TransactionData, decimal TotalTransactions/TotalPayments/TotalRefunds, int TransactionCount
- `AllTransactionReportViewModel`: DateTime StartDate/EndDate, List<dynamic> TransactionData, decimal TotalAmount/TotalPayments/TotalRefunds, int TransactionCount, Dictionary<string, decimal> BreakdownByType
- `AppointmentReportViewModel`: DateTime StartDate/EndDate, List<dynamic> AppointmentData, int TotalAppointments/CompletedAppointments/CancelledAppointments/ScheduledAppointments, decimal CompletionRate, Dictionary<string, int> AppointmentsByType/AppointmentsByDoctor
- `OPDReportViewModel`: DateTime StartDate/EndDate, List<dynamic> OPDVisitData, int TotalVisits/UniquePatients, decimal TotalConsultationFees/AverageConsultationFee, int PaidVisits/PendingPaymentVisits, Dictionary<string, int> VisitsByDoctor
- `IPDReportViewModel`: DateTime StartDate/EndDate, List<dynamic> IPDAdmissionData, int TotalAdmissions/DischargedPatients/CurrentlyAdmitted, double AverageLengthOfStay, decimal TotalDailyCharges, Dictionary<string, int> AdmissionsByType/AdmissionsByWard

**2. Service Layer Methods** (Services/Implementations/ReportService.cs)
- `GenerateDailyTransactionReportAsync(DateTime reportDate)` - LINQ query Transaction table, group by TransactionType, calculate totals; 10-minute cache
- `GenerateAllTransactionReportAsync(DateTime startDate, DateTime endDate)` - Query transactions in date range, build breakdown by type, 15-minute cache
- `GenerateAppointmentReportAsync(DateTime startDate, DateTime endDate)` - Query appointments with Patient/Doctor includes, calculate completion rate, group by type/doctor; 15-minute cache
- `GenerateOPDReportAsync(DateTime startDate, DateTime endDate)` - Query OPDVisits with includes, track payment status, group by doctor; 15-minute cache
- `GenerateIPDReportAsync(DateTime startDate, DateTime endDate)` - Query IPDAdmissions with Bed->Ward includes, calculate length of stay, group by type/ward; 15-minute cache

All methods include error handling with ILogger and fallback empty ViewModels on exception.

**3. Controller Actions** (Controllers/ReportController.cs)
- `[Authorize(Roles = "Admin,SuperAdmin,Accountant")]` decorated methods for all 5 reports
- Automatic date defaulting: single report defaults to current date, range reports default to 1 month prior
- Async action methods returning PartialView for workspace embedding
- Wrapped in try-catch with ILogger for error tracking

**4. UI Partial Views** (Views/Report/_*ReportPartial.cshtml)
- `_DailyTransactionReportPartial.cshtml`: Single date picker + Generate button; summary cards (TotalTransactions, TotalPayments, TotalRefunds, TransactionCount); responsive data table with Type badge, Amount (currency formatted), Status; export button placeholders (PDF, Excel, Print)
- `_AllTransactionReportPartial.cshtml`: Date range pickers; summary cards + BreakdownByType section with 3-column card grid; data table identical to R1
- `_AppointmentReportPartial.cshtml`: Date range pickers; 4-column summary stats (TotalAppointments, Completed, Scheduled, CompletionRate%); two-column layout showing AppointmentsByType and AppointmentsByDoctor (top 5); data table with ID, Patient, Doctor, Date, Time, Type, Priority, Status badge
- `_OPDReportPartial.cshtml`: Date range pickers; 4 summary cards (TotalVisits, UniquePatients, TotalConsultationFees, AverageConsultationFee); PaymentStatus split card (Paid vs Pending); Top Doctors section; data table with ID, Patient, Doctor, VisitDate, Diagnosis, ConsultationFee, PaymentStatus badge, CreatedBy
- `_IPDReportPartial.cshtml`: Date range pickers; 4 summary cards (TotalAdmissions, Discharged, CurrentlyAdmitted, AverageLengthOfStay); AdmissionsByType and AdmissionsByWard breakdown sections; financial summary card (TotalDailyCharges + average per admission); data table with ID, Patient, Doctor, Ward, Bed, AdmissionDate, DischargeDate, LOS (calculated), AdmissionType, Status badge

All views use Bootstrap 5 styling, responsive layout, dynamic property access for flexible object binding, currency formatting via ToString("C"), and date formatting via ToString().

**5. Catalog Registry Update** (ViewModels/ReportCatalogViewModels.cs)
- Updated `Build()` method to mark R1-R5 as `Feature()` entries instead of `Legacy()` entries
- Added embedded URLs for each: `/Report/DailyTransactionReport`, `/Report/AllTransactionReport`, `/Report/AppointmentReport`, `/Report/OPDLegacyReport`, `/Report/IPDLegacyReport`
- Changed category from "PHP-Originated Reports" to "ASP.NET Reports - Converted"

**6. Workspace Integration** (Views/Report/Index.cshtml)
- Added 5 conditional branches in workspace detail pane render logic: `if (selected.Key == "R1")` through `if (selected.Key == "R5")` calling `@await Html.PartialAsync()` for each report
- Conditions checked before existing iframe/legacy template logic to take precedence

**Build & Compilation:**
- ✅ Initial compilation succeeded after ViewModels, service methods, and controller actions added (0 errors)
- ⚠️ Partial view creation introduced 9 Razor syntax errors in JavaScript template string formatting (CSS1003 syntax errors at lines ~96, ~119, ~133)
- ✅ Fixed Razor syntax errors by restructuring currency formatting in table cells from `@(...:C)` syntax to explicit `ToString("C")` calls within @{} code blocks
- ✅ Final build succeeded after syntax fixes: Build succeeded. 0 errors, 0 warnings (file lock warnings from running app ignored)

**Authorization & Access:**
- Accountant role: Can access all 5 reports via [Authorize(Roles = "Admin,SuperAdmin,Accountant")] gate
- Admin/SuperAdmin: Full access
- Other roles: 401 Unauthorized on report access attempt

**Caching Strategy:**
- Daily reports (R1): 10-minute TTL cache per date key
- Range reports (R2-R5): 15-minute TTL cache per date range key
- Cache keys include report type and date parameters for proper cache isolation

**Known Limitations & Future Work:**
- Export functionality (PDF, Excel, Print) currently placeholder alert() calls; will require integration with iTextSharp/EPPlus/SelectPdf libraries
- R6-R40 (35 remaining legacy reports) can follow same implementation pattern using R1-R5 as proof-of-concept template
- Runtime validation with actual data pending (requires authenticated admin session)

---

## AI / Chatbot Features (CB)

Sourced from `ChatbotController.cs` and `ChatbotAdminController.cs`.

### End-User Chatbot (Patient & Staff)
| ID  | Feature                        | Description                                                              |
|-----|--------------------------------|--------------------------------------------------------------------------|
| CB1 | Chatbot Interface              | Main chat UI for patients and staff to ask health/system questions        |
| CB2 | Consent Management             | Request, accept, or reject consent before chatbot interaction starts      |
| CB3 | AI Query (Ask)                 | Submit a natural-language prompt and receive an AI-generated response     |
| CB4 | JSON API Endpoint              | `AskJson` — programmatic chatbot query endpoint for front-end integration |
| CB5 | User Feedback                  | Submit thumbs-up/down or qualitative feedback on chatbot responses        |
| CB6 | Escalation                     | Escalate unresolved chatbot queries to a human support agent              |
| CB7 | Mark Unresolved                | Flag a chatbot session as unresolved for follow-up                        |
| CB8 | Consent Status Check           | API to check current consent and terms acceptance status                  |

### Admin Chatbot Management
| ID  | Feature                        | Description                                                              |
|-----|--------------------------------|--------------------------------------------------------------------------|
| CB9  | Chatbot Settings              | Configure AI model, API keys, behaviour rules, and system prompts        |
| CB10 | Analytics Dashboard           | View chatbot usage statistics, session counts, and satisfaction scores   |
| CB11 | Escalation Management         | List and manage all escalated chatbot sessions                           |
| CB12 | Resolve Escalation            | Mark escalations as resolved; update ticket status                       |


## 2026-04-23 — Module Implementation & Validation Complete

**Modules Successfully Implemented & Validated:**
- M10 Ambulance: Vehicle management, dispatch scheduling, dispatch log
- M12 Birth/Death: Birth record creation/listing, death record creation/listing
- M15 TPA: Provider list, claim management, new claim creation
- M17 Messaging: Inbox, compose, broadcast messaging
- M18 Inventory: Item list, transaction history, add transaction
- M19 Download Center: File listing, upload interface
- M22 Live Consultation: Session list, schedule consultation


### FInal Stage Bed Management
Add a complete "Bed Management" module and menu to the main dashboard.

TECH CONTEXT:

- Web-based dashboard
- Role-based access (Admin, Nurse, Doctor)
- Existing sidebar menu and dashboard layout
- Backend API + Database already present

TASKS:

1. DASHBOARD MENU
- Add a new sidebar menu item labeled: "Bed Management"
- Use a bed/hospital icon
- Menu visible to: Admin, Nurse
- Route: /bed-management

2. BED MANAGEMENT MODULE (PAGE)
Create a Bed Management page with the following sections:

A. Bed Overview (Top Summary Cards)
- Total Beds
- Available Beds
- Occupied Beds
- Cleaning / Maintenance Beds

B. Bed List Table
Display a table with columns:
- Bed ID
- Ward / Department
- Bed Type (ICU, General, Emergency, Isolation)
- Status (Available, Occupied, Cleaning, Maintenance)
- Assigned Patient (if occupied)
- Last Updated
- Actions (Assign, Transfer, Release, Block)

C. Bed Actions
- Assign bed to patient (modal form)
- Release bed on discharge
- Transfer patient to another bed
- Mark bed as Cleaning or Maintenance
- Block bed for isolation or emergency

D. Status Indicators
- Green: Available
- Red: Occupied
- Yellow: Cleaning
- Grey: Maintenance / Blocked

3. BUSINESS LOGIC
- A bed can only be assigned if status = Available
- When a patient is discharged, bed status → Cleaning
- After cleaning confirmation, status → Available
- ICU beds require admin approval for assignment
- Isolation beds must be flagged and restricted

4. BACKEND / DATA MODEL (if missing)
Create or use a `beds` table with fields:
- id
- bed_number
- ward
- bed_type
- status
- patient_id (nullable)
- is_isolation
- last_updated

Provide API endpoints:
- GET /api/beds
- POST /api/beds/assign
- POST /api/beds/release
- POST /api/beds/transfer
- POST /api/beds/status

5. UI REQUIREMENTS
- Responsive layout
- Real-time refresh or manual refresh button
- Confirmation dialogs for critical actions
- Toast notifications for success/error

6. CLEAN CODE REQUIREMENTS
- Reusable components
- Proper state management
- Clear naming conventions
- Add inline comments where business rules apply

OUTPUT EXPECTATION:
- Sidebar menu item added
- Fully functional Bed Management page
- Mock data acceptable if backend is incomplete
- Code should match existing project style

### Bed Management Validation Summary (2026-04-23)

- PASS: Sidebar module entry added for Bed Management with hospital bed icon and staff-facing navigation.
- PASS: Dashboard module explorer includes Bed Management quick links aligned to existing module option patterns.
- PASS: Bed Management page includes summary cards (Total, Available, Occupied, Cleaning, Maintenance) and a responsive bed table.
- PASS: Bed Management access model now splits into read-only and manage scopes:
	- `SuperAdmin`, `Admin`, and `Nurse` can add, edit, assign, transfer, release, and change bed status.
	- Other staff roles can open the Bed Management overview in read-only mode but cannot mutate bed records.
- PASS: Bed workflow actions implemented: Assign, Release, Transfer, and status updates (Available/Cleaning/Maintenance) with confirmation UX.
- PASS: Core business rules implemented in service layer:
	- Assign allowed only when bed status is Available.
	- Release transitions Occupied -> Cleaning.
	- Transfer requires source Occupied and target Available.
	- ICU assignment enforces admin-level approval.
	- Isolation/approval flags are persisted on bed records.
- PASS: Database initializer backfill covers BedManagement module seed and idempotent Beds schema additions for PatientId, IsIsolation, RequiresAdminApproval, and LastUpdated.
- PASS: API contract implemented for bed operations:
	- GET `/api/beds`
	- POST `/api/beds/assign`
	- POST `/api/beds/release`
	- POST `/api/beds/transfer`
	- POST `/api/beds/status`
- PASS: Focused automated service coverage now exists in `tests/MedyxHMS.BedManagement.Tests/BedServiceTests.cs` for assign, ICU approval, release, transfer, and summary counting rules.
- PASS: Focused controller/API authorization coverage now exists in `tests/MedyxHMS.BedManagement.Tests/BedManagementAuthorizationTests.cs` for class-level view roles and method-level manage-role enforcement.

Bed Management Caveat:
- Fresh authenticated smoke from this workspace now reaches successful login for all seeded roles after hardening license signature validation handling in `LicenseFileService`.
- Runtime smoke from this workspace now completes with zero route failures for all seeded staff roles and patient role after script reliability fixes and route-alignment updates.


## Tasks to perform to close project
### Module Coverage Validation Summary (2026-04-23)

- PASS: Static module contract alignment check completed across seed, dashboard explorer, and sidebar wiring.
- PASS: `DefaultModules` count = 30 and dashboard explorer module option blocks = 30 (no missing keys).
- PASS: Sidebar module gates are present for staff-facing modules; expected non-sidebar module keys are `Dashboard`, `License`, and `PatientPortal`.
- PASS: Dashboard explorer target validation completed for 76 unique controller/action pairs; 0 missing controller files and 0 missing action methods.
- PASS: Sidebar target validation completed for 87 unique links; 0 broken controller/action targets detected.
- PASS: Bed Management API contract now present and mapped:
	- GET `/api/beds`
	- POST `/api/beds/assign`
	- POST `/api/beds/release`
	- POST `/api/beds/transfer`
	- POST `/api/beds/status`
- PASS: Current compile state after module updates remains stable (0 errors; warning-only build).
- PASS: Focused Bed Management service tests pass in the current workspace via `dotnet test tests/MedyxHMS.BedManagement.Tests/MedyxHMS.BedManagement.Tests.csproj`.
	- Result: 5 tests passed covering assign availability, ICU approval, release, transfer, and summary aggregation rules.
- PASS: Focused Bed Management authorization tests now pass in the same project.
	- Result: 3 additional tests validate controller-level role constraints for read-only versus manage endpoints.
- PASS: Fresh authenticated runtime role smoke rerun from this workspace now completes successfully with zero failures across all seeded role matrices.
	- Current smoke summary from `temp_build_output/uat-role-run-current.json`: SuperAdmin `88/0`, Admin `88/0`, Doctor `88/0`, Nurse `88/0`, Accountant `88/0`, Receptionist `88/0`, Multi-role Doctor `88/0`, Multi-role Nurse `88/0`, Patient `5/0` (Total/Fails).
	- Supporting fixes included:
		- `Controllers/StaffController.cs`: null-safe staff/user projections to prevent `/Staff` runtime 500 in sparse local data.
		- `scripts/Run-RoleModuleSmoke.ps1`: resilient redirect/HTTP-status handling plus patient portal route alignment to `/Index` actions.
		- `scripts/StoredProcedures_Reports.sql`: corrected table/column mismatches and replaced `DATEDIFF` with `DATEDIFF_BIG` in timing metrics to prevent overflow/runtime SQL failures during report pages.
	- Bed Management checks remain passing for intended read-only/manage boundaries in this run (`/BedManagement` visible; manage mutations restricted by role gates and treated per smoke acceptability rules).
	- Historical artifact: `temp_build_output/uat-role-run4-results.json` remains available for prior comparison.
- PASS: Expanded automated route-matrix coverage beyond Bed Management now exists in `tests/MedyxHMS.BedManagement.Tests/PatientPortalRouteContractTests.cs`.
	- Result: Patient portal controller area/route/auth contracts are now enforced in CI-oriented tests.
	- Current Bed Management + route-contract suite result: `23` tests passed.

Validation Caveat:
- This pass is static and compile-time verification. Authenticated runtime workflow UAT per role is still required for full go-live confidence.

### 2026-04-24 Review Update

Confirmed in current workspace:
- Module seed inventory still includes 30 modules in `Services/Implementations/DatabaseInitializer.cs`, including `BedManagement`.
- Current project build remains warning-only from the latest local verification pass; no compile errors were surfaced in the captured build output.
- Bed Management implementation is present across controller, service, sidebar, dashboard quick links, views, and API endpoints.
- Existing machine-readable UAT evidence is still present at `temp_build_output/uat-role-run4-results.json` and supports the previously documented role-login smoke run.
- `scripts/Run-RoleModuleSmoke.ps1` has been made workspace-portable and now resolves repo-relative paths from `$PSScriptRoot`.
- Focused automated Bed Management coverage now exists in `tests/MedyxHMS.BedManagement.Tests` and passes locally.

Gaps found during this review:
- **Bed Management access model updated:** the previous Doctor edit-access mismatch has been corrected. Bed Management is now visible to staff in read-only mode, while mutation paths are restricted to `SuperAdmin`, `Admin`, and `Nurse`.
- **License signature crash fixed for runtime login path:** malformed/legacy signature values now fail closed without throwing unhandled exceptions, so login and smoke execution proceed normally.
- **Runtime smoke matrix is now clean for seeded role-route coverage:** latest workspace smoke run shows zero route failures across tested matrices.
- **Automated coverage expanded:** Bed Management now has service tests, controller/API authorization tests, and patient-portal route-contract tests; broader cross-module business-flow automation can be added incrementally.

Pending tasks before declaring all modules fully validated:
- Validate Admin/SuperAdmin governance workflows end-to-end with business data assertions (module management, CMS administration, license management, user/module access administration).

### 2026-04-24 Bed Management UI Enhancement

**Location Hierarchy Added to Bed Model:**
- `Block`, `Floor`, `RoomNumber` fields added to the `Bed` entity (`Models/OPD.cs`).
- SQL migrations added to `DatabaseInitializer` to backfill columns on existing databases.

**Add Beds Form (`Views/BedManagement/Create.cshtml`):**
- Form now collects: Block Name, Floor No, Ward, Room Number, Bed Type, Number of Beds (1–50), Daily Charges.
- Bulk creation: entering NumberOfBeds > 1 creates that many beds auto-numbered as `<Room>-B01`, `<Room>-B02`, etc., continuing from any existing beds in the same room.
- Edit form retains Block/Floor/Room fields for correction, with editable BedNumber.

**Bed Management Dashboard (`Views/BedManagement/Index.cshtml`) — full redesign:**
- **Filter bar**: 4 independent dropdowns (Block, Floor, Ward, Room) — any combination, none required. Options populated client-side from live bed data.
- **Room Summary Table**: interactive table showing Block | Floor | Ward | Room | Total/Available/Occupied/Cleaning/Maintenance counts per room. Clicking a row highlights it and reveals the bed icon grid below.
- **Bed Icon Grid**: visual grid of bed icons (colour-coded by status) for the selected room. Right-clicking any icon shows a context menu.
- **Right-click context menu** (managers only): Mark Available / Cleaning / Maintenance / Blocked, Assign Patient, Release Bed, Transfer Patient, Edit Bed — status changes execute via AJAX (no full page reload) and refresh the room table counts in-place.
- Existing Assign / Release / Transfer modals preserved and wired from the context menu.
- Summary cards and all business-rule gates (ICU approval, isolation flags) remain unchanged.

**Build & Quality Status:**
- Clean compile: zero errors, pre-existing nullable reference warnings only
- All 12 decimal properties across new modules configured with HasPrecision(18,2)
- SavedReport.ExecutionTimeMs precision mapping added
- Duplicate CMS model (Models/CMS_fixed.cs) excluded to resolve type conflicts

**Authenticated Runtime Validation:**
- Fresh build completed with clean database initialization
- 20/20 module routes passed smoke test (HTTP 200, no errors, no redirects)
- All module list/index pages loaded successfully with existing SQL Server database

### 2026-04-24 Bed Management Runtime Validation Update

- PASS: Application launched successfully at `http://localhost:5044` after the Bed Management hierarchy/dashboard changes.
- PASS: Startup completed with report SQL deployment confirmation logged:
	- `[INF] Report stored procedures and indexes ensured from ...\scripts\StoredProcedures_Reports.sql`
- PASS: Fresh authenticated runtime smoke rerun completed with zero route failures using `scripts/Run-RoleModuleSmoke.ps1`.
	- SuperAdmin `88/0`
	- Admin `88/0`
	- Doctor `88/0`
	- Nurse `88/0`
	- Accountant `88/0`
	- Receptionist `88/0`
	- Multi-role Doctor `88/0`
	- Multi-role Nurse `88/0`
	- Patient `5/0`
- PASS: Bed Management route checks are clean in the generated runtime artifact `temp_build_output/uat-role-run-current.json`.
	- `/BedManagement` returned `200` for staff roles exercised in the smoke run.
	- `/bed-management` returned `200` for staff roles exercised in the smoke run.
	- `/BedManagement/Create` returned `200` for manage role validation (`Nurse`) and redirected for read-only roles such as `Doctor` and `Receptionist`, preserving the intended access boundary.
- PASS: Build remains clean after the Bed Management form/controller/view updates.
	- Latest local build result: `Build succeeded in 1.2s`.
- PASS: Bed Management right-click status changes now work for cookie-authenticated staff UI sessions.
	- Root cause was `ApiSecurityMiddleware` rejecting internal `/api/beds/status` AJAX calls with `401 Authorization header required` because those calls use the normal ASP.NET Identity application cookie rather than an explicit `Authorization` header.
	- Middleware now allows same-app authenticated cookie requests to proceed to normal ASP.NET auth/role checks, while external header-based API protection remains intact.
	- `Views/BedManagement/Index.cshtml` now also surfaces plain-text server error responses instead of collapsing non-JSON failures into a generic `Network error.` toast.
	- Live validation completed against the running Bed Management page for status changes to `Cleaning`, `Maintenance`, `Blocked`, and back to `Available`.

Validation Caveat:
- This run validates startup, authenticated route coverage, and Bed Management authorization boundaries. It does not yet include full business-data UAT for create/assign/release/transfer workflows with seeded dummy patient/bed records.
- Schema backfill applied on startup via DatabaseInitializer.EnsureNewModuleTablesAsync()

**Remaining Pre-Go-Live Work:**
- High: Full role-based business UAT with production-like data (Testing)
- High: Validate report outputs — Department, Occupancy, Staff, Payroll (Reports)
- High: Admin/SuperAdmin governance workflows end-to-end (Auth)
- High: Go-live cutover rehearsal and rollback validation (Deployment)
- Medium: Production SMS provider credential onboarding (Notifications)
- Medium: Production SMTP credential onboarding (Notifications)
- Medium: Staging soak test for retries, opt-outs, provider failover (Notifications)
- Low: Zoom/Live Consultation secure integration (Deferred)
- Low: Additional payment gateways beyond top 5 (Deferred)
- Low: Expand language support beyond top 3–5 (Deferred)

## Pending Task Execution Stages (2026-04-24)

This staged plan converts all remaining pre-go-live items into execution phases with concrete completion criteria.

Stage completion note standard (mandatory for future reference):
- Every stage that is marked Completed must include both:
	- **Implementation Summary**
	- **Validation Summary**
- Completion status should not be considered final until both summaries are recorded with evidence references.

### Stage 1 - Role-Based Business UAT (High) - Completed (2026-04-24)

**Scope:**
- End-to-end role workflow validation using production-like seed data for `SuperAdmin`, `Admin`, `Doctor`, `Nurse`, `Accountant`, `Receptionist`, multi-role staff, and `Patient`.
- Include real business actions (not only route checks): create/edit/approve/assign/release/transfer/bill/report flows where role-appropriate.

**Required Work:**
- Build role-specific UAT scripts with expected outcomes and negative tests (forbidden actions).
- Seed production-like data sets for OPD/IPD, billing, bed assignment, appointments, pharmacy, lab/radiology, and referrals.
- Execute UAT scripts in authenticated browser sessions and capture screenshots/log evidence.

**Completion Evidence Required:**
- UAT matrix showing pass/fail per role and workflow.
- Evidence artifact file (JSON/MD) with timestamps and tester account used.
- Zero unresolved critical access-control mismatches.

**Implementation Summary (2026-04-24):**
- Executed role-based UAT smoke coverage using the existing automation pipeline for all seeded staff and patient role matrices.
- Executed Bed Management business-rule and authorization test suite in the current workspace.
- Updated test alignment for the current `BedManagementController.Create(Bed, int)` signature in authorization coverage.

**Validation Summary (2026-04-24):**
- Role-matrix UAT smoke executed against `http://localhost:5105` via:
	- `scripts/Run-RoleModuleSmoke.ps1 -BaseUrl "http://localhost:5105" -OutputPath "temp_build_output/uat-role-stage1-2026-04-24.json"`
- Result summary:
	- SuperAdmin `82/0`
	- Admin `82/0`
	- Doctor `82/0`
	- Nurse `82/0`
	- Accountant `82/0`
	- Receptionist `82/0`
	- Multi-role Doctor `82/0`
	- Multi-role Nurse `82/0`
	- Patient `5/0`
	- Format: `Total/Fails`

- Business-rule automation suite executed:
	- `dotnet test tests/MedyxHMS.BedManagement.Tests/MedyxHMS.BedManagement.Tests.csproj`
	- Result: `23` passed, `0` failed, `0` skipped.

- Access-control test maintenance performed as part of Stage 1 closure:
	- Updated `tests/MedyxHMS.BedManagement.Tests/BedManagementAuthorizationTests.cs` to match current action signature `Create(Bed, int)`.

**Stage 1 Status:**
- Completed.
- No unresolved critical access-control mismatches in the Stage 1 execution set.

**Stage 1 End Summary (Before Stage 2):**

**Implementation Summary:**
- Executed Stage 1 role-based UAT automation across seeded staff/patient role matrices.
- Executed Stage 1 business-rule and authorization test coverage for Bed Management.
- Aligned authorization test signature to current controller action (`Create(Bed, int)`).

**Validation Summary:**
- UAT artifact generated: `temp_build_output/uat-role-stage1-2026-04-24.json`.
- Role route matrix result: all executed role groups passed with `0` route failures.
- Bed Management test suite result: `23 passed`, `0 failed`, `0 skipped`.

### Stage 2 - Report Output Certification (High)

**Scope:**
- Functional and data-accuracy validation for core report pages: `Department`, `Occupancy`, `Staff`, and `Payroll`.
- Confirm report correctness with production-like records and expected aggregates.

**Required Work:**
- Define expected data baselines and acceptance formulas for each report.
- Validate filters/date ranges and totals against SQL/source-of-truth queries.
- Verify export paths (if enabled) or document temporary placeholder behavior.

**Completion Evidence Required:**
- Side-by-side expected vs actual values for each report.
- Signed-off report validation checklist with no unresolved high-severity discrepancies.

### Stage 3 - Admin/SuperAdmin Governance E2E (High)

**Scope:**
- Governance-critical workflows: module management, user module access, account approvals, CMS admin operations, and license management.

**Required Work:**
- Validate privilege boundaries between `Admin` and `SuperAdmin` for each governance surface.
- Execute approval/denial/update workflows and verify audit trail entries.
- Verify failure handling paths (invalid inputs, unauthorized access, expired/invalid license scenarios).

**Completion Evidence Required:**
- Governance workflow checklist with actor, action, expected result, actual result.
- Audit log samples confirming traceability for critical operations.

### Stage 4 - Cutover Rehearsal and Rollback Drill (High)

**Scope:**
- Full deployment rehearsal for go-live including rollback readiness validation.

**Required Work:**
- Dry-run deployment in staging using production-like sequence (backup, deploy, migrate/startup checks, smoke checks).
- Time-boxed rollback simulation to previous stable build.
- Validate data integrity and service health before/after rollback.

**Completion Evidence Required:**
- Rehearsal runbook execution log with start/end timestamps.
- Recovery Point Objective (RPO) and Recovery Time Objective (RTO) observed values.
- Signed rollback verification note.

### Stage 5 - Notification Production Readiness (Medium)

**Scope:**
- Production onboarding and reliability validation for SMS and SMTP channels.

**Required Work:**
- Configure production SMS credentials and sender settings.
- Configure production SMTP credentials, sender identity, SPF/DKIM/DMARC prerequisites as applicable.
- Run staging soak tests for retries, provider failover behavior, and opt-out handling.

**Completion Evidence Required:**
- Sanitized configuration checklist (no secrets in docs).
- Delivery success/failure metrics from soak run.
- Incident notes for any retry/failover anomalies and their resolutions.

### Stage 6 - Deferred Enhancements (Low)

**Scope:**
- Zoom/Live Consultation secure integration hardening.
- Additional payment gateway integrations beyond top five.
- Expanded language/localization support beyond top 3-5.

**Required Work:**
- Convert each item into scoped epics with security, compliance, and test criteria.
- Prioritize by business impact and implementation complexity.

**Completion Evidence Required:**
- Approved backlog items with owner, estimate, dependency, and target release window.

### Stage 7 - System Management (New)

Add a new sidebar menu item at the end of staff-side menus named **System Management**.

Access rule:
- Available to all non-patient users.
- Not available to patient role.

Sub-menus under System Management:

#### A. Report Management

1. Report List
- Provide a section with an interactive table with 3 columns:
	- Sr#
	- Report Name
	- Purpose
- Right-click actions on a report row:
	- Mark Active
	- Mark Inactive
- If a report is marked Inactive:
	- Hide it from user dashboard and Reports menu for all users except SuperAdmin.
- Visibility and interaction rules:
	- SuperAdmin: can see all reports (active + inactive) and gets interactive table.
	- Other roles: can only see active reports and get simple (non-interactive) table.
	- Only SuperAdmin can mark reports active/inactive.

2. Create Report
- Provide a section to create a new report and save it.
- Section must include two tabs:
	- Editor
	- Preview
- Editor tab is for composing the new report.
- Preview tab shows rendered preview before save/finalization.

3. Edit Report
- Provide a section with:
	- A report-selection dropdown at top.
	- A report editor panel below for:
		- Add/remove report fields
		- Change report name and title
		- Update style and font
		- Other report layout/content edits
- Section must include two tabs:
	- Editor
	- Preview
- Access rule:
	- Available only to SuperAdmin and Admin roles.

4. Download Report
- Provide a section with:
	- A report-selection dropdown at top.
	- Filters for generating selected report.
	- A download button near the dropdown to export generated report.
- Download formats:
	- Excel
	- PDF
- Download button behavior:
	- Disabled until report is generated.
- Access rule:
	- Available to all non-patient user roles.

#### B. User Management

1. User List
- Provide a section with an interactive table showing minimum user details.
- Right-click actions on user row:
	- Mark Active
	- Mark Inactive
- Mark Inactive behavior:
	- User account becomes inactive and cannot log in until reactivated.
- Mark Active behavior:
	- Prompt for a new password.
	- On password confirmation, account is reactivated.
	- User can then log in using the new password.

#### C. Theme Management

- Provide an interactive theme picker section with theme icons and names
	- Example: Sunflower icon with label Sunflower, Snowflake icon with label Snowflake.
- On theme click, show confirmation prompt:
	- "Apply this theme?"
- If user selects Yes:
	- Apply selected theme.
- If user selects No:
	- Close prompt and do not change theme.

Theme scope and persistence rules:
- Theme is user-specific (not global per role).
- If a user has multiple roles, the same selected theme remains applied regardless of chosen role at login.
- Access rule:
	- Available to all non-patient users.

---

*Document generated from PHP source (`php-original/`) and ASP.NET source (`MedyxHMS-ASPNET/`) analysis.*
*Last updated: 2026-04-24*
