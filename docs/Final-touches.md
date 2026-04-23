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

---

## Portals (P)

| ID | Portal Name          | URL Prefix         | Intended Users                         |
|----|----------------------|--------------------|----------------------------------------|
| P1 | Staff / Admin Portal | `/`                | All staff roles (RL1–RL7, RL9–RL11)   |
| P2 | Patient Portal       | `/PatientPortal/`  | Patients (RL8)                         |
| P3 | Public Website (CMS) | `/Site/`           | General public (unauthenticated)        |

---

## PHP Sidebar Menu Items (M)

Sourced from the original PHP AdminLTE-based admin panel sidebar. Items are module-gated where noted.

| ID  | Menu Item                   | Sub-items                                                                 | Conditional |
|-----|-----------------------------|---------------------------------------------------------------------------|-------------|
| M1  | Dashboard                   | —                                                                         | No          |
| M2  | Billing                     | —                                                                         | Module      |
| M3  | Appointment                 | —                                                                         | Module      |
| M4  | OPD (Out Patient)           | —                                                                         | No          |
| M5  | IPD (In Patient)            | —                                                                         | No          |
| M6  | Pharmacy                    | —                                                                         | Module      |
| M7  | Pathology                   | —                                                                         | Module      |
| M8  | Radiology                   | —                                                                         | Module      |
| M9  | Blood Bank                  | —                                                                         | Module      |
| M10 | Ambulance / Transport       | —                                                                         | Module      |
| M11 | Front Office                | —                                                                         | No          |
| M12 | Birth / Death Record        | Birth Record, Death Record                                                | No          |
| M13 | Human Resource (HR)         | —                                                                         | No          |
| M14 | Referral                    | —                                                                         | Module      |
| M15 | TPA Management              | —                                                                         | Module      |
| M16 | Finance                     | Income, Expenses                                                          | No          |
| M17 | Messaging                   | —                                                                         | No          |
| M18 | Inventory                   | —                                                                         | Module      |
| M19 | Download Center             | —                                                                         | No          |
| M20 | Certificate                 | Certificate, Patient ID Card, Staff ID Card                               | No          |
| M21 | Front CMS                   | —                                                                         | Module      |
| M22 | Live Consultation           | Live Consultation, Live Meeting                                            | Module      |
| M23 | Reports                     | *(See Reports section below — R1–R40)*                                    | No          |
| M24 | Setup / Settings            | Settings, Patient, Hospital Charges, Bed, Print Header/Footer, Front Office Setup, Operations, Pharmacy Setup, Pathology Setup, Radiology Setup, Blood Bank Setup, Symptoms, Findings, Zoom Settings, Finance Setup, HR (Leave Types), Referral Commission, Online Appointment, Inventory, Custom Fields | No |

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

---

*Document generated from PHP source (`php-original/`) and ASP.NET source (`MedyxHMS-ASPNET/`) analysis.*
*Last updated: 2026-04-23*
