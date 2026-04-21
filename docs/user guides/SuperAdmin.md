# SuperAdmin


> Last Updated: 2026-04-22
> Operational Baseline: runtime stabilization complete, automated UAT technical gates passing.
> References: docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md, docs/UPDATED-PRD-2026-04-22.md, docs/UPDATED-TODO-LIST-2026-04-22.md

## Primary Responsibilities
- License management, security oversight, and full system administration.
- Global user, role, feature, and report governance.

## Login and Dashboard
- Open the application and click Login.
- Select SuperAdmin role during sign-in when prompted.
- You are redirected to the administrative dashboard.

## Core Modules
- Dashboard, Users/Roles, Settings, License, Reports, Audit, CMS, Chatbot Admin.

## Accounts Approval and Activation
- Open Accounts Approval from the admin navigation.
- Review pending signup requests and approve or reject.
- Rejection requires a reason and is logged for auditability.
- Approved accounts are activated immediately.

## Password Governance
- SuperAdmin can reset passwords for all users, including SuperAdmin accounts.
- Use Password Management under account governance screens for reset operations.

## Identity Policy
- New accounts must include a unique `user_name`.
- Duplicate usernames are blocked during account creation and signup.
- Newly created identity records use numeric user IDs.

## License Workflow
- Go to License page.
- Upload signed MedyxHMS.lic generated from the MedyxHMS-Lic desktop tool.
- Review module entitlement matrix.
- Use Export CSV to download entitlement snapshot.

Detailed runbook:
- [MedyxHMS-Lic-Operator-Guide.md](MedyxHMS-Lic-Operator-Guide.md)

## Reports and Exports
- Open Reports module and generate required report.
- Export tabular data to CSV where available.
- Save report output to PDF from report export options.
- Edit report schedules/templates and remove generated reports as needed.

## Patient and Billing Operations
- Can add/edit patient records.
- Can review payments, receipts, bed assignment, and module activities.

## AI Assistant
- Use floating AI button at bottom-right.
- Ask for module navigation, billing, appointment, and process guidance.
