# Licensing Feature Technical Design

**Status:** Implemented | **Target Phase:** Phase 6 | **Date:** April 2026

## Objective

Add a secure, auditable licensing system that:

- enforces expiry for staff/admin users only
- does not block `SuperAdmin` users
- does not block patient users
- sends a predefined renewal reminder email to all users 5 days before expiry
- allows only `SuperAdmin` users to renew the license for 1, 2, or 3 years

The design must keep all enforcement server-side and avoid any client-controlled license state.

## April 21, 2026 Security Update

- Added a deterministic Verification Key fingerprint generated from RSA public key components in the vendor tool.
- Each generated .lic now embeds VerificationKey inside the signed payload.
- ASP.NET license import rejects any license where payload VerificationKey does not match the configured key fingerprint.
- ASP.NET runtime validation also verifies the active license record is bound to the currently configured verification key.
- Outcome: editing license content outside MedyxHMS-Lic invalidates signature or verification-key binding and is rejected.

### One-Time Key Consumption Policy

- A Verification Key can be used for only one successful license upload.
- After successful upload, ASP.NET clears upload-key settings (`LicensePublicKeyModulusHex`, `LicensePublicKeyExponentHex`, `LicenseVerificationKey`).
- Future uploads with the same Verification Key are rejected.
- Runtime verification stays stable because the active license record stores the public key used for that imported license.

### Unique-Key Availability

- MedyxHMS-Lic now generates 256-bit random key identities and unique output key files per generation.
- Practical key-space exhaustion is infeasible; operators can generate fresh keys indefinitely.

### Module Entitlement Licensing

- MedyxHMS-Lic now prompts a module checklist during license creation.
- Selected module keys are embedded as `LicensedModules` inside the signed payload and covered by signature validation.
- Runtime middleware enforces module entitlements for authenticated non-SuperAdmin users.
- If a module is not licensed, users are redirected to a locked-feature page with purchase guidance.
- SuperAdmin users remain exempt and can still access locked modules for administration.

### Encoded License Data At Rest

- Imported `.lic` content and signature/canonical payload values are now protected before DB storage.
- `LicenseRecords` stores encoded-at-rest values (`EncodedLicenseFile`, protected `SignatureHex`, protected `CanonicalPayloadJson`) to reduce license-pattern exposure in raw DB reads.
- Runtime verification transparently unprotects values and preserves backward compatibility with older plain records.

## Implementation Notes

The current implementation applies these product decisions:

- renewal extends from the current UTC date when the previous license is already expired
- reminder recipients are all active users with valid email addresses
- patient exemption is enforced through `PatientPortal` route bypass and `Patient` role bypass when present
- no post-expiry grace period is applied

---

## Product Rules

### Core Business Rules

1. The application has one authoritative active license record for the installation/tenant.
2. License expiry affects staff/admin users except `SuperAdmin`.
3. License expiry does not affect patient users.
4. Only `SuperAdmin` users can update license expiry.
5. Renewal duration must be limited to exactly:
   - 1 year
   - 2 years
   - 3 years
6. A predefined reminder email must be sent to all users 5 days before expiry.
7. Reminder content must instruct users to make payment and contact a `SuperAdmin` user to update the expiry.

### Operational Rules

- Reminder jobs must not send duplicates for the same expiry cycle.
- Every renewal and reminder action must be audited.
- SuperAdmin users must retain access to the renewal screen even after expiry.
- Expired-license messaging must be consistent and actionable for blocked users.

---

## Recommended Architecture

### 1. License Domain Service

Create a dedicated service, for example `ILicenseService`, responsible for:

- reading the current active license
- evaluating license status (`Active`, `ExpiringSoon`, `Expired`)
- validating renewal requests
- applying renewal duration rules
- writing audit records
- exposing reminder/job helpers

This keeps license logic out of controllers and middleware.

### 2. License Enforcement Layer

Add a dedicated middleware or authorization filter that runs after authentication and before protected business controllers execute.

Recommended behavior:

- if user is unauthenticated: continue to normal auth flow
- if user is `SuperAdmin`: bypass license restriction
- if user is patient: bypass license restriction
- if license is active: allow request
- if license is expired and user is a restricted staff/admin role: redirect to license-expired page

This approach centralizes enforcement and avoids scattering checks across controllers.

### 3. Scheduled Reminder Job

Use a scheduled background job to run daily and evaluate:

- current expiry date
- days remaining
- whether the 5-day reminder has already been sent for the current license cycle

Recommended implementation path:

- background hosted service or scheduled job framework
- idempotent reminder evaluation
- delivery/audit logging for each send attempt

---

## Data Model Proposal

### LicenseRecord

Suggested fields:

