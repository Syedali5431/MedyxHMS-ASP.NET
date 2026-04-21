# Admin Guide


> Last Updated: 2026-04-22
> Operational Baseline: runtime stabilization complete, automated UAT technical gates passing.
> References: docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md, docs/UPDATED-PRD-2026-04-22.md, docs/UPDATED-TODO-LIST-2026-04-22.md

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

## Change Management

- Use controlled deployment windows.
- Take a backup before production database changes.
- Record rollback decisions and incident timelines.