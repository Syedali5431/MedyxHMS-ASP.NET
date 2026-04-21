# UAT Role Validation Checklist (2026-04-22)

## Objective
Validate role-based access, module visibility, and core business workflows before deployment.

## Test Data Preconditions
- Seed representative data for patients, appointments, IPD, billing, reports, payroll, and attendance.
- Create at least one active user for each role:
  - SuperAdmin
  - Admin
  - Accountant
  - Doctor
  - Nurse
  - Receptionist
  - Patient
- Ensure at least one multi-role user exists for dynamic role selection validation.

## Access and Authentication
1. Open login page and authenticate with each role.
2. Verify expected post-login landing route for each role.
3. Verify unauthenticated access to protected routes returns redirect (302) to login.
4. Verify logout invalidates session and protected routes redirect again.

## Dynamic Role Selection
1. Log in with multi-role user.
2. Confirm only assigned roles appear in role picker.
3. Select each role and verify role-specific dashboard and menu rendering.
4. Confirm active role persists correctly for module authorization checks.

## Licensing and Entitlements
1. Upload a valid license file and confirm expiry and module entitlements display correctly.
2. Verify module access gates behavior for disabled modules.
3. Verify SuperAdmin and Admin exemption behavior matches current policy.
4. Validate expired-license behavior and recovery after valid renewal upload.

## Module Management Governance
1. As SuperAdmin, toggle global module states and verify system-wide effects.
2. As Admin, toggle per-user module access where allowed.
3. Confirm disabled modules are hidden/blocked in navigation and direct route access.

## Functional Workflow Validation
### Dashboard and Analytics
1. Validate widgets, charts, and summary cards render without runtime errors.
2. Spot-check metric totals against known seeded records.

### Reports
1. Open report index and each report page.
2. Generate Department, Occupancy, Staff, and Payroll reports.
3. Validate generated report list, edit flow, and delete behavior.
4. Verify schedule creation and deletion paths.

### Patient Portal
1. Validate patient registration page loads and submits correctly.
2. Validate login and dashboard access.
3. Validate patient-visible report/lab/radiology areas if enabled.

### Billing and Front Office
1. Validate bill creation, listing, payment capture, and status transitions.
2. Validate front-office visitor/complaint/dispatch flows.

## Security and Auditability
1. Confirm unauthorized access attempts are blocked and routed correctly.
2. Confirm key administrative actions produce audit/log records.
3. Confirm no sensitive stack traces leak to end users on handled errors.

## Exit Criteria
- No P0/P1 defects open.
- All role access tests pass.
- Report generation/editing and dashboard checks pass with accurate seeded data sampling.
- License and module governance behavior passes.
- Signoff from QA + product owner.

## Evidence to Attach
- Route status smoke output.
- Screen captures for each role dashboard.
- Sample report output and edited report confirmation.
- License screen screenshots (entitlements/expiry).
- Defect list with severity and disposition.
