# Programmer Training Guide

## Medyx HMS ASP.NET Project Workflow and File Map

Last Updated: 2026-04-22

## 1. Purpose of This Guide

This guide helps new programmers understand how Medyx HMS is structured, how requests flow through the application, and what each major file contains.

Primary goals:
- Explain runtime workflow from startup to response.
- Show where each feature is implemented.
- Provide a practical learning order for onboarding.
- Give a file-by-file map of the main code areas.

## 2. High-Level System Workflow

## 2.1 Application Startup Flow

1. Application bootstraps in [Program.cs](../../Program.cs).
2. Dependency Injection registrations are added for:
- DbContext
- Identity
- Controllers + global filter
- Services/Interfaces
- Middleware
3. Database initialization runs at startup via [Services/Implementations/DatabaseInitializer.cs](../../Services/Implementations/DatabaseInitializer.cs).
4. HTTP pipeline is configured:
- Security middleware
- Static files
- Routing
- CORS
- Session
- Authentication
- License enforcement middleware
- Module entitlement middleware
- Authorization
5. Default route maps to Home/Index.

## 2.2 Request Processing Flow

For a typical web request:
1. Route resolves to a controller action in [Controllers](../../Controllers).
2. ASP.NET authorization runs ([Authorize] attributes, policies, roles).
3. Global filter [Services/Filters/LicenseExpiryFilter.cs](../../Services/Filters/LicenseExpiryFilter.cs) evaluates expiry behavior.
4. Middleware checks:
- [Extensions/LicenseEnforcementMiddleware.cs](../../Extensions/LicenseEnforcementMiddleware.cs)
- [Extensions/ModuleEntitlementMiddleware.cs](../../Extensions/ModuleEntitlementMiddleware.cs)
5. Controller action calls a service in [Services/Implementations](../../Services/Implementations).
6. Service queries/updates EF Core via [Data/ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs).
7. Controller returns a Razor View from [Views](../../Views) or returns JSON/File response.

## 2.3 Identity and Role Routing Workflow

Authentication and role selection are mainly in:
- [Controllers/AccountController.cs](../../Controllers/AccountController.cs)
- [Controllers/PatientPortal/AccountController.cs](../../Controllers/PatientPortal/AccountController.cs)

Important behavior:
- Staff/Admin roles use the main portal routes.
- Patient role uses Patient Portal routes.
- Cross-portal routing is explicitly controlled to prevent namespace/area confusion.

## 2.4 Licensing and Entitlement Workflow

Core licensing files:
- [Controllers/LicenseController.cs](../../Controllers/LicenseController.cs)
- [Services/Implementations/LicenseService.cs](../../Services/Implementations/LicenseService.cs)
- [Services/Implementations/LicenseFileService.cs](../../Services/Implementations/LicenseFileService.cs)
- [Extensions/LicenseEnforcementMiddleware.cs](../../Extensions/LicenseEnforcementMiddleware.cs)
- [Extensions/ModuleEntitlementMiddleware.cs](../../Extensions/ModuleEntitlementMiddleware.cs)

Enforcement model:
- Expired license restricts staff/admin operations.
- SuperAdmin and Patient are treated as exempt in key pathways.
- Module-level access requires both:
- module enabled for user
- module licensed in current license

## 2.5 Data Access Workflow

EF Core context is centralized in [Data/ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs).

This file contains:
- All DbSet declarations.
- Relationship and index configuration in OnModelCreating.
- Seed-related hooks and constraints.

## 3. Root-Level Key Files

- [Program.cs](../../Program.cs): Main startup, DI registrations, middleware pipeline, route mapping.
- [appsettings.json](../../appsettings.json): Base runtime config (connection strings, Serilog, notification, feature toggles, security, OpenAI).
- [appsettings.Development.json](../../appsettings.Development.json): Development overrides for local environment.
- [MedyxHMS.csproj](../../MedyxHMS.csproj): .NET target, package references, compile include/exclude behavior.
- [MedyxHMS.lic](../../MedyxHMS.lic): Current runtime license file consumed by licensing workflow.

## 4. Folder-by-Folder File Map

## 4.1 Data

- [Data/ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs): EF Core DbContext, full model graph, relations, indexes, and schema behavior.

## 4.2 Extensions (Security, Authorization, Middleware)

