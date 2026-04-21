# UAT Execution Evidence (2026-04-22)

## Scope
This document records the UAT checklist execution that was possible to validate automatically in the current environment.

## Automated Validation Results

### 1) Build Validation
- Command: dotnet build MedyxHMS.csproj -v minimal
- Result: PASS
- Notes: Build completed successfully.

### 2) Test Validation
- Command: dotnet test tests/MedyxHMS.MobileApi.Tests/MedyxHMS.MobileApi.Tests.csproj -v minimal
- Result: PASS
- Summary: 3 passed, 0 failed, 0 skipped

- Command: dotnet test tests/MedyxHMS.Chatbot.Security.Tests/MedyxHMS.Chatbot.Security.Tests.csproj -v minimal
- Result: PASS
- Summary: 6 passed, 0 failed, 0 skipped

### 3) Runtime Startup Validation
- Command: dotnet run --project MedyxHMS.csproj --urls http://localhost:5044
- Result: PASS
- Notes:
  - Application started and served HTTP requests.
  - Report stored procedures/index ensure step completed successfully at startup.

### 4) Route Smoke Validation

Public routes (anonymous expected 200):
- / -> 200 (PASS)
- /Account/Login -> 200 (PASS)
- /Account/Register -> 200 (PASS)
- /PatientPortal/Account/Register -> 200 (PASS)

Protected routes (anonymous expected 302 redirect):
- /Dashboard/Index -> 302 (PASS)
- /Report/Index -> 302 (PASS)
- /Report/GeneratedReports -> 302 (PASS)
- /Report/ScheduleReport -> 302 (PASS)
- /License/Expired -> 302 (PASS)

## Known Non-Blocking Observations
- Startup warning observed for SavedReport.ExecutionTimeMs decimal precision mapping.
- Startup warning observed for HTTPS port redirection detection in current run configuration.
- Current warning baseline remains 1099 compiler warnings (no build errors).

## Checklist Items Not Fully Executed (Environment-Limited)
The following UAT checklist areas require role credentials, seeded business data, or manual UI verification and were not fully executable in this automated run:
- Role-by-role authenticated dashboard and module navigation validation.
- Dynamic multi-role selection UX validation.
- License upload/expiry/recovery flow validation through UI.
- Admin/SuperAdmin module governance UI actions.
- Data-accuracy checks for dashboard aggregates, charts, and report outputs.
- End-to-end billing/front-office transactional flow verification.
- Manual audit-log evidence attachment and role screenshot collection.

## Exit State for This Run
- Automated technical gate: PASS
- Full business UAT signoff: PENDING (manual role-based execution required)

## Recommended Immediate Next Step
Execute docs/UAT-ROLE-VALIDATION-CHECKLIST-2026-04-22.md with prepared credentials and seeded data, then append role-wise screenshots and pass/fail notes to this evidence file.
