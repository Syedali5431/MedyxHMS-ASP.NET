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

## Feedback Collection

- collect issues by module and user role
- separate training issues from actual defects
- prioritize fixes by business impact and recurrence