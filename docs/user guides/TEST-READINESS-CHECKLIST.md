# TEST READINESS CHECKLIST


> Last Updated: 2026-04-22
> Operational Baseline: runtime stabilization complete, automated UAT technical gates passing.
> References: docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md, docs/UPDATED-PRD-2026-04-22.md, docs/UPDATED-TODO-LIST-2026-04-22.md

Use this checklist to validate end-to-end behavior before UAT.

## 1) Licensing Desktop Tool and Upload
- Build and run MedyxHMS-Lic desktop app from MedyxHMS-Lic project.
- Generate MedyxHMS.lic with module checklist and MaxConcurrentUsers.
- Upload in License page as SuperAdmin.
- Confirm license accepted and entitlement matrix rendered.

## 2) Concurrent User Limits
- Use test users in non-exempt roles (for example Staff/Receptionist).
- Login concurrently across multiple browsers until limit is hit.
- Verify Admin, SuperAdmin, and Patient remain exempt from concurrency blocking.

## 3) Login and Redirects
- Check Login button appears on public/app layouts.
- Verify role-based redirect after login:
  - Patient -> PatientPortal/Dashboard
  - Receptionist -> FrontOffice
  - Accountant -> Billing
  - Pharmacist -> Prescription
  - Nurse -> IPD
  - Doctor -> OPD
  - Admin/SuperAdmin -> Dashboard

## 4) Module and Export Validation
- Open major modules: Patient, Appointment, OPD, IPD, Billing, Lab, Pharmacy, Reports.
- Confirm CSV/PDF export options produce downloadable files.

## 5) Security Checks
- Confirm only configured CORS origins are allowed.
- Confirm HTTPS redirection/HSTS behavior in non-development.
- Confirm anti-forgery on forms and role-based authorization.

## 6) Report Governance
- Verify ScheduleReport/DeleteReport/DeleteSchedule are editable by Admin/SuperAdmin only.

## 7) AI Assistant Availability
- Confirm floating AI button appears at bottom-right for guest and authenticated pages.
- Confirm Chatbot page opens for both anonymous and authenticated users.

## 8) Email Functionality
- Configure Notification:Smtp settings.
- Trigger an email workflow and verify delivery/logs.

## 9) Database Completeness
- Run database initializer and migration validation scripts.
- Compare required module fields against PHP reference and test CRUD screens.

## 10) Final Build/Test
- Run dotnet build on MedyxHMS ASP.NET project.
- Run automated tests in tests/MedyxHMS.Tests.
- Execute smoke login, CRUD, export, and chatbot checks.
