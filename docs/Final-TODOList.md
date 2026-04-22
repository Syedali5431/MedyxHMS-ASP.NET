# Final TODO List — Medyx HMS Migration & Development

**Last Updated:** 2026-04-22  
**Overall Progress: ~99.7% Complete**  
**Source Reference:** PHP `Docs/TODO List.md` + ASP.NET implementation evidence  

---

## 📊 Progress Summary

| Phase | Name | Status |
|-------|------|--------|
| Phase 0 | Database Migration & Enterprise Enhancements | ✅ Complete |
| Phase 1 | Foundation & Setup | ✅ Complete |
| Phase 2 | Core Hospital Operations | ✅ Complete |
| Phase 3 | Clinical & Diagnostic Services | ✅ Complete |
| Phase 4 | Administrative & Support Functions | ✅ Complete |
| Phase 5 | Integration & Deployment | 🔄 Partially Complete |
| Phase 6 | Licensing & Commercial Control | ✅ Complete |
| Phase 7 | AI Chatbot & OpenAI Assistance | ✅ Complete |

---

## ✅ Phase 0: Database Migration & Enterprise Enhancements

### Database Conversion
- [x] MySQL to SQL Server conversion — 167 tables converted with proper SQL Server data types
- [x] Backtick → square bracket syntax conversion; T-SQL compatible
- [x] Static data removal — all INSERT statements removed (file size 4.97 MB → 0.166 MB, 96% reduction)
- [x] Clean schema ready for development and production deployment

### Enterprise-Grade Enhancements
- [x] RBAC system added: 10 roles, 43 features, role_features, staff_roles tables, granular permissions (view/add/edit/delete)
- [x] SuperAdmin user created with full access (superadmin@hospital.com / Employee ID: SUPER001)
- [x] Foreign keys, indexes, and constraints applied

---

## ✅ Phase 1: Foundation & Setup

### STEP 1.1 — Critical Pre-Development Decisions ✅

- [x] **Patient password migration strategy** — bcrypt lazy-hash approach; rehash on first login
- [x] **Payment gateways inventory** — Top 5 prioritized (PayPal, Stripe, Razorpay, SSLCommerz, Paytm); remaining 11 phased
- [x] **Phase 1 go-live scope** — All 8 core modules required; strict ordering: Patient → OPD/IPD → Billing → Pharmacy/Lab
- [x] **CMS/public website priority** — Non-critical; moved to Phase 2 post-go-live
- [x] **Mobile API backward compatibility** — Versioned API (/api/v1/, /api/v2/) with legacy fallback endpoint
- [x] **Zoom integration priority** — Moved to Phase 1.5/2; hardcoded API token revoked and re-secured
- [x] **Mandatory documents identification** — All 5 types identified (Invoices, Patient ID Cards, Staff ID Cards, Certificates, Receipts)
- [x] **Language requirements** — Top 3–5 languages for Phase 1; additional languages incremental
- [x] **Migration approach decision** — Phased: Phase 1 (Patient/OPD/IPD/Billing) → Phase 2 (Pharmacy/Lab/Payments) → Phase 3 (Staff/Admin) → Phase 4 (CMS/Advanced)
- [x] **Security credential remediation** — 4 exposed credentials revoked; environment-based configuration (.env) implemented

### STEP 1.2 — ASP.NET Core Foundation Setup ✅

- [x] ASP.NET Core MVC project created (.NET 8.0 LTS) with layered architecture (Data / Services / Controllers / Views)
- [x] EF Core DbContext configured with SQL Server; connection strings via appsettings.json
- [x] Comprehensive data models created (Patient, Appointment, Billing, RBAC, etc.)
- [x] Settings entity and feature-toggle subsystem built (module activation, branding, theme, language)
- [x] Serilog structured logging (file + console); audit trail service; error handling; health checks
- [x] Secure file storage service with upload validation (type/size); uploads directory structure
- [x] Quartz.NET background job framework configured; scheduled job infrastructure set up
- [x] Development/staging/production environments; CORS, session management, security headers; response caching; API behavior configuration

### STEP 1.3 — Authentication & Security ✅

- [x] ASP.NET Core Identity with custom ApplicationUser; staff login with PHP bcrypt migration
- [x] Login/logout/password reset flows with audit logging
- [x] RBAC: 10 roles, 28+ features; permission/claim-based authorization with AuthorizationService
- [x] Session management and secure cookie handling; concurrent session management
- [x] Input validation and sanitization; EF Core SQL injection protection
- [x] CSRF protection with anti-forgery tokens; rate limiting for sensitive endpoints
- [x] Security headers and CORS configuration
- [x] GDPR-compliant data handling; data retention policies; audit trails for sensitive operations

