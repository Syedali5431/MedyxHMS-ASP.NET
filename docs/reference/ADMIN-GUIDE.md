# Admin Guide


> Last Updated: 2026-06-24
> Operational Baseline: all 5 UI/UX phases complete; MFA with recovery codes; forced password change on default password.
> References: [../planning/UI-ENHANCEMENT-ROADMAP.md](../planning/UI-ENHANCEMENT-ROADMAP.md), [../testing/UAT.md](../testing/UAT.md)

## Audience

Administrators, SuperAdmin users, and system operators.

## Responsibilities

- maintain configuration and provider settings
- review audit logs and notification delivery logs
- manage public booking approval flow
- verify backup, deployment, and validation procedures are followed
- manage account approval queue (approve/reject with reason)
- enforce password governance boundaries (Admin vs SuperAdmin target restrictions)
- coordinate SuperAdmin-driven license file upload workflow

## Admin Validation Checklist

- Confirm database connectivity after deployment.
- Confirm startup seeding completed without errors.
- Confirm staff roles/permissions exist as expected.
- Confirm key modules load: patients, appointments, billing, CMS, diagnostics.
- Confirm exports and PDF downloads work for authorized users.
- Confirm pending signup requests appear in Accounts Approval.
- Confirm rejected requests require a reason and are tracked.
- Confirm username uniqueness validation blocks duplicate `user_name` values.

## Account Governance Operations

- Use Accounts Approval to process signup requests.
- Approval activates accounts immediately.
- Rejection requires a reason and is audit logged.
- Password resets must follow role boundaries:
	- Admin can reset non-SuperAdmin users.
	- SuperAdmin can reset all users.

## Licensing Operations

- Only SuperAdmin uploads/updates license files.
- Use the license operator guide for MedyxHMS-Lic usage and upload sequence:
	- [user guides/MedyxHMS-Lic-Operator-Guide.md](user%20guides/MedyxHMS-Lic-Operator-Guide.md)

## Audit and Support Expectations

- Review audit activity for create/update/delete paths after go-live.
- Review failed notification sends and investigate provider configuration.
- Escalate data-integrity mismatches immediately if seen after migration.

## Module Access Reference (Admin Role)

The Admin role has access to the following modules (all subject to license entitlement):

| Module | Access Level |
|--------|--------------|
| Dashboard | Full |
| Patient Management | Full |
| Appointments | Full |
| OPD | Full |
| IPD | Full |
| Billing | Full |
| Pharmacy / Prescriptions | Full |
| Lab (Pathology) | Full |
| Radiology | Full |
| Blood Bank | Full |
| Operation Theatre | Full |
| Front Office | Full |
| Bed Management | Full — assign, release, transfer, bulk add |
| Attendance | Full |
| Leave | Full |
| Payroll | Full |
| Certificates | Full |
| Referrals | Full |
| Birth & Death Records | Full |
| Ambulance | Full |
| TPA (Third-Party Admin) | Full |
| Messaging | Full |
| Inventory | Full |
| Download Center | Full |
| Live Consultation | Full |
| Reports | Full |
| CMS | Full |
| Accounts Approval | Full |
| Audit | Read-only |
| License | No access (SuperAdmin only) |
| Module Management | No access (SuperAdmin only) |
| Chatbot Admin | No access (SuperAdmin only) |

## Change Management

- Use controlled deployment windows.
- Take a backup before production database changes.
- Record rollback decisions and incident timelines.