# Product Requirements Document (PRD)
## Medyx Hospital Management System — ASP.NET Core Edition

**Version:** 1.0  
**Date:** 2026-04-24  
**Status:** Live / Post-Stabilization  
**Platform:** ASP.NET Core 8 MVC + SQL Server  

---

## 1. Product Overview

### 1.1 Product Summary
Medyx HMS is a full-featured, multi-role Hospital Management System built on ASP.NET Core 8. It replaces a legacy PHP/CodeIgniter application and provides a modern, secure, and extensible platform for managing hospital operations — including patient care, clinical workflows, billing, HR, licensing, and AI-assisted support.

### 1.2 Goals
- Provide a single unified platform for all hospital operational workflows.
- Enforce strict role-based access control (RBAC) across all modules and portals.
- Deliver a secure, GDPR-aware system with full audit trails.
- Support self-service patients via a dedicated Patient Portal.
- Enable AI-assisted guidance through an integrated chatbot with full governance and safety controls.
- Support licensing, multi-role governance, and subscription lifecycle management.

### 1.3 Target Users

| User Group      | Role(s)              | Portal / Area                 |
|-----------------|----------------------|-------------------------------|
| System Owner    | SuperAdmin           | Staff Portal — all modules    |
| Hospital Admin  | Admin                | Staff Portal — operational    |
| Clinical Staff  | Doctor, Nurse        | Staff Portal — OPD, IPD, OT   |
| Diagnostics     | LabTechnician, Radiologist | Staff Portal — Lab, Radiology |
| Pharmacy        | Pharmacist           | Staff Portal — Prescriptions  |
| Finance         | Accountant           | Staff Portal — Billing        |
| Reception       | Receptionist         | Staff Portal — Front Office   |
| Patient         | Patient              | Patient Portal                |
| General Public  | Unauthenticated      | Public CMS Site               |

---

## 2. System Architecture

### 2.1 Technology Stack
| Layer            | Technology                              |
|------------------|-----------------------------------------|
| Framework        | ASP.NET Core 8 MVC                      |
| ORM              | Entity Framework Core (SQL Server)      |
| Database         | SQL Server (LocalDB dev / full prod)    |
| Identity         | ASP.NET Core Identity (custom user)     |
| Background Jobs  | Quartz.NET                              |
| PDF Generation   | QuestPDF                                |
| AI / Chatbot     | OpenAI API (backend-only, server-side)  |
| Logging          | Serilog (file + console)                |
| Testing          | xUnit, integration test project         |
| Email            | SMTP (configurable, with retry)         |
| SMS              | Twilio + Africa's Talking (pluggable)   |

### 2.2 Portals
| Portal              | URL Prefix        | Description                              |
|---------------------|-------------------|------------------------------------------|
| Staff / Admin Portal | `/`              | Main operational portal for all staff    |
| Patient Portal      | `/PatientPortal/` | Self-service portal for patients         |
| Public CMS Site     | `/Site/`          | Public-facing website managed via CMS    |

### 2.3 Architecture Principles
- **Layered architecture**: Controllers → Services → Data (EF Core DbContext)
- **RBAC enforcement**: Every protected route uses `[Authorize(Roles = "...")]`; no client-side authorization
- **Explicit routing**: No ambiguous `RedirectToAction` across controller namespaces — all cross-namespace redirects use `LocalRedirect("/explicit-path")`
- **Audit-first**: Every sensitive operation (login, billing, patient data, license changes) writes to an audit trail
- **Security headers**: CSRF tokens, rate limiting, security response headers, CORS policy tightening
- **Configuration-driven**: All secrets, API keys, SMTP/SMS credentials stored in `appsettings.json` / environment config; never hardcoded

---

## 3. Modules & Features

### 3.1 Authentication & Identity (MD1 Foundation)
- ASP.NET Core Identity with custom `ApplicationUser`
- Two-step login: credentials → role select → session-bound `ActiveRole`
- PHP bcrypt password migration support (lazy rehash on first login)
- Unique `user_name` policy enforced at signup, patient registration, and admin-created users
- Numeric user ID assignment policy for newly created users
- Role-based redirect on login (SuperAdmin/Admin → `/Dashboard`, Patient → `/PatientPortal/Dashboard`, etc.)
- Concurrent session management service
- Password reset with role-based governance (Admin cannot reset SuperAdmin passwords)
- Account approval workflow for new staff registrations (Admin/SuperAdmin only)