- [Extensions/AuthorizationExtensions.cs](../../Extensions/AuthorizationExtensions.cs): Permission-based authorization requirement/handler/policy provider and permission matrix helpers.
- [Extensions/HtmlHelperExtensions.cs](../../Extensions/HtmlHelperExtensions.cs): Razor/UI helper extensions used by views.
- [Extensions/LicenseEnforcementMiddleware.cs](../../Extensions/LicenseEnforcementMiddleware.cs): Runtime license-expiry access restriction middleware.
- [Extensions/ModuleEntitlementMiddleware.cs](../../Extensions/ModuleEntitlementMiddleware.cs): Module key path mapping and entitlement gating middleware.
- [Extensions/SecurityHeadersMiddleware.cs](../../Extensions/SecurityHeadersMiddleware.cs): Security headers, basic rate-limiting/input-validation/API security middleware components.

## 4.3 Services/Filters

- [Services/Filters/LicenseExpiryFilter.cs](../../Services/Filters/LicenseExpiryFilter.cs): Global MVC action filter for license-expiry role behavior.

## 4.4 Services/Interfaces

- [Services/Interfaces/IServices.cs](../../Services/Interfaces/IServices.cs): Central interface contracts for all major modules/services.

This file defines the contracts for:
- Core platform: settings, authz, audit, files, exports, notifications, sessions.
- Clinical and diagnostics: OPD, IPD, Prescription, Lab, Radiology.
- Operations/admin: Attendance, Leave, Payroll, FrontOffice, Certificate.
- Governance: License, Module management.
- AI: Chatbot moderation, prompt, knowledge, consent, data cleanup.

## 4.5 Services/Implementations

Core platform services:
- [Services/Implementations/DatabaseInitializer.cs](../../Services/Implementations/DatabaseInitializer.cs): Schema safety checks, seed defaults, role/user bootstrap.
- [Services/Implementations/SettingService.cs](../../Services/Implementations/SettingService.cs): Reads/writes system settings and feature configuration.
- [Services/Implementations/AuditService.cs](../../Services/Implementations/AuditService.cs): Writes and retrieves audit logs.
- [Services/Implementations/FileService.cs](../../Services/Implementations/FileService.cs): File upload/delete/read and URL generation.
- [Services/Implementations/ExportService.cs](../../Services/Implementations/ExportService.cs): CSV and PDF table export generation.
- [Services/Implementations/CacheService.cs](../../Services/Implementations/CacheService.cs): Cache abstraction for speed optimization.

Authorization and module governance:
- [Services/Implementations/AuthorizationService.cs](../../Services/Implementations/AuthorizationService.cs): Role and permission lookup/assignment logic.
- [Services/Implementations/ModuleService.cs](../../Services/Implementations/ModuleService.cs): Global and per-user module enablement rules.

License services:
- [Services/Implementations/LicenseService.cs](../../Services/Implementations/LicenseService.cs): Active license state, reminders, renewals, access checks.
- [Services/Implementations/LicenseFileService.cs](../../Services/Implementations/LicenseFileService.cs): Validate and activate uploaded license file.
- [Services/Implementations/LicenseCryptoUtility.cs](../../Services/Implementations/LicenseCryptoUtility.cs): Signature/verification utility logic.
- [Services/Implementations/LicenseReminderHostedService.cs](../../Services/Implementations/LicenseReminderHostedService.cs): Background license reminder scheduler.

Concurrency and notifications:
- [Services/Implementations/ConcurrentSessionService.cs](../../Services/Implementations/ConcurrentSessionService.cs): Tracks active sessions and concurrent login policies.
- [Services/Implementations/SystemNotificationService.cs](../../Services/Implementations/SystemNotificationService.cs): In-app notification creation/mark-read/delete.
- [Services/Implementations/NotificationDeliveryAuditService.cs](../../Services/Implementations/NotificationDeliveryAuditService.cs): Records outbound delivery logs.
- [Services/Implementations/PublicBookingNotificationService.cs](../../Services/Implementations/PublicBookingNotificationService.cs): Sends booking notifications after public appointment events.

Email/SMS providers:
- [Services/Implementations/SmtpEmailNotificationProvider.cs](../../Services/Implementations/SmtpEmailNotificationProvider.cs): SMTP email sender.
- [Services/Implementations/SmtpHealthService.cs](../../Services/Implementations/SmtpHealthService.cs): SMTP configuration and connectivity health checks.
- [Services/Implementations/TwilioSmsNotificationProvider.cs](../../Services/Implementations/TwilioSmsNotificationProvider.cs): Twilio SMS sender.
- [Services/Implementations/AfricaTalkingSmsNotificationProvider.cs](../../Services/Implementations/AfricaTalkingSmsNotificationProvider.cs): Africa's Talking SMS sender.
- [Services/Implementations/SmsNotificationProviderRouter.cs](../../Services/Implementations/SmsNotificationProviderRouter.cs): Runtime provider selection.
- [Services/Implementations/SmsLogNotificationProvider.cs](../../Services/Implementations/SmsLogNotificationProvider.cs): Logging-only SMS fallback provider.

