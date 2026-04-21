# Admin


> Last Updated: 2026-04-22
> Operational Baseline: runtime stabilization complete, automated UAT technical gates passing.
> References: docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md, docs/UPDATED-PRD-2026-04-22.md, docs/UPDATED-TODO-LIST-2026-04-22.md

## Primary Responsibilities
- Day-to-day administration, operations, and report management.
- User supervision, settings, and cross-module coordination.

## Login and Dashboard
- Click Login and sign in with Admin role.
- You are redirected to the dashboard with operational KPIs.

## Core Modules
- Patient, Appointment, OPD/IPD, Billing, Pharmacy, Lab/Radiology, Reports, CMS.

## Accounts Approval and Activation
- Use Accounts Approval in admin navigation to process new signup requests.
- Approve requests to activate accounts.
- Reject requests with a mandatory reason.

## Password Governance
- Use Password Management to reset user passwords.
- Admin can reset non-SuperAdmin accounts.
- Admin cannot reset SuperAdmin account passwords.

## Identity Policy
- New accounts must include a unique `user_name`.
- Duplicate usernames are blocked during creation.
- Newly created users are assigned numeric user IDs in identity records.

## Patient Operations
- Add/edit patient details from Patient module.
- Track admission, bed assignment, and discharge workflow.

## Billing and Receipts
- Create bills and process payments.
- Generate and print payment receipts.
- Export bill/payment tables as CSV/PDF where available.

## Reports
- Generate financial, occupancy, and departmental reports.
- Export reports to CSV/PDF.
- Edit report schedules and delete generated/scheduled reports (Admin and SuperAdmin only).

## AI Assistant
- Use bottom-right AI launcher for in-app guidance.