### 3.2 Dashboard (MD1)
**Controller:** `DashboardController`  
- SuperAdmin/Admin dashboard with system-wide KPIs
- Module enablement governance panel (SuperAdmin-only section toggles)
- Quick-access cards for active modules
- Recent activity summary

### 3.3 Patient Management (MD2)
**Controller:** `PatientController`  
- Patient registration with full demographics, contact, and insurance details
- Unique patient ID generation
- Search, filter, paginate patient records
- Patient profile view with linked appointments, bills, prescriptions, and lab/radiology results
- Custom fields support

### 3.4 Appointments (MD3)
**Controller:** `AppointmentController`  
- Create, edit, reschedule, and cancel appointments
- Doctor-patient-time slot management
- Approval/rejection workflow with notifications
- Public online appointment booking via `/Site/` with honeypot + captcha spam protection
- Duplicate booking prevention (same phone, doctor, date, time)
- Admin duplicate review tools with notes capture
- Confirmation notifications (email + SMS) on approval
- Notification delivery audit logging

### 3.5 Outpatient Department — OPD (MD4)
**Controller:** `OPDController`  
- OPD encounter creation and check-in
- Consultation notes: symptoms, diagnosis, treatment plan, prescriptions, notes
- OPD-to-billing linkage (auto consultation bill generation)
- OPD encounter list/details/edit views

### 3.6 Inpatient Department — IPD (MD5)
**Controller:** `IPDController`  
- Patient admission management with ward/bed assignment
- Daily charge accumulation
- Discharge process with auto bill creation/update
- IPD encounter history and reporting

### 3.7 Billing & Finance (MD6)
**Controller:** `BillingController`  
- Create bills linked to appointments, OPD, IPD, lab, radiology, pharmacy, OT, and blood bank
- Multiple payment methods: Cash, Card, Cheque, Online, Insurance
- Payment reconciliation with bill status tracking
- Invoice, receipt, and bill PDF generation (QuestPDF)
- Financial reports: revenue by department, daily transactions, all transactions
- TPA (Third Party Administration) integration
- Export: CSV and PDF for billing lists

### 3.8 Pharmacy & Prescriptions (MD7)
**Controller:** `PrescriptionController`  
- Medicine/drug catalogue and stock management
- Prescription creation by doctors; dispensing by pharmacists
- Pharmacy billing integration
- Low-stock and expiry notifications
- Pharmacy balance and expiry medicine reports

### 3.9 Laboratory / Pathology (MD8)
**Controller:** `LabController`  
- Pathology order placement from OPD/IPD
- Test result entry workflow
- Result viewing in Patient Portal
- Pathology patient reports; PDF download

### 3.10 Radiology (MD9)
**Controller:** `RadiologyController`  
- Radiology order placement
- Image/result management workflow
- Result viewing in Patient Portal
- Radiology patient reports; PDF download

### 3.11 Blood Bank (MD10)
**Controller:** `BloodBankController`  
- Blood inventory management
- Blood issue workflow with donor tracking
- Component issue tracking
- Blood bank billing
- Blood issue, component issue, and donor reports

### 3.12 Operation Theatre (MD11)
**Controller:** `OperationTheatreController`  
- OT scheduling and booking
- OT-to-billing linkage
- OT utilization reports

### 3.13 Front Office (MD12)
**Controller:** `FrontOfficeController`  
- Visitor registration and tracking
- Complaints management
- Dispatch/receive tracking
- Birth and death record management
- Ambulance/transport management

### 3.14 Attendance (MD13)
**Controller:** `AttendanceController`  
- Staff check-in and check-out
- Attendance history and reporting

### 3.15 Leave Management (MD14)
**Controller:** `LeaveController`  
- Leave type configuration
- Leave request and approval workflow
- Leave balance tracking

### 3.16 Payroll (MD15)
**Controller:** `PayrollController`  
- Salary structure and calculation
- Payroll processing and payslip generation
- Payroll reports (monthly and summary)