Module business services:
- [Services/Implementations/PatientService.cs](../../Services/Implementations/PatientService.cs): Patient CRUD/search and profile-related logic.
- [Services/Implementations/AppointmentService.cs](../../Services/Implementations/AppointmentService.cs): Appointment lifecycle operations.
- [Services/Implementations/BillingService.cs](../../Services/Implementations/BillingService.cs): Billing, payment processing, financial aggregates.
- [Services/Implementations/StaffService.cs](../../Services/Implementations/StaffService.cs): Staff management, password/profile updates, role assignment.
- [Services/Implementations/PatientPortalService.cs](../../Services/Implementations/PatientPortalService.cs): Patient portal booking, records, billing, notification flows.
- [Services/Implementations/OPDService.cs](../../Services/Implementations/OPDService.cs): OPD visits and related analytics.
- [Services/Implementations/IPDService.cs](../../Services/Implementations/IPDService.cs): Admission/discharge workflows and IPD metrics.
- [Services/Implementations/WardService.cs](../../Services/Implementations/WardService.cs): Ward availability and occupancy logic.
- [Services/Implementations/BedService.cs](../../Services/Implementations/BedService.cs): Bed state and assignment support.
- [Services/Implementations/PrescriptionService.cs](../../Services/Implementations/PrescriptionService.cs): Prescription, medicine stock, pharmacy billing operations.
- [Services/Implementations/LabService.cs](../../Services/Implementations/LabService.cs): Lab catalog/results workflows and KPIs.
- [Services/Implementations/RadiologyService.cs](../../Services/Implementations/RadiologyService.cs): Radiology catalog/results workflows and KPIs.
- [Services/Implementations/BloodBankService.cs](../../Services/Implementations/BloodBankService.cs): Blood inventory and issue operations.
- [Services/Implementations/OperationTheatreService.cs](../../Services/Implementations/OperationTheatreService.cs): OT schedule and status workflows.
- [Services/Implementations/ReferralService.cs](../../Services/Implementations/ReferralService.cs): Referral creation and status transitions.
- [Services/Implementations/AttendanceService.cs](../../Services/Implementations/AttendanceService.cs): Attendance check-in/out and summaries.
- [Services/Implementations/LeaveService.cs](../../Services/Implementations/LeaveService.cs): Leave types, requests, approvals, balances.
- [Services/Implementations/PayrollService.cs](../../Services/Implementations/PayrollService.cs): Payroll generation and paid status handling.
- [Services/Implementations/FrontOfficeService.cs](../../Services/Implementations/FrontOfficeService.cs): Visitors, complaints, dispatch/receive.
- [Services/Implementations/CertificateService.cs](../../Services/Implementations/CertificateService.cs): Certificates and ID card generation.
- [Services/Implementations/ReportService.cs](../../Services/Implementations/ReportService.cs): Department, finance, occupancy, staff reports.
- [Services/Implementations/ReportTemplateService.cs](../../Services/Implementations/ReportTemplateService.cs): Custom report template CRUD and execution.

Chatbot services:
- [Services/Implementations/OpenAiChatbotService.cs](../../Services/Implementations/OpenAiChatbotService.cs): Chat orchestration and model invocation.
- [Services/Implementations/ChatbotModerationService.cs](../../Services/Implementations/ChatbotModerationService.cs): Input/output moderation.
- [Services/Implementations/ChatbotPromptBuilder.cs](../../Services/Implementations/ChatbotPromptBuilder.cs): System prompt composition by context.
- [Services/Implementations/ChatbotKnowledgeService.cs](../../Services/Implementations/ChatbotKnowledgeService.cs): Knowledge retrieval and context building.
- [Services/Implementations/ChatbotPiiRedactionService.cs](../../Services/Implementations/ChatbotPiiRedactionService.cs): Sensitive-data redaction in logs/events.
- [Services/Implementations/ChatbotConsentService.cs](../../Services/Implementations/ChatbotConsentService.cs): Consent lifecycle and audit.
- [Services/Implementations/ChatbotDataCleanupService.cs](../../Services/Implementations/ChatbotDataCleanupService.cs): Data retention cleanup operations.
- [Services/Implementations/ChatbotDataCleanupHostedService.cs](../../Services/Implementations/ChatbotDataCleanupHostedService.cs): Background cleanup scheduler.

