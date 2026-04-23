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


### FInal Stage Bed Management
Add a complete "Bed Management" module and menu to the main dashboard.

TECH CONTEXT:

---

## 2026-04-23 — Module Implementation & Validation Complete

**Modules Successfully Implemented & Validated:**
- M10 Ambulance: Vehicle management, dispatch scheduling, dispatch log
- M12 Birth/Death: Birth record creation/listing, death record creation/listing
- M15 TPA: Provider list, claim management, new claim creation
- M17 Messaging: Inbox, compose, broadcast messaging
- M18 Inventory: Item list, transaction history, add transaction
- M19 Download Center: File listing, upload interface
- M22 Live Consultation: Session list, schedule consultation

**Build & Quality Status:**
- Clean compile: zero errors, pre-existing nullable reference warnings only
- All 12 decimal properties across new modules configured with HasPrecision(18,2)
- SavedReport.ExecutionTimeMs precision mapping added
- Duplicate CMS model (Models/CMS_fixed.cs) excluded to resolve type conflicts

**Authenticated Runtime Validation:**
- Fresh build completed with clean database initialization
- 20/20 module routes passed smoke test (HTTP 200, no errors, no redirects)
- All module list/index pages loaded successfully with existing SQL Server database
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

---

*Document generated from PHP source (`php-original/`) and ASP.NET source (`MedyxHMS-ASPNET/`) analysis.*
*Last updated: 2026-04-23*
