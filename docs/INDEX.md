# MedyxHMS Technical Documentation Index

## Purpose
This index provides a phase-by-phase navigation map for the remediation, stabilization, and hardening work completed in the ASP.NET application.

## Phase Map

### PH1 - Baseline Audit and Build Recovery Plan
- Document: [PH1.md](PH1.md)
- Focus:
  - Baseline build diagnostics
  - Error categorization strategy
  - Recovery sequencing approach
- Read this first to understand initial system state and triage method.

### PH2 - Compile Stabilization Across Layers
- Document: [PH2.md](PH2.md)
- Focus:
  - Controllers, Views, DTOs, ViewModels alignment
  - Startup/DI/extension wiring corrections
  - Compile blocker elimination
- Read this to understand how compile stability was restored.

### PH3 - Runtime Startup Blocker Resolution
- Document: [PH3.md](PH3.md)
- Focus:
  - EF model mismatch fixes
  - Seed data requirement fixes
  - Relationship mapping corrections for startup success
- Read this for database/model initialization troubleshooting patterns.

### PH4 - Route, Authorization, and Login Runtime Fixes
- Document: [PH4.md](PH4.md)
- Focus:
  - Route ambiguity resolution
  - Dynamic Permission:* authorization policy handling
  - Login flow model/view compatibility fixes
- Read this for auth pipeline and routing runtime behavior changes.

### PH5 - EF Warning Hardening and Startup Log Cleanup
- Document: [PH5.md](PH5.md)
- Focus:
  - Shadow FK warning elimination
  - Decimal precision mapping standardization
  - Development HTTPS warning cleanup
- Read this for maintainability hardening and warning reduction decisions.

### PH6 - Phase 3 Delivery Completion (Clinical/Diagnostic/Specialized)
- Document: [PH6.md](PH6.md)
- Focus:
  - Step 3.1 finalization (IPD billing automation, portal prescriptions)
  - Step 3.2 completion (patient portal pathology/radiology result visibility)
  - Step 3.3 implementation (Blood Bank, OT, Referral/TPA with billing)
- Read this for the complete implementation snapshot before Phase 4 work.

### PH7 - Phase 4 Step 4.1 Delivery (HR & Administrative Functions)
- Document: [PH7.md](PH7.md)
- Focus:
  - Staff Attendance Management (check-in/out, daily reporting)
  - Leave Management (leave types, requests, balance tracking, approvals)
  - Payroll Processing (salary calculation, payment status tracking)
  - Front Office Operations (visitor tracking, complaints, dispatch/receive)
  - Certificates & ID Card Generation (auto-numbered, staff tracking)
- **Status:** Step 4.1 complete and validated (report routes verified; basic audit viewer routes implemented)
- Read this for comprehensive HR workflow implementation patterns.

### PH8 - Phase 4 Step 4.2 Delivery (CMS, Public Booking, Exports)
- Document: [PH8.md](PH8.md)
- Focus:
  - Public CMS and public booking administrative workflow completion
  - Shared CSV/PDF export subsystem using QuestPDF
  - Cross-module dashboard export coverage and PDF-only report downloads
  - Build/runtime validation for export and protected-route behavior
- **Status:** Step 4.2 export/download layer completed and validated
- Read this for the implementation snapshot of CMS/public-booking observability and document export patterns.

### PH9 - Phase 5 Step 5.2 Readiness Delivery
- Document: [PH9.md](PH9.md)
- Focus:
  - regression suite expansion
  - go-live runbooks and operational documentation
  - security/performance validation planning
  - migrated-data validation blocker documentation
- **Status:** Internal Step 5.2 work completed; external migrated-data validation remains blocked by environment availability
- Read this for the latest integration/deployment readiness snapshot.

### PH10 - Phase 6 Licensing & Commercial Control Delivery
- Document: [PH10.md](PH10.md)
- Focus:
  - license domain entities and audit/reminder logs
  - SuperAdmin-only renewal workflow and admin screen
  - expiry enforcement middleware and expired-license messaging
  - daily reminder automation and manual resend controls