## 4.6 Controllers (Staff/Admin Portal)

Authentication, governance, and shell:
- [Controllers/AccountController.cs](../../Controllers/AccountController.cs): Signup/login/credential-validation/logout, role-based redirection, account activation gating.
- [Controllers/AccountsApprovalController.cs](../../Controllers/AccountsApprovalController.cs): Pending account approval/rejection and password admin actions.
- [Controllers/DashboardController.cs](../../Controllers/DashboardController.cs): Role dashboard entry and dashboard KPIs.
- [Controllers/ModuleManagementController.cs](../../Controllers/ModuleManagementController.cs): Module enablement UI and user/module access controls.
- [Controllers/LicenseController.cs](../../Controllers/LicenseController.cs): License key/config upload, entitlement matrix, renewals, reminders.

Core hospital workflows:
- [Controllers/PatientController.cs](../../Controllers/PatientController.cs): Patient registration, profile, search, and maintenance.
- [Controllers/AppointmentController.cs](../../Controllers/AppointmentController.cs): Appointment list/create/edit/status operations.
- [Controllers/OPDController.cs](../../Controllers/OPDController.cs): Outpatient visit lifecycle.
- [Controllers/IPDController.cs](../../Controllers/IPDController.cs): Inpatient admission, bed assignment, discharge.
- [Controllers/BillingController.cs](../../Controllers/BillingController.cs): Bill creation, payment, receipts, exports.
- [Controllers/PrescriptionController.cs](../../Controllers/PrescriptionController.cs): Prescription and pharmacy workflow.
- [Controllers/LabController.cs](../../Controllers/LabController.cs): Lab test catalog and results operations.
- [Controllers/RadiologyController.cs](../../Controllers/RadiologyController.cs): Radiology catalog and results operations.
- [Controllers/BloodBankController.cs](../../Controllers/BloodBankController.cs): Blood inventory and issue management.
- [Controllers/OperationTheatreController.cs](../../Controllers/OperationTheatreController.cs): OT scheduling and status management.
- [Controllers/ReferralController.cs](../../Controllers/ReferralController.cs): Referral workflows and status handling.

Administrative/operational modules:
- [Controllers/AttendanceController.cs](../../Controllers/AttendanceController.cs): Attendance operations and summaries.
- [Controllers/LeaveController.cs](../../Controllers/LeaveController.cs): Leave request and approval workflows.
- [Controllers/PayrollController.cs](../../Controllers/PayrollController.cs): Payroll generation/payment views.
- [Controllers/FrontOfficeController.cs](../../Controllers/FrontOfficeController.cs): Visitors, complaints, dispatch/receive, front-office tasks.
- [Controllers/CertificateController.cs](../../Controllers/CertificateController.cs): Certificates and ID card workflows.
- [Controllers/ReportController.cs](../../Controllers/ReportController.cs): Report generation, export, templates/schedules integration.

Support and platform modules:
- [Controllers/AuditController.cs](../../Controllers/AuditController.cs): Audit trail browsing and filters.
- [Controllers/NotificationsController.cs](../../Controllers/NotificationsController.cs): In-app notification endpoints.
- [Controllers/ChatbotController.cs](../../Controllers/ChatbotController.cs): End-user chatbot interactions and session messages.
- [Controllers/ChatbotAdminController.cs](../../Controllers/ChatbotAdminController.cs): Chatbot analytics/settings/escalation handling.

CMS and public-site management:
- [Controllers/CmsController.cs](../../Controllers/CmsController.cs): CMS pages, menu items, notices, notification settings, SMTP checks.
- [Controllers/PublicSiteAdminController.cs](../../Controllers/PublicSiteAdminController.cs): Public site style/content administration.
- [Controllers/SiteController.cs](../../Controllers/SiteController.cs): Public-facing website pages and public appointment booking.

Framework/default controllers:
- [Controllers/HomeController.cs](../../Controllers/HomeController.cs): Home/error base endpoints.
- [Controllers/AppController.cs](../../Controllers/AppController.cs): Application-level helpers/routes used by shell/public context.
- [Controllers/StaffController.cs](../../Controllers/StaffController.cs): Staff-specific management endpoints.

## 4.7 Controllers (Patient Portal)