---

## ✅ Phase 2: Core Hospital Operations

### STEP 2.1 — Core Business Logic (Staff / Patient / Appointments) ✅

- [x] Staff role assignments with RBAC; admin dashboard with audit logging
- [x] Patient Management — full CRUD, DTOs, ViewModels, search, audit logging
- [x] Appointments Management — CRUD, approval/rejection workflow, reschedule, cancel, notifications
- [x] Patient Portal — patient login, dashboard, appointment booking/management, medical records, bills, prescriptions, profile/settings; responsive Bootstrap 5 UI

### STEP 2.2 — Billing & Financial Systems ✅

- [x] Bill, BillItem, Payment, Transaction entities; appointment-based billing
- [x] Invoice/receipt/bill generation DTOs and ViewModels; QuestPDF generation
- [x] Multiple payment methods (Cash, Card, Cheque, Online, Insurance)
- [x] Payment reconciliation with bill status updates
- [x] BillingController with Index, Details, Create, Pay actions; responsive views
- [x] Patient Portal billing integration (view and pay)

---

## ✅ Phase 3: Clinical & Diagnostic Services

### STEP 3.1 — Clinical Modules (OPD / IPD / Prescriptions) ✅

- [x] OPD — encounter entities/DTOs/ViewModels; check-in; consultation notes (symptoms, diagnosis, treatment, prescription, notes); OPD-to-billing linkage
- [x] IPD — admission entities; ward/bed assignment; daily charge accumulation; discharge process with auto bill creation/update
- [x] Prescriptions — prescription entity and full DTO/ViewModel/service/controller layer; creation workflow; patient portal viewing; pharmacy fulfillment foundation

### STEP 3.2 — Diagnostic Modules (Pathology / Radiology / Pharmacy) ✅

- [x] Pathology — order placement; test result entry; patient portal viewing; report PDFs
- [x] Radiology — order placement; image/result management; patient portal viewing; report PDFs
- [x] Pharmacy — medicine/stock entities; stock management; pharmacy billing foundation; low-stock/expiry notifications

### STEP 3.3 — Specialized Services (Blood Bank / OT / Referrals) ✅

- [x] Blood Bank — blood inventory; issue workflow; donor tracking; component issue tracking; billing; reports
- [x] Operation Theatre — OT schedule entities; OT booking; OT-to-billing
- [x] Referrals & TPA — referral entities; referral workflow; TPA integration

---

## ✅ Phase 4: Administrative & Support Functions

### STEP 4.1 — HR & Administrative Functions ✅

- [x] Staff Attendance — check-in/out; attendance history and reporting
- [x] Leave Management — leave types; request/approval workflow; leave balance tracking
- [x] Payroll — salary structure; payroll calculation and processing; reports
- [x] Front Office — visitor registration; complaints management; dispatch/receive tracking; birth/death records; ambulance management
- [x] Certificates & ID Cards — certificate generation; patient and staff ID card generation (auto-numbered)
- [x] Audit/Log Viewers — AuditController with Index/Details/UserActions; date/entity/user filtering; UserActionLog viewer with status badges
- [x] Additional Reports — department, financial (revenue by department), occupancy (bed/ward), staff distribution

### STEP 4.2 — Public Website, CMS & Data Export ✅

- [x] Public CMS/website — CmsPage CRUD; CmsMenuItem navigation system; CmsNotice display
- [x] Online appointment booking (public) — SiteController; honeypot anti-spam; captcha; DoctorShift model; PublicAppointmentRequest tracking; patient self-registration/linking from booking; duplicate booking prevention; admin duplicate review tools with notes; confirmation notification hook (email/SMS stub) on approval; Twilio SMS adapter; admin CMS notification settings UI; test SMS/email actions; last test status panel; notification test history clear; delivery audit logging; admin delivery log viewer with filter and pagination
- [x] Data export — QuestPDF + shared export subsystem; CSV/PDF for Patient, Appointment, Billing, CMS lists; PDF-only for receipts, medical records, prescriptions, lab/radiology reports; build blockers resolved; runtime validation completed
- [x] Admin public-site content editing with style controls and responsive live preview
- [x] SuperAdmin-only dashboard section/module enablement governance

---

## 🔄 Phase 5: Integration & Deployment *(Partially Complete)*

### STEP 5.1 — Integrations & Third-Party Services

