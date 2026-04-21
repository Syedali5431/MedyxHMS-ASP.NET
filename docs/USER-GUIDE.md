# User Guide


> Last Updated: 2026-04-22
> Operational Baseline: runtime stabilization complete, automated UAT technical gates passing.
> References: docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md, docs/UPDATED-PRD-2026-04-22.md, docs/UPDATED-TODO-LIST-2026-04-22.md

## Audience

Front-desk staff, clinicians, billing staff, and patient-facing operational users.

## Role-Specific Guides

- Detailed role-wise guides are available in [user guides/README.md](user%20guides/README.md).
- Use [user guides/TEST-READINESS-CHECKLIST.md](user%20guides/TEST-READINESS-CHECKLIST.md) for structured UAT.

## Core Daily Workflows

### Patients

- Create a new patient from the patient module.
- Search by name, patient ID, or contact details.
- Open the patient profile to review appointments, bills, and clinical history.

### Appointments

- Create or review appointments from the appointment module.
- Approve or manage public booking requests through the CMS/public booking admin flow.
- Reschedule or cancel when necessary.

### Billing

- Create bills from the billing module.
- Collect payments and confirm status updates.
- Generate receipts or export operational lists when required.

### Clinical Records

- Use OPD/IPD modules for visit/admission records.
- Use pathology, radiology, and prescription modules for diagnostics and pharmacy workflows.

## Patient Portal Summary

Patients can:

- review appointments
- review bills
- view selected medical records and test results
- update selected profile/settings areas

## Operational Rules

- Do not share accounts.
- Use export/download features only for authorized operational work.
- Report incorrect patient, billing, or appointment data immediately.

## Account and Access Updates (April 2026)

- Self-signup is now approval-based: newly registered accounts remain inactive until approved by Admin or SuperAdmin.
- Admin and SuperAdmin can process account requests from the Accounts Approval module.
- Account rejection now requires a reason and records it for traceability.
- New account creation requires a unique `user_name`; duplicate usernames are blocked.
- Identity records for newly created users use numeric user IDs.