- `Id`
- `LicenseKey` or `LicenseReference`
- `IssuedAtUtc`
- `ExpiresAtUtc`
- `Status`
- `LastReminderSentAtUtc`
- `LastReminderCycleExpiryUtc`
- `RenewedByUserId`
- `RenewedAtUtc`
- `RenewalTermYears`
- `Notes`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### LicenseAuditLog

Suggested fields:

- `Id`
- `LicenseRecordId`
- `ActionType`
- `PerformedByUserId`
- `PerformedAtUtc`
- `OldExpiresAtUtc`
- `NewExpiresAtUtc`
- `RenewalTermYears`
- `Details`
- `IpAddress`

### LicenseReminderLog

Suggested fields:

- `Id`
- `LicenseRecordId`
- `ReminderType`
- `TargetExpiryUtc`
- `TriggeredAtUtc`
- `SentToCount`
- `Status`
- `ErrorMessage`

This split keeps commercial state, administrative audit, and notification history separate.

---

## Access Matrix

### Users Unaffected by Expiry

- `SuperAdmin`
- patients

### Users Affected by Expiry

- admin users other than `SuperAdmin`
- doctors
- nurses
- reception/staff roles
- other internal operational roles

### Renewal Permissions

- `SuperAdmin`: allowed
- all other roles: denied

---

## Renewal Workflow

### SuperAdmin Renewal Flow

1. SuperAdmin opens license management screen.
2. Screen shows current expiry, status, last reminder status, and renewal history.
3. SuperAdmin selects one of the allowed terms: 1, 2, or 3 years.
4. Server validates role and term.
5. License expiry is updated from the correct base date:
   - if license is active: extend from current expiry
   - if license is expired: extend from current UTC date or from expiry according to final product rule
6. Audit log entry is created.
7. Reminder state for the new cycle is reset.

### Renewal Validation Rules

- reject any term not in `{1, 2, 3}`
- reject non-SuperAdmin users
- reject invalid or missing active license state
- require CSRF protection and server-side confirmation handling

---

## Reminder Email Design

### Trigger Rule

Send reminder when:

- `ExpiresAtUtc.Date - UtcNow.Date == 5`
- and no reminder has been sent for that expiry cycle

### Recipients

Default requirement from product direction:

- all users

Implementation refinement recommended:

- all active users with valid email addresses
- optionally exclude system/service accounts if present

### Email Content Requirements

The predefined email should include:

- current license expiry date
- payment reminder wording
- instruction to contact a `SuperAdmin` user for renewal/update
- support or billing contact details if available

### Suggested Template Fields

- Hospital/App Name
- Expiry Date
- Days Remaining
- SuperAdmin Contact Guidance
- Billing Contact Details

---

## UI Surfaces

### License Management Screen

SuperAdmin-only page should show:

- current status badge
- expiry date
- days remaining
- renewal buttons for 1 / 2 / 3 years
- reminder history
- audit history summary
- manual resend reminder action

### Expired License Screen

Restricted staff/admin users should see:

- notice that the system license has expired
- instruction to contact a `SuperAdmin`
- minimal explanation of affected access

This screen must not reveal sensitive internal commercial data.

---

## Security Recommendations

- never trust license dates from the client
- keep license enforcement in middleware/service layer
- require authenticated identity and role validation for renewals
- log all renewal and reminder actions
- protect renewal actions with antiforgery tokens
- restrict reminder resend to SuperAdmin users
- avoid hard-coding billing contacts in code where settings are more appropriate

---

## Testing Strategy

### Unit Tests

- status evaluation for active/expiring/expired states
- permitted renewal terms only
- exemption logic for `SuperAdmin` and patients
- duplicate reminder prevention

### Integration Tests

- expired staff/admin access is blocked
- expired SuperAdmin access is allowed
- expired patient access is allowed
- reminder job sends exactly once at 5 days remaining
- renewal updates expiry and resets reminder cycle

### Operational Validation

- verify delivery logging for reminder email sends
- verify audit log entries for renewals and manual resend actions
- verify timezone/date-boundary behavior around midnight UTC

---

## Open Decisions

Before implementation, these product details should be finalized:

1. Should renewal extend from current expiry or current date when already expired?
2. Should all user accounts receive reminder mail, or only staff/admin accounts plus billing contacts?
3. Should the application support a hidden grace period after expiry?
4. Should license state be editable only through UI, or also via protected API/admin tooling?

---

## Recommended Implementation Order

1. Create domain entities and license service
2. Add SuperAdmin-only management page and renewal actions
3. Add middleware/filter enforcement
4. Add reminder scheduler and logs
5. Add tests and operational monitoring

---

**End of Licensing Design**