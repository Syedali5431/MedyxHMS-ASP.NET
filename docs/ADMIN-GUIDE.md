# Admin Guide

## Audience

Administrators, SuperAdmin users, and system operators.

## Responsibilities

- maintain configuration and provider settings
- review audit logs and notification delivery logs
- manage public booking approval flow
- verify backup, deployment, and validation procedures are followed

## Admin Validation Checklist

- Confirm database connectivity after deployment.
- Confirm startup seeding completed without errors.
- Confirm staff roles/permissions exist as expected.
- Confirm key modules load: patients, appointments, billing, CMS, diagnostics.
- Confirm exports and PDF downloads work for authorized users.

## Audit and Support Expectations

- Review audit activity for create/update/delete paths after go-live.
- Review failed notification sends and investigate provider configuration.
- Escalate data-integrity mismatches immediately if seen after migration.

## Change Management

- Use controlled deployment windows.
- Take a backup before production database changes.
- Record rollback decisions and incident timelines.