- [Controllers/PatientPortal/AccountController.cs](../../Controllers/PatientPortal/AccountController.cs): Patient login/register/logout/password reset and approval-aware registration behavior.
- [Controllers/PatientPortal/DashboardController.cs](../../Controllers/PatientPortal/DashboardController.cs): Patient dashboard summary widgets and quick actions.
- [Controllers/PatientPortal/AppointmentsController.cs](../../Controllers/PatientPortal/AppointmentsController.cs): Patient booking, reschedule, and cancellation flows.
- [Controllers/PatientPortal/BillsController.cs](../../Controllers/PatientPortal/BillsController.cs): Patient bill list/details/payment-history views.
- [Controllers/PatientPortal/MedicalRecordsController.cs](../../Controllers/PatientPortal/MedicalRecordsController.cs): Patient records, test results, report downloads.
- [Controllers/PatientPortal/SettingsController.cs](../../Controllers/PatientPortal/SettingsController.cs): Profile and account preference operations.

## 4.8 Models (Domain Entities)

- [Models/ApplicationUser.cs](../../Models/ApplicationUser.cs): Identity user extension for Medyx-specific fields and account flags.
- [Models/Patient.cs](../../Models/Patient.cs): Patient entity and related health profile data.
- [Models/OPD.cs](../../Models/OPD.cs): OPD visit and outpatient domain models.
- [Models/MedicalRecord.cs](../../Models/MedicalRecord.cs): Shared medical record and test result entities.
- [Models/Billing.cs](../../Models/Billing.cs): Bills, bill items, payments, transactions, finance entities.
- [Models/Pharmacy.cs](../../Models/Pharmacy.cs): Medicine, pharmacy bill, prescription entities.
- [Models/Lab.cs](../../Models/Lab.cs): Lab test and lab result domain entities.
- [Models/Radiology.cs](../../Models/Radiology.cs): Radiology test and result domain entities.
- [Models/SpecializedServices.cs](../../Models/SpecializedServices.cs): OT, blood bank, referral, and related module entities.
- [Models/HR.cs](../../Models/HR.cs): Attendance, leave, payroll, and HR entities.
- [Models/CMS.cs](../../Models/CMS.cs): CMS pages/menu/notices/public booking entities.
- [Models/Licensing.cs](../../Models/Licensing.cs): License record, audit, reminders, and snapshot structures.
- [Models/Chatbot.cs](../../Models/Chatbot.cs): Chat sessions, messages, feedback, escalations, and analytics entities.
- [Models/Settings.cs](../../Models/Settings.cs): General settings/config entities.
- [Models/NotificationDeliveryLog.cs](../../Models/NotificationDeliveryLog.cs): Notification provider delivery log entity.
- [Models/UserSession.cs](../../Models/UserSession.cs): Concurrent session tracking entity.
- [Models/OperationalHealth.cs](../../Models/OperationalHealth.cs): Health/diagnostic support models.
- [Models/RBAC.cs](../../Models/RBAC.cs): Roles/features/role-feature mapping entities.
- [Models/AccountApprovalRequest.cs](../../Models/AccountApprovalRequest.cs): Signup approval queue entity.
- [Models/ReportModels.cs](../../Models/ReportModels.cs): Generated report, schedule, and report-related entities.
- [Models/PatientClinicalExtensions.cs](../../Models/PatientClinicalExtensions.cs): Patient clinical helper extensions and convenience structures.
- [Models/ErrorViewModel.cs](../../Models/ErrorViewModel.cs): Standard MVC error view model.

## 4.9 ViewModels (UI Models)

