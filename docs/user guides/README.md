# MedyxHMS Role-Based User Guides

This folder contains user guides by role for the ASP.NET MedyxHMS application.

## Roles Covered
- SuperAdmin
- Admin
- Doctor
- Nurse
- Receptionist
- Accountant/Staff Billing
- Pharmacist
- LabTechnician
- Radiologist
- Patient

## Additional Operational Guide
- [MedyxHMS-Lic-Operator-Guide.md](MedyxHMS-Lic-Operator-Guide.md)

## Common Notes
- Use the Login button at the top-right on public/app pages.
- After login, users are redirected to a role-appropriate dashboard/module.
- CSV/PDF export options are available on report/listing pages where enabled.
- Report editing and schedule/delete operations are restricted to Admin and SuperAdmin.
- AI Assistant can be opened using the floating AI button on the bottom-right.
- New self-signup requests are approval-gated; account access starts only after Admin or SuperAdmin approval.
- Account creation now requires a unique `user_name`; duplicates are blocked at validation.
- New accounts use numeric user IDs in system identity records.
