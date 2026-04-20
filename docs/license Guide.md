# License Guide

## Purpose
This guide explains how licensing is implemented in MedyxHMS and how to renew/update license expiry securely so clients cannot change expiry from the UI or browser side.

## Where License Is Stored
Licensing in this system is server-side and database-backed (not client-side file-based):

- License state table: `LicenseRecords`
- Audit trail table: `LicenseAuditLogs`
- Reminder history table: `LicenseReminderLogs`

Primary model classes:
- `Models/Licensing.cs`

Initialization and default seeding:
- `Services/Implementations/DatabaseInitializer.cs`

## Core Security Design
The client cannot directly set license expiry because all critical checks and updates are enforced on the server.

### 1) Server-Side Authorization for Renewal
Renewal endpoints are restricted to `SuperAdmin` only:

- Controller: `Controllers/LicenseController.cs`
- Actions:
  - `Index` -> `[Authorize(Roles = "SuperAdmin")]`
  - `Renew` -> `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[Authorize(Roles = "SuperAdmin")]`
  - `SendReminder` -> `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[Authorize(Roles = "SuperAdmin")]`

This prevents standard staff/client users from calling renewal operations.

### 2) Server-Side Renewal Validation
Renewal rules are enforced in service code, not in the UI:

- Service: `Services/Implementations/LicenseService.cs`
- `RenewAsync(...)` validates terms strictly to `1`, `2`, or `3` years.
- Any other term throws an exception and is rejected.
- Every successful renewal is written to `LicenseAuditLogs`.

### 3) Runtime Access Enforcement in Middleware
Every authenticated request is evaluated against license state:

- Middleware: `Extensions/LicenseEnforcementMiddleware.cs`
- Service check: `LicenseService.ShouldRestrictAccessAsync(...)`

If license is expired, restricted users are redirected to:
- `/License/Expired`

Exemptions are applied server-side for:
- `SuperAdmin`
- `Patient`
- `PatientPortal` route path

### 4) No Client-Controlled Expiry Logic
Expiry calculations and state transitions are computed on the server:

- `DetermineState(...)` in `LicenseService`
- Snapshot generation in `GetCurrentSnapshotAsync()`

No trusted license decision is taken from browser input.

### 5) Reminder Automation Is Server-Side
Daily reminder processing runs in hosted background service:

- `Services/Implementations/LicenseReminderHostedService.cs`

This service triggers `SendReminderAsync(force: false, ...)` and records results in reminder/audit tables.

## Secure License Update Procedure (Recommended)
Use this process for production renewal.

1. Sign in using a `SuperAdmin` account.
2. Open the license management page (`/License/Index`).
3. Renew using only approved term options (1/2/3 years).
4. Confirm the success message in UI.
5. Verify audit entry in `LicenseAuditLogs` (performed user, old/new expiry, timestamp, IP if available).
6. Validate runtime behavior by testing a non-SuperAdmin account if license was previously expired.

## Why Clients Cannot Change Expiry (Practical Summary)
A client user cannot safely extend expiry by editing browser requests because:

- Renewal endpoint is role-protected for `SuperAdmin` only.
- Antiforgery validation is required on renewal POST.
- Term and renewal logic are validated in server service code.
- Enforcement middleware checks server state on each request.
- Renewal actions are auditable.

## Important Infrastructure Controls
Application-level controls are strong, but database and infrastructure controls must also be enforced:

1. Do not give clients direct SQL write access to licensing tables.
2. Use least-privilege SQL credentials for app and admin operations.
3. Restrict DB network access to trusted server/admin networks only.
4. Use HTTPS in production for all authenticated traffic.
5. Monitor `LicenseAuditLogs` for unexpected renewal patterns.

## If You Need a Signed License File Model Later
Current implementation is DB-backed. If you later require offline/distributable "license file" updates, implement:

- signed payload (vendor private key)
- server-side signature verification (public key)
- nonce or version check to prevent replay
- optional hardware/tenant binding

Until then, the current secure path is: `SuperAdmin` renewal through server APIs + DB audit trail + middleware enforcement.