### 3.17 Certificates & ID Cards (MD16)
**Controller:** `CertificateController`  
- Certificate generation (configurable types)
- Patient ID card generation (auto-numbered)
- Staff ID card generation

### 3.18 Referrals (MD17)
**Controller:** `ReferralController`  
- Internal and external referral workflow
- Referral commission tracking
- TPA referral integration
- Referral reports

### 3.19 Reports (MD18)
**Controller:** `ReportController`  
Built-in report types:
- Department Report (analytics)
- Financial Report (revenue by department)
- Occupancy Report (bed/ward utilization)
- Staff Report (attendance analytics)
- Payroll Report
- Report Builder with customizable templates (create, design, clone, delete)
- Report Scheduler (automated report generation)
- Generated Reports Archive
- Legacy PHP Report Import
- Report Preview

Also see [Final-touches.md](Final-touches.md) for full list of R1–R49 report types.

### 3.20 Patient Portal (MD19)
**Controllers:** `PatientPortal/*`  
- Dedicated portal at `/PatientPortal/` — restricted to `Patient` role only
- Patient dashboard with stats and quick actions
- Appointment viewing, booking, reschedule, and cancel
- Medical records viewing (OPD, IPD, Lab, Radiology)
- Bill viewing and payment interface
- Prescription viewing and PDF download
- Profile and settings management
- Export: Bills list (CSV/PDF), medical records PDF

### 3.21 CMS / Public Website (MD22)
**Controllers:** `CmsController`, `PublicSiteAdminController`, `SiteController`  
- Page management (CmsPage CRUD)
- Menu/navigation management (CmsMenuItem model)
- News/notice display (CmsNotice model)
- Online appointment booking form (public, with anti-spam)
- Admin content editing with style controls and responsive live preview
- Notification template management (email subject/body + SMS body)
- Notification delivery audit log with filter and pagination
- SMTP health check in notification settings
- SMS test send action and last-test status panel

### 3.22 Notifications (MD24)
**Controller:** `NotificationsController`  
- Email notifications via configurable SMTP provider
- SMTP retry strategy (configurable retry count + backoff)
- SMS notifications via Twilio and Africa's Talking (pluggable adapter)
- Runtime provider routing based on configuration
- Recipient-level email and SMS opt-out enforcement
- CMS-managed opt-out lists
- Appointment confirmation email/SMS templates with token placeholders
- Delivery audit logging per provider

### 3.23 Audit Trail (MD25)
**Controller:** `AuditController`  
- All sensitive operations recorded (login, billing, patient data, license, chatbot)
- Audit log viewer with date/entity/user filtering and search
- User action log viewer with status badges

### 3.24 Staff & User Management (MD26)
**Controller:** `StaffController`, `AccountsApprovalController`  
- Staff registration and profile management
- Role assignment and permission management
- Accounts approval workflow (Admin/SuperAdmin approve/reject with mandatory reject reason)
- Audit trail for all approval actions

### 3.25 Module Management (MD27)
**Controller:** `ModuleManagementController`  
- SuperAdmin-only global module enable/disable
- Per-user module access configuration
- Role-based access override (effective status = global + user override)

### 3.26 Licensing (MD23)
**Controller:** `LicenseController`  
- SuperAdmin-only license management screen
- Displays expiry date, status, renewal history, and reminder status
- Renewal actions limited to 1-year, 2-year, or 3-year extensions only
- License expiry enforcement (blocks staff/admin modules; bypasses SuperAdmin and Patient users)
- Expired-license information screen for restricted users
- Pre-expiry email reminders (5 days before expiry)
- Background job: daily license expiry evaluation (Quartz.NET)
- Configurable reminder template; reminder duplication prevention
- Full audit trail for all renewal and reminder events

### 3.27 AI Chatbot (MD21)
**Controllers:** `ChatbotController`, `ChatbotAdminController`  
See full feature list in [Final-touches.md — CB1–CB12](Final-touches.md).

**End-user features:**
- Natural-language query interface for patients and staff
- Role-aware responses (patient/staff/admin/SuperAdmin contexts)
- Guided appointment, billing, and navigation help flows
- Consent gating with explicit accept/reject before first interaction
- Feedback submission (per response)
- Support escalation workflow with admin-managed escalation queue
- Async chat UI with typing state, retry, and error handling
- Session-bound conversation history