- [ViewModels/AccountViewModels.cs](../../ViewModels/AccountViewModels.cs): Register/login/reset/password model definitions.
- [ViewModels/AppointmentViewModels.cs](../../ViewModels/AppointmentViewModels.cs): Appointment UI forms and list projections.
- [ViewModels/PatientViewModels.cs](../../ViewModels/PatientViewModels.cs): Patient create/edit/profile models.
- [ViewModels/OPDViewModels.cs](../../ViewModels/OPDViewModels.cs): OPD form and list/detail view models.
- [ViewModels/BillingViewModels.cs](../../ViewModels/BillingViewModels.cs): Billing form/list/payment/receipt models.
- [ViewModels/LabViewModels.cs](../../ViewModels/LabViewModels.cs): Lab test/result UI model structures.
- [ViewModels/RadiologyViewModels.cs](../../ViewModels/RadiologyViewModels.cs): Radiology test/result UI model structures.
- [ViewModels/PrescriptionViewModels.cs](../../ViewModels/PrescriptionViewModels.cs): Prescription/pharmacy UI model structures.
- [ViewModels/AttendanceViewModels.cs](../../ViewModels/AttendanceViewModels.cs): Attendance screens and summaries.
- [ViewModels/LeaveViewModels.cs](../../ViewModels/LeaveViewModels.cs): Leave request/approval/list model structures.
- [ViewModels/PayrollViewModels.cs](../../ViewModels/PayrollViewModels.cs): Payroll generation and list/detail models.
- [ViewModels/FrontOfficeViewModels.cs](../../ViewModels/FrontOfficeViewModels.cs): Visitor/complaint/dispatch UI models.
- [ViewModels/CertificateViewModels.cs](../../ViewModels/CertificateViewModels.cs): Certificate and ID card UI models.
- [ViewModels/ModuleManagementViewModels.cs](../../ViewModels/ModuleManagementViewModels.cs): User-module matrix and module governance models.
- [ViewModels/LicenseViewModels.cs](../../ViewModels/LicenseViewModels.cs): License management and expired/locked page models.
- [ViewModels/ChatbotViewModels.cs](../../ViewModels/ChatbotViewModels.cs): Chat UI messages, settings, analytics models.
- [ViewModels/CmsViewModels.cs](../../ViewModels/CmsViewModels.cs): CMS page/menu/notice/settings models.
- [ViewModels/ReportViewModels.cs](../../ViewModels/ReportViewModels.cs): Report filters/results/template/schedule models.
- [ViewModels/PatientPortalViewModels.cs](../../ViewModels/PatientPortalViewModels.cs): Patient portal specific forms and dashboard models.
- [ViewModels/SiteViewModels.cs](../../ViewModels/SiteViewModels.cs): Public site and booking page UI models.
- [ViewModels/StaffViewModels.cs](../../ViewModels/StaffViewModels.cs): Staff profile and management view models.
- [ViewModels/AuditViewModels.cs](../../ViewModels/AuditViewModels.cs): Audit search/list/detail view models.

## 4.10 DTOs (API and Transfer Objects)

- [DTOs/AppointmentDtos.cs](../../DTOs/AppointmentDtos.cs): Appointment API request/response contracts.
- [DTOs/BillingDtos.cs](../../DTOs/BillingDtos.cs): Billing/payment transfer contracts.
- [DTOs/PatientDtos.cs](../../DTOs/PatientDtos.cs): Patient profile/search transfer contracts.
- [DTOs/OPDDtos.cs](../../DTOs/OPDDtos.cs): OPD transfer contracts.
- [DTOs/LabDtos.cs](../../DTOs/LabDtos.cs): Lab catalog/result transfer contracts.
- [DTOs/RadiologyDtos.cs](../../DTOs/RadiologyDtos.cs): Radiology transfer contracts.
- [DTOs/PrescriptionDtos.cs](../../DTOs/PrescriptionDtos.cs): Prescription/medicine transfer contracts.
- [DTOs/StaffDtos.cs](../../DTOs/StaffDtos.cs): Staff management transfer contracts.
- [DTOs/PatientPortalDtos.cs](../../DTOs/PatientPortalDtos.cs): Patient portal transfer contracts.
- [DTOs/MobileApiDtos.cs](../../DTOs/MobileApiDtos.cs): Mobile compatibility layer DTO contracts.

## 4.11 Views (Razor)

Top-level areas under [Views](../../Views) mirror controller names.

Important shell files:
- [Views/_ViewStart.cshtml](../../Views/_ViewStart.cshtml): Shared view startup behavior.
- [Views/_ViewImports.cshtml](../../Views/_ViewImports.cshtml): Razor namespace/tag helper imports.
- [Views/Shared/_Layout.cshtml](../../Views/Shared/_Layout.cshtml): Main authenticated layout and nav shell.
- [Views/Shared/_PublicLayout.cshtml](../../Views/Shared/_PublicLayout.cshtml): Public-site layout shell.
- [Views/Shared/Error.cshtml](../../Views/Shared/Error.cshtml): Error display page.

Patient portal views are under [Views/PatientPortal](../../Views/PatientPortal), separated into Account, Appointments, Bills, Dashboard, MedicalRecords, Settings.

## 4.12 Static Assets

- [wwwroot/css](../../wwwroot/css): Site/app stylesheets.
- [wwwroot/js](../../wwwroot/js): Front-end scripts.
- [wwwroot/images](../../wwwroot/images): Images/icons.
- [wwwroot/lib](../../wwwroot/lib): Third-party front-end libraries.
- [wwwroot/uploads](../../wwwroot/uploads): Runtime uploaded files.

## 4.13 Scripts and Operational SQL/PowerShell

