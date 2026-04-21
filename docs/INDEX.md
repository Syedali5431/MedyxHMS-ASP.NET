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

### PH12 - Phase 7.2 Core Chat Experience and Knowledge Grounding
- Document: [PH12.md](PH12.md)
- Focus:
  - grounded chatbot retrieval from approved CMS/support/workflow sources
  - source attribution and confidence-scored response shaping
  - asynchronous chat UX (typing/retry/error handling) and feedback capture
  - role-safe patient-context boundaries and ownership checks
- **Status:** Phase 7.2 implemented and validated
- Read this for the complete implementation snapshot of Step 7.2 before Step 7.3 operational controls.

### PH13 - Phase 7.3 Operational Features and Integrations
- Document: [PH13.md](PH13.md)
- Focus:
  - chatbot operational guidance for appointment and billing flows
  - escalation handoff workflow and unresolved conversation handling
  - multilingual strategy controls and language-aware response path
  - admin settings, escalation queue, event logging, and analytics snapshots
- **Status:** Phase 7.3 implemented and validated
- Read this for the completion snapshot of Step 7.3 before Step 7.4 hardening.

### PH14 - Authentication and Dashboard Routing Audit
- Document: [PH14.md](PH14.md)
- Focus:
  - end-to-end module reachability and authorization audit
  - login and post-authentication redirect path verification
  - role-aware dashboard redirection hardening in main account flow
  - patient portal and staff portal routing separation checks
  - focused integration smoke tests for role routing, precedence, and return URL safety
- **Status:** Implemented and validated (role-based redirect logic enforced)
- Read this for the latest authentication and dashboard routing assurance snapshot.

### PH15 - Dynamic Role Selection Login & Module Management System
- Document: [PH15.md](PH15.md)
- Focus:
  - Two-step AJAX login UX: credentials first, then only the user's assigned roles are shown
  - Multi-role user support with explicit role selection stored in session
  - `SystemModule` and `UserModuleAccess` models + DB table creation and 23-module seed
  - `IModuleService` / `ModuleService` with global-disabled enforcement rule
  - `ModuleManagementController` with SuperAdmin global toggle and Admin+SuperAdmin per-user toggle
  - Three new views: global module list, searchable user list, per-user module access grid
- **Status:** Implemented and validated (build clean, 0 errors)
- Read this for the dynamic login role selection and module visibility control implementation.

### PH16 - Role Enforcement Hardening and UAT Automation
- Document: [PH16.md](PH16.md)
- Focus:
  - role-permission seed alignment for Accountant and Patient
  - view-layer permission hardening for staff-only actions
  - Admin and SuperAdmin-only report editing guardrails
  - automated UAT smoke scripts for build, tests, license generation, and HTTP reachability
- **Status:** Implemented for test readiness and operational validation
- Read this for the final role-security and smoke-test automation pass.

### PH17 - Public Site Content Studio and SuperAdmin Governance
- Document: [PH17.md](PH17.md)
- Focus:
  - Admin/SuperAdmin management for Home, Contact, Location page content and presentation
  - CMS page image and typography customization controls
  - automatic publishing of new CMS pages into public site navigation
  - SuperAdmin-only module/dashboard section enablement controls
- **Status:** Implemented for public content operations and governance hardening
- Read this for the current public-site content management and dashboard section authority model.

### PH18 - Public Style Studio Extension
- Document: [PH18.md](PH18.md)
- Focus:
  - public-site visual controls for colors, heading style, and button shape
  - layout-level style variable application with safe fallbacks
  - continuity of Admin/SuperAdmin public editing with SuperAdmin-only dashboard governance
- **Status:** Implemented as an extension of public-site content controls
- Read this for style customization coverage in the public website layer.

### PH19 - Public Style Studio Live Preview
- Document: [PH19.md](PH19.md)
- Focus:
  - live, in-form visual preview for public-site style settings
  - immediate feedback for color, heading style, and button shape decisions
  - no impact on dashboard styling scope or role boundaries
- **Status:** Implemented as UX enhancement for Admin/SuperAdmin content management
- Read this for authoring-time preview behavior in the public style flow.

### PH20 - Dual Desktop and Mobile Style Preview
- Document: [PH20.md](PH20.md)
- Focus:
  - simultaneous desktop and mobile live preview in style studio
  - pre-save visual validation for responsive-facing choices
  - preservation of existing role and persistence boundaries
- **Status:** Implemented as responsive preview enhancement
- Read this for dual-preview behavior in Public Site Settings.

### PH21 - Theme Preset Selector for Style Studio
- Document: [PH21.md](PH21.md)
- Focus:
  - one-click preset application for public-site visual settings
  - automatic fallback to Custom when manual edits diverge from preset values
  - persisted preset selection for consistent authoring continuity
- **Status:** Implemented as productivity enhancement for Admin/SuperAdmin styling workflow
- Read this for preset application behavior and persistence details.

### PH22 - Visual Preset Swatch Cards
- Document: [PH22.md](PH22.md)
- Focus:
  - clickable visual preset cards for faster selection
  - active-state feedback and dropdown synchronization
  - seamless custom override behavior after manual edits