- [x] SMS abstraction layer; Twilio SMS provider with configuration-based live/test toggle
- [x] Email abstraction layer; SMTP email provider; delivery audit logging
- [x] Configurable SMTP retry strategy (attempt count + backoff delay)
- [x] Appointment notification templates (email subject/body + SMS body with token placeholders)
- [x] Africa's Talking SMS provider support with runtime routing
- [x] Recipient-level email and SMS opt-out enforcement with CMS-managed lists
- [x] Mobile API compatibility layer — backward-compatible endpoints (`/App/Index`, `/api/v1/app`, `/api/v2/app/config`)
- [ ] **PENDING** — Full production credential onboarding for selected SMS provider and SMTP
- [ ] **PENDING** — Staging soak test for notification retries, opt-outs, and provider failover behavior

### STEP 5.2 — Testing, Deployment & Go-Live

- [x] xUnit test project baseline; unit tests for export service, PatientService, AppointmentService, BillingService
- [x] Integration test for public booking approval and appointment-to-billing linkage flow
- [x] Regression test suite for IPD, prescription/pharmacy, and reporting workflows
- [x] Security validation checklist/documentation; performance validation checklist/documentation
- [x] Data migration validation — repeatable SQL validation script; source count comparison; executed against `(localdb)\MSSQLLocalDB/MedyxHMS`; zero integrity issues; evidence artifacts in `docs/migration-evidence/`
- [x] CI pipeline — GitHub Actions restore/build/test on push/PR to `main`; coverage collection; artifact publishing
- [x] Deployment planning — database migration execution plan; cutover strategy; rollback plan documented
- [x] User training and documentation — user docs, admin docs, training plan
- [x] Production support plan — escalation procedures, incident response, post-launch monitoring, feedback collection
- [ ] **PENDING** — Execute full role-based business UAT with seeded production-like data and role credentials
- [ ] **PENDING** — Validate report outputs for Department, Occupancy, Staff, and Payroll reports
- [ ] **PENDING** — Validate Admin/SuperAdmin governance workflows end-to-end
- [ ] **PENDING** — Record pass/fail UAT evidence and defects in UAT evidence artifacts
- [ ] **PENDING** — Go-live cutover rehearsal and rollback validation before release

---

## ✅ Phase 6: Licensing & Commercial Control

### STEP 6.1 — License Domain & Security Design ✅

- [x] Single authoritative license entity with immutable audit trail
- [x] Server-side validation; client-side tampering prevention
- [x] Renewal rules: fixed durations (1, 2, 3 years only)
- [x] SuperAdmin and Patient users bypass license expiry enforcement
- [x] Expired-license messaging for restricted staff/admin users

### STEP 6.2 — License Administration & Renewal Workflow ✅

- [x] SuperAdmin-only license management screen — expiry date, status, renewal history, reminder status
- [x] Renewal action (1/2/3-year options) with confirmation and audit logging
- [x] Expiry enforcement middleware — redirect to expired-license info screen; SuperAdmin access always available
- [x] Unauthorized role access validation and invalid renewal period validation

### STEP 6.3 — Reminder Notifications & Background Automation ✅

- [x] Pre-expiry email reminder 5 days before expiry — all users; payment reminder wording + SuperAdmin contact instruction
- [x] Reminder send date/time tracking; duplicate reminder prevention
- [x] Configurable reminder template content
- [x] Quartz.NET background job: daily license expiry evaluation
- [x] Reminder dispatch logging (successes, failures, skipped conditions)
- [x] Admin visibility for next expiry and reminder history
- [x] Fallback manual resend action for SuperAdmin

### STEP 6.4 — Testing, Compliance & Operational Hardening ✅

- [x] Tests: SuperAdmin and Patient role exemptions; 5-day reminder trigger and non-duplication; allowed renewal durations (1/2/3 years only)
- [x] Audit log verification for renewal changes and reminder activity
- [x] Expired-license behavior validated across all staff/admin roles

---

## ✅ Phase 7: AI Chatbot & OpenAI Assistance

### STEP 7.1 — Chatbot Product Design & AI Safety Foundation ✅

- [x] OpenAI chat completions API selected; all API keys in server-side config only
- [x] All AI requests routed through backend services; no client-side OpenAI calls
- [x] Token, timeout, retry, and rate-limit policies defined
- [x] Healthcare-safe use cases defined (helpdesk, FAQ, billing/appointment guidance, patient support)
- [x] Unsafe medical diagnosis blocked; emergency disclaimer and escalation guidance implemented
- [x] Prompt safety, moderation, and abuse prevention rules defined

### STEP 7.2 — Core Chat Experience & Knowledge Scope ✅