Key automation and deployment assets in [scripts](../../scripts):
- [scripts/New-Database.sql](../../scripts/New-Database.sql): Database creation script.
- [scripts/New-Database.Validation.sql](../../scripts/New-Database.Validation.sql): Validation checks for schema.
- [scripts/SeedDemoData.sql](../../scripts/SeedDemoData.sql): Demo/test data seeding.
- [scripts/StoredProcedures_Reports.sql](../../scripts/StoredProcedures_Reports.sql): Reporting stored procedures and related DB objects.
- [scripts/Import-HospitalDemoData.ps1](../../scripts/Import-HospitalDemoData.ps1): PowerShell data import workflow.
- [scripts/Validate-DatabaseDeployment.ps1](../../scripts/Validate-DatabaseDeployment.ps1): Post-deployment validation automation.
- [scripts/Invoke-UatSmoke.ps1](../../scripts/Invoke-UatSmoke.ps1): UAT smoke run automation.
- [scripts/Invoke-LicenseToolAutomation.ps1](../../scripts/Invoke-LicenseToolAutomation.ps1): License tool automation support.

## 4.14 Documentation

Primary docs folder: [docs](../../docs)

High-value references:
- [docs/PRD.md](../../docs/PRD.md): Product requirements and module scope.
- [docs/Final-touches.md](../../docs/Final-touches.md): Role/portal/module/report/chatbot inventory.
- [docs/Final-TODOList.md](../../docs/Final-TODOList.md): Completed and pending implementation tasks.
- [docs/LICENSING-DESIGN.md](../../docs/LICENSING-DESIGN.md): Licensing architecture rationale.
- [docs/OPENAI-CHATBOT-DESIGN.md](../../docs/OPENAI-CHATBOT-DESIGN.md): Chatbot governance and design.
- [docs/DEPLOYMENT-RUNBOOK.md](../../docs/DEPLOYMENT-RUNBOOK.md): Deployment operations guide.

## 5. Recommended Reading Order for New Programmers

1. [Program.cs](../../Program.cs)
2. [MedyxHMS.csproj](../../MedyxHMS.csproj)
3. [appsettings.json](../../appsettings.json)
4. [Data/ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs)
5. [Services/Interfaces/IServices.cs](../../Services/Interfaces/IServices.cs)
6. [Controllers/AccountController.cs](../../Controllers/AccountController.cs)
7. [Controllers/PatientPortal/AccountController.cs](../../Controllers/PatientPortal/AccountController.cs)
8. [Extensions/LicenseEnforcementMiddleware.cs](../../Extensions/LicenseEnforcementMiddleware.cs)
9. [Extensions/ModuleEntitlementMiddleware.cs](../../Extensions/ModuleEntitlementMiddleware.cs)
10. [Services/Implementations/DatabaseInitializer.cs](../../Services/Implementations/DatabaseInitializer.cs)
11. [Controllers/LicenseController.cs](../../Controllers/LicenseController.cs)
12. One vertical slice end-to-end:
- [Controllers/AppointmentController.cs](../../Controllers/AppointmentController.cs)
- [Services/Implementations/AppointmentService.cs](../../Services/Implementations/AppointmentService.cs)
- [ViewModels/AppointmentViewModels.cs](../../ViewModels/AppointmentViewModels.cs)
- [Views/Appointment](../../Views/Appointment)

## 6. Common Development Workflows

## 6.1 Add a New Feature Module

1. Add or update entities in [Models](../../Models).
2. Register new DbSet/relationship in [Data/ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs).
3. Add interface methods in [Services/Interfaces/IServices.cs](../../Services/Interfaces/IServices.cs).
4. Implement service in [Services/Implementations](../../Services/Implementations).
5. Register service in [Program.cs](../../Program.cs).
6. Add controller actions in [Controllers](../../Controllers).
7. Add ViewModels in [ViewModels](../../ViewModels).
8. Add Razor views in [Views](../../Views).
9. If license-gated, map route prefix in [Extensions/ModuleEntitlementMiddleware.cs](../../Extensions/ModuleEntitlementMiddleware.cs).
10. If module-toggle needed, integrate with [Services/Implementations/ModuleService.cs](../../Services/Implementations/ModuleService.cs).

## 6.2 Add New Report

1. Add service methods in:
- [Services/Interfaces/IServices.cs](../../Services/Interfaces/IServices.cs)
- [Services/Implementations/ReportService.cs](../../Services/Implementations/ReportService.cs)
2. Add optional stored procedure in [scripts/StoredProcedures_Reports.sql](../../scripts/StoredProcedures_Reports.sql).
3. Add report view models in [ViewModels/ReportViewModels.cs](../../ViewModels/ReportViewModels.cs).
4. Add controller action in [Controllers/ReportController.cs](../../Controllers/ReportController.cs).
5. Add export path using [Services/Implementations/ExportService.cs](../../Services/Implementations/ExportService.cs).