**Safety & governance:**
- All OpenAI API calls routed through backend-only services (no client-side keys)
- PHI/PII redaction before transmission to OpenAI
- Rate limiting, abuse controls, and output moderation
- Prompt injection blocking
- Transcript retention policy and automated cleanup job
- Feature-flag-based phased enablement

**Admin controls:**
- Enable/disable chatbot by role group
- Model, temperature, system prompt, and token/usage-limit configuration
- Chatbot analytics dashboard (usage, satisfaction, escalation rates)
- Escalation management queue with resolve action

### 3.28 Mobile API Compatibility (MD20 / AppController)
**Controller:** `AppController`  
- Backward-compatible endpoint: `POST /App/Index` (legacy)
- RESTful API: `POST /api/v1/app`
- Configuration API: `GET/POST /api/v2/app/config`

---

## 4. Data Export & Document Generation

| Export Type      | Formats     | Where Available                               |
|------------------|-------------|-----------------------------------------------|
| Patient List     | CSV, PDF    | Patient Management                            |
| Appointment List | CSV, PDF    | Appointments                                  |
| Billing List     | CSV, PDF    | Billing module                                |
| CMS Delivery Log | CSV, PDF    | CMS Notification settings                     |
| CMS Pages        | CSV, PDF    | CMS admin                                     |
| CMS Notices      | CSV, PDF    | CMS admin                                     |
| CMS Menus        | CSV, PDF    | CMS admin                                     |
| Appointment Requests | CSV, PDF | CMS public booking admin                     |
| Duplicate Review | CSV, PDF    | CMS duplicate booking admin                   |
| Portal Bills     | CSV, PDF    | Patient Portal                                |
| Portal Dashboard | CSV, PDF    | Patient Portal                                |
| Billing Receipt  | PDF only    | Billing (receipt download)                    |
| Medical Records  | PDF only    | Patient Portal                                |
| Prescription     | PDF only    | Patient Portal                                |
| Pathology Report | PDF only    | Patient Portal / Lab                          |
| Radiology Report | PDF only    | Patient Portal / Radiology                    |

---

## 5. Security Requirements

| Requirement                          | Implementation                                       |
|--------------------------------------|------------------------------------------------------|
| Authentication                       | ASP.NET Core Identity + session-bound ActiveRole     |
| Authorization                        | `[Authorize(Roles = "...")]` on all protected routes |
| CSRF Protection                      | Anti-forgery tokens on all forms                     |
| SQL Injection Prevention             | EF Core parameterized queries                        |
| Password Security                    | ASP.NET Identity bcrypt + legacy PHP bcrypt migration|
| Secret Management                    | appsettings.json / environment variables only        |
| Rate Limiting                        | Sensitive endpoints + chatbot endpoints              |
| Audit Trail                          | All sensitive operations logged                      |
| PHI/PII Protection (AI)              | Redaction before OpenAI transmission                 |
| Unique Username Policy               | Enforced at all user creation paths                  |
| Role Boundary Enforcement            | Server-side only; privileged ops checked per request |
| License Bypass for SuperAdmin        | Enforced at middleware level                         |
| Security Headers                     | CORS tightening, response headers                    |
| Input Validation                     | Data annotations + service-layer validation          |

---

## 6. Non-Functional Requirements

| Category         | Requirement                                                           |
|------------------|-----------------------------------------------------------------------|
| Availability     | Target 99.5% uptime; health check endpoint exposed                    |
| Performance      | Dashboard page < 2s; report generation < 5s for standard reports      |
| Scalability      | Stateless MVC design; session via distributed cache when scaled        |
| Maintainability  | Serilog structured logging; layered architecture; no hardcoded secrets |
| Testability      | xUnit unit + integration tests; smoke check CI pipeline               |
| Localization     | Top 3–5 languages for Phase 1; additional languages incremental        |
| Browser Support  | Modern evergreen browsers (Chrome, Edge, Firefox, Safari)             |
| Mobile           | Bootstrap 5 responsive layout; mobile API compatibility layer          |

---

## 7. Deployment Requirements

