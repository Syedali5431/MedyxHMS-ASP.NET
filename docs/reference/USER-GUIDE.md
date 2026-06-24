# User Guide


> Last Updated: 2026-04-28
> Operational Baseline: production-ready — final UAT passed 2026-04-26; all 30 modules validated.
> References: docs/UAT-Evidence-2026-04-26.md, docs/Final-touches.md, docs/RECENT_CHANGES.md

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

### Bed Management

- Nurses and Admins use the Bed Management module to assign, release, and transfer patients between beds.
- View real-time bed availability per ward/room from the Bed Management dashboard.
- Bulk-add beds during ward setup; right-click context menu for rapid status changes.

### Birth & Death Records

- Receptionists and Admins register birth and death events via the Front Office / Birth & Death module.
- Each record links to the relevant patient and generates a numbered certificate.

### Ambulance

- Track ambulance dispatch and pickup from the Ambulance module.
- Receptionist and Admin roles can log and monitor ambulance requests.

### Messaging

- Internal messaging between staff is available to all authenticated staff roles via the Messaging module.
- Send direct messages to colleagues; view conversation threads.

### Inventory

- Manage medical supply stock levels, item categories, purchase orders, and usage logs via the Inventory module.
- Pharmacist and Admin roles have full inventory management access.

### Download Center

- Access authorized document downloads (reports, forms, export files) via the Download Center.
- Available to all staff roles; content is governed by Admin/SuperAdmin upload.

### Live Consultation

- Doctors can initiate and manage remote consultations via the Live Consultation module.
- Patients can join sessions from their Patient Portal.
- Sessions are scheduled through the Appointments module.

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