- **Status:** Phase 6 implemented and validated
- Read this for the complete licensing implementation snapshot before Phase 7 work.

### PH11 - Phase 7.1 Chatbot Foundation and SMTP Operational Check
- Document: [PH11.md](PH11.md)
- Focus:
  - backend-only OpenAI chatbot orchestration foundation
  - moderation and role-aware prompt safety baseline
  - chat session/message persistence and initial chat UI route
  - SMTP configuration/connectivity operational health check in CMS settings
- **Status:** Phase 7.1 implemented and validated
- Read this for the completed implementation snapshot of Step 7.1 before Step 7.2 work.

## Suggested Reading Order
1. [PH1.md](PH1.md)
2. [PH2.md](PH2.md)
3. [PH3.md](PH3.md)
4. [PH4.md](PH4.md)
5. [PH5.md](PH5.md)
6. [PH6.md](PH6.md)
7. [PH7.md](PH7.md)
8. [PH8.md](PH8.md)
9. [PH9.md](PH9.md)
10. [PH10.md](PH10.md)
11. [PH11.md](PH11.md)

## Module-to-Phase Cross Reference
- Build and compile recovery: [PH1.md](PH1.md), [PH2.md](PH2.md)
- EF models, DbContext, relationships, seeding: [PH3.md](PH3.md), [PH5.md](PH5.md)
- Routing and authorization pipeline: [PH4.md](PH4.md)
- Runtime smoke validation: [PH3.md](PH3.md), [PH4.md](PH4.md), [PH5.md](PH5.md)
- Phase 3 module completion and billing integrations: [PH6.md](PH6.md)
- CMS, public booking, export/download coverage: [PH8.md](PH8.md)
- Integration, go-live readiness, and deployment/support planning: [PH9.md](PH9.md)
- Licensing domain, renewal workflow, and expiry enforcement: [PH10.md](PH10.md)
- Chatbot foundation, moderation baseline, and SMTP health checks: [PH11.md](PH11.md)

## Planned Design Docs

## Operational Docs

### Deployment and Cutover
- Document: [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md)
- Checklist: [MIGRATION-COUNT-COMPARISON-CHECKLIST.md](MIGRATION-COUNT-COMPARISON-CHECKLIST.md)
- Script: `scripts/compare-migration-counts.ps1`
- Template: `scripts/source-count-snapshot.template.csv`

### Security and Performance Validation
- Document: [SECURITY-PERFORMANCE-VALIDATION.md](SECURITY-PERFORMANCE-VALIDATION.md)

### User Guidance
- Document: [USER-GUIDE.md](USER-GUIDE.md)

### Admin Guidance
- Document: [ADMIN-GUIDE.md](ADMIN-GUIDE.md)

### Training and Support
- Document: [TRAINING-SUPPORT-PLAN.md](TRAINING-SUPPORT-PLAN.md)

### Stage Handoff
- Document: [STAGE-5.2-HANDOFF.md](STAGE-5.2-HANDOFF.md)

### Licensing & Commercial Control
- Design: [LICENSING-DESIGN.md](LICENSING-DESIGN.md)
- Delivery: [PH10.md](PH10.md)
- Focus:
  - License data model and enforcement workflow
  - SuperAdmin-only renewal design
  - 5-day pre-expiry reminder automation
  - Access-control rules for expired licenses

### OpenAI Chatbot
- Document: [OPENAI-CHATBOT-DESIGN.md](OPENAI-CHATBOT-DESIGN.md)
- Focus:
  - Backend-only OpenAI integration architecture
  - Safety guardrails and privacy controls
  - Role-aware chatbot behavior and knowledge scope
  - Rollout, observability, and moderation design

## Maintenance Rule
When a new implementation phase is completed, create a new file in this folder (for example PH6.md), then append it to this index with:
- phase objective
- technical scope
- key changes
- validation summary
- dependencies/risks