- **Status:** Implemented as UX acceleration for style preset application
- Read this for visual preset card behavior and interaction rules.

### PH23 - Password Governance and Accounts Approval
- Document: [PH23.md](PH23.md)
- Focus:
  - Admin/SuperAdmin account approval queue for new signups
  - inactive-until-approved activation flow and pending-state messaging
  - role-governed password reset authority (SuperAdmin-protected target rule)
- **Status:** Implemented for account lifecycle governance and credential control
- Read this for approval pipeline and password authority model details.

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
12. [PH12.md](PH12.md)
13. [PH13.md](PH13.md)
14. [PH14.md](PH14.md)
15. [PH15.md](PH15.md)
16. [PH16.md](PH16.md)
17. [PH17.md](PH17.md)
18. [PH18.md](PH18.md)
19. [PH19.md](PH19.md)
20. [PH20.md](PH20.md)
21. [PH21.md](PH21.md)
22. [PH22.md](PH22.md)
23. [PH23.md](PH23.md)

## Module-to-Phase Cross Reference
- Build and compile recovery: [PH1.md](PH1.md), [PH2.md](PH2.md)
- EF models, DbContext, relationships, seeding: [PH3.md](PH3.md), [PH5.md](PH5.md)
- Routing and authorization pipeline: [PH4.md](PH4.md), [PH14.md](PH14.md)
- Runtime smoke validation: [PH3.md](PH3.md), [PH4.md](PH4.md), [PH5.md](PH5.md), [PH14.md](PH14.md)
- Phase 3 module completion and billing integrations: [PH6.md](PH6.md)
- CMS, public booking, export/download coverage: [PH8.md](PH8.md)
- Integration, go-live readiness, and deployment/support planning: [PH9.md](PH9.md)
- Licensing domain, renewal workflow, and expiry enforcement: [PH10.md](PH10.md)
- Chatbot foundation, moderation baseline, and SMTP health checks: [PH11.md](PH11.md)
- Chatbot grounded UX, source attribution, and feedback workflow: [PH12.md](PH12.md)
- Chatbot operational controls, escalation, and analytics: [PH13.md](PH13.md)
- Authentication redirect assurance and dashboard routing: [PH14.md](PH14.md)
- Dynamic role selection login and module management system: [PH15.md](PH15.md)
- Role enforcement hardening and smoke automation: [PH16.md](PH16.md)
- Public site content studio and dashboard governance hardening: [PH17.md](PH17.md)
- Public site style studio controls and theming: [PH18.md](PH18.md)
- Public style studio live preview UX: [PH19.md](PH19.md)
- Dual desktop/mobile live preview support: [PH20.md](PH20.md)
- Theme preset selector and custom override behavior: [PH21.md](PH21.md)
- Visual preset swatch card interactions: [PH22.md](PH22.md)
- Password governance and account approval workflow: [PH23.md](PH23.md)

## Planned Design Docs

## Operational Docs

### Deployment and Cutover
- Document: [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md)
- Checklist: [MIGRATION-COUNT-COMPARISON-CHECKLIST.md](MIGRATION-COUNT-COMPARISON-CHECKLIST.md)
- Scripts Guide: [../scripts/README.md](../scripts/README.md)
- Deployment Checklist: [../scripts/DEPLOYMENT_CHECKLIST.md](../scripts/DEPLOYMENT_CHECKLIST.md)
- Validation Guide: [../scripts/VALIDATION_GUIDE.md](../scripts/VALIDATION_GUIDE.md)
- Validation Script: [../scripts/Validate-DatabaseDeployment.ps1](../scripts/Validate-DatabaseDeployment.ps1)
- Bootstrap (seeded): [../scripts/New-Database.sql](../scripts/New-Database.sql)
- Bootstrap (empty): [../scripts/New-Database-Empty.sql](../scripts/New-Database-Empty.sql)
- Script: [../scripts/compare-migration-counts.ps1](../scripts/compare-migration-counts.ps1)
- Template: [../scripts/source-count-snapshot.template.csv](../scripts/source-count-snapshot.template.csv)

### Security and Performance Validation
- Document: [SECURITY-PERFORMANCE-VALIDATION.md](SECURITY-PERFORMANCE-VALIDATION.md)

### UAT Automation
- Script Folder: [../scripts/README.md](../scripts/README.md)
- Smoke Runner: [../scripts/Invoke-UatSmoke.ps1](../scripts/Invoke-UatSmoke.ps1)
- License Automation: [../scripts/Invoke-LicenseToolAutomation.ps1](../scripts/Invoke-LicenseToolAutomation.ps1)

### User Guidance
- Document: [USER-GUIDE.md](USER-GUIDE.md)
- Role Guides Folder: [user guides/README.md](user%20guides/README.md)
- Test Checklist: [user guides/TEST-READINESS-CHECKLIST.md](user%20guides/TEST-READINESS-CHECKLIST.md)

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