## 6.3 Add Patient Portal Capability

1. Implement service behavior in [Services/Implementations/PatientPortalService.cs](../../Services/Implementations/PatientPortalService.cs).
2. Expose action in [Controllers/PatientPortal](../../Controllers/PatientPortal).
3. Add portal-specific view models in [ViewModels/PatientPortalViewModels.cs](../../ViewModels/PatientPortalViewModels.cs).
4. Add views in [Views/PatientPortal](../../Views/PatientPortal).
5. Ensure role restrictions remain Patient-only.

## 6.4 Add Notification Event

1. Compose notification in relevant module service/controller.
2. Use [Services/Implementations/SystemNotificationService.cs](../../Services/Implementations/SystemNotificationService.cs) for in-app events.
3. Use email/SMS providers via router/service if external message required.
4. Audit outbound delivery in [Services/Implementations/NotificationDeliveryAuditService.cs](../../Services/Implementations/NotificationDeliveryAuditService.cs).

## 7. Architectural Rules and Pitfalls

- Keep cross-portal routes explicit; avoid ambiguous redirects between main and PatientPortal controllers.
- Do not bypass service layer from controllers for business rules.
- Keep authorization at action level and in middleware/filter policy.
- Remember dual gating for modules:
- global/per-user module toggle
- license entitlement
- Seed and migration safety logic is startup critical; review changes to [Services/Implementations/DatabaseInitializer.cs](../../Services/Implementations/DatabaseInitializer.cs) carefully.
- Update both interface and implementation whenever changing a service contract.
- Avoid hardcoding keys/secrets; use settings and environment configuration.

## 8. Quick Module-to-Controller Reference

- Dashboard: [Controllers/DashboardController.cs](../../Controllers/DashboardController.cs)
- Patients: [Controllers/PatientController.cs](../../Controllers/PatientController.cs)
- Appointments: [Controllers/AppointmentController.cs](../../Controllers/AppointmentController.cs)
- OPD: [Controllers/OPDController.cs](../../Controllers/OPDController.cs)
- IPD: [Controllers/IPDController.cs](../../Controllers/IPDController.cs)
- Billing: [Controllers/BillingController.cs](../../Controllers/BillingController.cs)
- Prescription/Pharmacy: [Controllers/PrescriptionController.cs](../../Controllers/PrescriptionController.cs)
- Lab: [Controllers/LabController.cs](../../Controllers/LabController.cs)
- Radiology: [Controllers/RadiologyController.cs](../../Controllers/RadiologyController.cs)
- Blood Bank: [Controllers/BloodBankController.cs](../../Controllers/BloodBankController.cs)
- Operation Theatre: [Controllers/OperationTheatreController.cs](../../Controllers/OperationTheatreController.cs)
- Front Office: [Controllers/FrontOfficeController.cs](../../Controllers/FrontOfficeController.cs)
- Attendance: [Controllers/AttendanceController.cs](../../Controllers/AttendanceController.cs)
- Leave: [Controllers/LeaveController.cs](../../Controllers/LeaveController.cs)
- Payroll: [Controllers/PayrollController.cs](../../Controllers/PayrollController.cs)
- Certificates: [Controllers/CertificateController.cs](../../Controllers/CertificateController.cs)
- Referrals: [Controllers/ReferralController.cs](../../Controllers/ReferralController.cs)
- Reports: [Controllers/ReportController.cs](../../Controllers/ReportController.cs)
- CMS: [Controllers/CmsController.cs](../../Controllers/CmsController.cs)
- Chatbot (user): [Controllers/ChatbotController.cs](../../Controllers/ChatbotController.cs)
- Chatbot (admin): [Controllers/ChatbotAdminController.cs](../../Controllers/ChatbotAdminController.cs)
- Licensing: [Controllers/LicenseController.cs](../../Controllers/LicenseController.cs)
- Patient Portal: [Controllers/PatientPortal](../../Controllers/PatientPortal)

## 9. First-Week Onboarding Checklist

1. Run project locally and confirm login + dashboard load.
2. Read startup and pipeline files (Program, middleware, filters).
3. Follow one request end-to-end (Appointment or Patient) from controller to service to DbContext to view.
4. Trace one role-routing flow (main portal vs patient portal).
5. Trace one license-restricted route and verify feature-locked behavior.
6. Generate one report and one export (CSV/PDF).
7. Review docs in [docs](../../docs) for product context and deployment process.

This completes the programmer workflow training map for the current ASP.NET codebase.