- [x] Chatbot widget/page for staff and patient-facing usage
- [x] Async conversation UI with typing state, retry flow, and error handling
- [x] Role-aware response behavior (patient, staff, admin, SuperAdmin contexts)
- [x] Fallback response when bot cannot answer confidently
- [x] Retrieval-grounded context from approved CMS/support/workflow sources
- [x] Source attribution and confidence metadata in responses
- [x] Ownership-based feedback/history access; patient-scoped context boundaries

### STEP 7.3 — Operational Features & Integrations ✅

- [x] Guided appointment help flows; billing and payment guidance flows
- [x] Support escalation / contact handoff workflow
- [x] Multilingual support strategy defined
- [x] Admin settings: enable/disable chatbot by role group; model/temperature/system prompt/token/usage-limit configuration
- [x] Session, error, feedback, and moderation outcome logging
- [x] Analytics: question categories, unresolved conversations, handoff rates

### STEP 7.4 — Security, Privacy & Quality Assurance ✅

- [x] PHI/PII redacted before OpenAI transmission
- [x] Consent/disclosure language implemented; consent gating with accept/reject
- [x] Rate limiting, abuse controls, and output moderation applied
- [x] Transcript retention policy with automated cleanup job
- [x] Test coverage: prompt assembly, authorization boundaries, fallback behavior
- [x] Prompt injection red-team testing; sensitive data leakage validation
- [x] Chatbot answers validated against approved business/help content
- [x] Feature-flag-based phased enablement; configurable CORS tightening
- [x] Dedicated prompt injection tests; explicit usage audit actions

---

## 🔴 Remaining Pending Items

### High Priority (Pre Go-Live)

| # | Item | Area |
|---|------|------|
| 1 | Execute full role-based business UAT with seeded production-like data | Testing |
| 2 | Validate report outputs: Department, Occupancy, Staff, Payroll | Reports |
| 3 | Validate Admin/SuperAdmin governance workflows end-to-end | Auth |
| 4 | Record UAT pass/fail evidence and defects | Testing |
| 5 | Go-live cutover rehearsal and rollback validation | Deployment |

### Medium Priority (Deployment Readiness)

| # | Item | Area |
|---|------|------|
| 6 | Onboard production SMS provider credentials (Twilio or Africa's Talking) | Notifications |
| 7 | Onboard production SMTP credentials | Notifications |
| 8 | Staging soak test: notification retries, opt-outs, provider failover | Notifications |
| 9 | Close runtime-risk warning triage batches in controllers/services (nullability) | Code Quality |

### Low Priority (Post Go-Live)

| # | Item | Area |
|---|------|------|
| 10 | Zoom / Live Consultation integration (secure key management) | Integration |
| 11 | Additional payment gateways beyond top 5 | Payments |
| 12 | Expand language support beyond top 3–5 | Localization |
| 13 | Additional SMS providers beyond Twilio / Africa's Talking | Notifications |

---

## 📌 Recently Completed Enhancements (April 2026)

- [x] Admin/SuperAdmin public-site content editing with style controls and responsive live preview
- [x] SuperAdmin-only dashboard module enablement governance
- [x] Accounts Approval module with approve/reject, mandatory reject reason, and audit trail
- [x] Role-based password reset governance (Admin cannot reset SuperAdmin targets)
- [x] Mandatory unique `user_name` across all user creation paths
- [x] Numeric user ID assignment policy for new users
- [x] SQL bootstrap script alignment for required/unique username constraints (`New-Database.sql`, `New-Database-Empty.sql`)
- [x] Chatbot consent, PII redaction, retention cleanup, rate limiting, output moderation hardening
- [x] Dedicated prompt-injection tests; explicit chatbot usage audit; restored async chatbot UI
- [x] Configurable chatbot CORS policy tightening; Phase 7.4 documentation closure
- [x] Mobile API compatibility layer for legacy `/App/Index` + `/api/v1/app` + `/api/v2/app/config`
- [x] Appointment notification template management in CMS (email + SMS)
- [x] SMTP retry strategy (configurable retry count + backoff delay)
- [x] Recipient-level email and SMS opt-out enforcement with CMS-managed lists
- [x] Africa's Talking SMS provider support with runtime routing
- [x] Role-to-dashboard routing fix — SuperAdmin/Admin correctly land on `/Dashboard`, not `/PatientPortal/Dashboard`

---

*Based on: `Medyx-HMS-php/php-original/Docs/TODO List.md` and ASP.NET implementation evidence.*  
*Cross-reference: [PRD.md](PRD.md) | [Final-touches.md](Final-touches.md) | [UPDATED-CHECKLIST-2026-04-22.md](UPDATED-CHECKLIST-2026-04-22.md)*
