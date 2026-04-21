# Training and Support Plan

## Purpose

This document covers the remaining Step 5.2 non-code operational tasks:

- user training sessions
- support escalation
- incident response procedures
- post-launch monitoring
- feedback collection

## Training Plan

### Session 1: Front Desk and Appointments

- patient registration/search
- appointment create/update/cancel
- public booking approval review

### Session 2: Billing and Receipts

- bill creation
- payment collection
- receipt/download/export workflows

### Session 3: Clinical Operations

- OPD/IPD workflow review
- prescription, pathology, radiology, pharmacy basics

### Session 4: Admin and Support

- audit logs
- notification delivery logs
- deployment/rollback awareness
- incident reporting path

### Session 5: Account Governance (Admin/SuperAdmin)

- Accounts Approval workflow for signup requests
- approve vs reject decision path
- mandatory reject reason behavior
- password reset authority boundaries by role
- duplicate `user_name` handling during account creation

### Session 6: License Operations (SuperAdmin)

- run MedyxHMS-Lic tool and generate key pair
- generate signed MedyxHMS.lic with module entitlements
- upload/import license into ASP.NET license screen
- verify entitlement matrix and export CSV snapshot
- understand one-time verification key consumption policy

Reference guide:
- [user guides/MedyxHMS-Lic-Operator-Guide.md](user%20guides/MedyxHMS-Lic-Operator-Guide.md)

## Support Escalation Model

1. First line: operational user or department lead
2. Second line: application administrator/SuperAdmin
3. Third line: engineering/deployment owner for code, database, or migration issues

## Incident Response Procedure

1. Record time, user impact, and affected module.
2. Classify severity: login, patient safety workflow, billing, reporting, or non-critical admin issue.
3. Contain the issue if possible.
4. Capture logs, screenshots, route, and data identifiers.
5. Escalate according to severity.
6. Document resolution and any rollback performed.

## Post-Launch Monitoring

- monitor startup and runtime logs
- monitor failed notification deliveries
- monitor appointment creation and payment processing success
- monitor export/report failures
- monitor account approval backlog and rejection trends
- monitor license import/verification failures and entitlement lock events

## Feedback Collection

- collect issues by module and user role
- separate training issues from actual defects
- prioritize fixes by business impact and recurrence