- **Build gate**: Project must compile with zero errors before deployment
- **Smoke tests**: Automated public-route and protected-redirect validation must pass
- **Database**: SQL Server with schema applied via `New-Database.sql` / `New-Database-Empty.sql`; identity constraints required
- **Migrations**: EF Core migrations for schema changes; repeatable SQL validation script for data integrity
- **CI/CD**: GitHub Actions pipeline — restore → build → test on push and PRs to `main`; coverage collection and artifact publishing
- **Secrets**: All credentials in environment config; never committed to source control
- **Rollback plan**: Documented in `DEPLOYMENT-RUNBOOK.md`

---

## 8. Program Stage Tracking (Final-touches Alignment)

As of 2026-04-24, staged execution status is:

| Stage | Name | Status | Evidence |
|-------|------|--------|----------|
| Stage 1 | Role-Based Business UAT | Completed | `temp_build_output/uat-role-stage1-2026-04-24.json` |
| Stage 2 | Report Output Certification | Completed | `temp_build_output/stage2-report-cert-2026-04-24.json` |
| Stage 3 | Admin/SuperAdmin Governance E2E | Completed | `temp_build_output/stage3-governance-e2e-2026-04-24.json` |
| Stage 4 | Cutover Rehearsal and Rollback Drill | Completed | `temp_build_output/stage4-cutover-rehearsal-2026-04-24.json` |
| Stage 5 | Notification Production Readiness | Completed | `temp_build_output/stage5-notification-readiness-2026-04-24.json` |
| Stage 6 | Deferred Enhancements Backlog | Completed | `temp_build_output/stage6-deferred-enhancements-backlog-2026-04-24.json` |
| Stage 7 | System Management | Planned in Phases | See `Final-touches.md` Stage 7 phased plan |
| Stage 8 | Certificates | Planned in Phases | See `Final-touches.md` Stage 8 phased plan |

Execution sequencing and phase-level details for Stage 7 and Stage 8 are governed by `Final-touches.md` and must be reflected in execution checklists before implementation starts.

---

## 9. Linked Documents

| Document                                           | Purpose                                                        |
|----------------------------------------------------|----------------------------------------------------------------|
| [Final-touches.md](Final-touches.md)              | Staged inventories: roles, portals, menus, modules, reports, AI |
| [Final-TODOList.md](Final-TODOList.md)            | Consolidated completed + pending phase task list               |
| [UPDATED-TODO-LIST-2026-04-22.md](UPDATED-TODO-LIST-2026-04-22.md) | Short-form active execution priorities |
| [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md)    | Step-by-step deployment and rollback procedures                |
| [ADMIN-GUIDE.md](ADMIN-GUIDE.md)                  | Admin user guide                                               |
| [USER-GUIDE.md](USER-GUIDE.md)                    | End-user documentation                                         |
| [TRAINING-SUPPORT-PLAN.md](TRAINING-SUPPORT-PLAN.md) | User training plan and support escalation                   |
| [OPENAI-CHATBOT-DESIGN.md](OPENAI-CHATBOT-DESIGN.md) | Chatbot architecture and safety design                      |
| [LICENSING-DESIGN.md](LICENSING-DESIGN.md)        | License domain design and enforcement rules                    |
| [SECURITY-PERFORMANCE-VALIDATION.md](SECURITY-PERFORMANCE-VALIDATION.md) | Security and performance validation checklists |
| [UAT-EXECUTION-EVIDENCE-2026-04-22.md](UAT-EXECUTION-EVIDENCE-2026-04-22.md) | UAT execution evidence (April 2026)        |
| [UAT-ROLE-VALIDATION-CHECKLIST-2026-04-22.md](UAT-ROLE-VALIDATION-CHECKLIST-2026-04-22.md) | Role-by-role UAT checklist |
| [NULLABILITY-WARNING-TRIAGE-PLAN-2026-04-22.md](NULLABILITY-WARNING-TRIAGE-PLAN-2026-04-22.md) | Warning triage plan |
| [IMPLEMENTATION-STATUS-2026-04-22-FINAL-RUN.md](IMPLEMENTATION-STATUS-2026-04-22-FINAL-RUN.md) | Final implementation status |

---

*PRD maintained by the Medyx HMS project team. Last updated: 2026-04-24.*
