# User Guide — SuperAdmin

**Role:** SuperAdmin  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-04-28  

---

## Overview

The SuperAdmin is the highest-privilege user in Medyx HMS. This role has unrestricted access to every module, every user record, and every system configuration. There is typically only one SuperAdmin account per installation. The SuperAdmin is also the only user exempt from license expiry enforcement, meaning the system remains accessible even after the license lapses.

**Key responsibilities:**
- System-wide configuration and governance
- User account management and role assignment
- License management and module entitlement control
- Security oversight and audit review
- Module enable/disable governance on the dashboard
- Chatbot, CMS, and notification infrastructure management

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email address and password.
3. A role selection screen appears — select **SuperAdmin**.
4. Click **Sign In**. You are redirected to `/Dashboard`.

> If you see the PatientPortal instead, your browser may have cached a Patient session. Log out fully and log back in selecting SuperAdmin.

### Session Behaviour
- Your active role is stored in the session as `ActiveRole = SuperAdmin`.
- Navigating to `/PatientPortal/` will be blocked with an Access Denied response.
- Your session expires after the configured idle timeout.

---

## 2. Dashboard

The SuperAdmin dashboard shows system-wide KPIs:

| Widget | Description |
|--------|-------------|
| Total Patients | Count of all registered patients |
| Today's Appointments | Appointments scheduled for today |
| Pending Approvals | Staff accounts awaiting activation |
| Active Beds | Currently occupied IPD beds |
| License Status | Current expiry date and license health |
| Recent Activity | Last 10 audit events across the system |

### Module Enablement Panel
A SuperAdmin-exclusive section on the dashboard lets you toggle system-wide module availability:
1. Click **Manage Modules** in the dashboard governance panel.
2. Use the toggles to enable or disable individual modules globally.
3. Disabled modules are hidden from all non-SuperAdmin users immediately.
4. You retain access to all modules regardless of their toggle state.

---

## 3. User & Account Management

### 3.1 Viewing All Users
Navigate to **Staff → All Staff** (or equivalent user list screen).  
You can filter by role, status (active/inactive/pending), and search by name or email.

### 3.2 Creating a New User
1. Click **Add New Staff**.
2. Fill in: Full Name, Email, `user_name` (must be unique), Role(s), and a temporary password.
3. Numeric user IDs are assigned automatically — do not enter manually.
4. Click **Save**. The account is immediately active when created by SuperAdmin.

> **Username Policy:** Every account requires a unique `user_name`. The system blocks duplicate usernames at creation time. This applies to all creation paths including signup, patient registration, and admin-created users.

### 3.3 Editing a User
1. Find the user in the Staff list and click **Edit**.
2. Modify name, email, role assignments, or status.
3. Click **Save Changes**.

### 3.4 Accounts Approval Workflow
New staff who register themselves via the signup form are placed in a **Pending** state. To process them:
1. Navigate to **Accounts Approval** in the admin navigation.
2. The list shows all pending registrations with name, email, requested role, and registration date.
3. Click **Approve** to activate the account immediately.
4. Click **Reject** to deny — a reject **reason is mandatory** and is recorded in the audit trail.
5. Rejected users receive a notification and cannot log in.

### 3.5 Password Management
- SuperAdmin can reset passwords for **all users**, including other SuperAdmin accounts.
- Go to the user's profile and click **Reset Password**.
- Enter the new password (or generate one) and confirm.
- The user is required to change the password on next login (if configured).

> **Role Boundary:** Admin role cannot reset SuperAdmin account passwords. Only SuperAdmin can reset SuperAdmin credentials.

---

## 4. Role & Permission Management

Medyx HMS uses Role-Based Access Control (RBAC). Roles are assigned per user and can be stacked where applicable.

### Available Roles
| Role | Primary Area |
|------|-------------|
| SuperAdmin | Full system |
| Admin | Hospital operations |
| Doctor | OPD, IPD, prescriptions |
| Nurse | IPD nursing |
| Pharmacist | Pharmacy |
| Accountant | Billing |
| Receptionist | Front Office |
| LabTechnician | Laboratory |
| Radiologist | Radiology |
| Staff | General access |
| Patient | Patient Portal only |

### Assigning Roles
1. Open the staff member's edit screen.
2. Under **Roles**, check the appropriate role(s).
3. Save changes. The new role takes effect at the user's next login.

---

## 5. License Management

See also: [MedyxHMS-LIC.md](MedyxHMS-LIC.md) for a complete technical explanation.

### 5.1 Viewing License Status
1. Navigate to **License** in the top navigation.
2. The license screen shows:
   - License ID (GUID)
   - Tenant ID
   - Issue date and expiry date
   - Days remaining
   - Maximum concurrent users
   - Module entitlement matrix (Licensed / Locked per module)
   - Renewal history

### 5.2 Uploading a New License
1. Obtain a new `MedyxHMS.lic` file from the MedyxHMS-Lic vendor tool (see [MedyxHMS-LIC.md](MedyxHMS-LIC.md)).
2. On the License screen, paste the RSA public key values into the upload key fields:
   - Public Key Modulus (hex)
   - Public Key Exponent (hex)
   - Verification Key (fingerprint)
3. Click **Apply Key Configuration**.
4. Use the **Upload License** action and select `MedyxHMS.lic`.
5. The system verifies the RSA-SHA256 signature. On success, the new expiry date and module entitlements take effect immediately.
6. The public key fields are cleared after a successful upload (one-time key consumption).

### 5.3 License Renewal
- Only SuperAdmin can renew (extend) the license expiry.
- Renewal options: **+1 year**, **+2 years**, **+3 years** (no other values accepted).
- Every renewal is logged to the audit trail with operator identity and timestamp.
- A new `.lic` file covering the extended period must be provided for each renewal.

### 5.4 Pre-Expiry Reminders
- The system sends an automated email to all users **5 days before expiry** as a reminder.
- The reminder email instructs staff to contact the SuperAdmin for renewal.
- Duplicate reminders within the same expiry cycle are automatically suppressed.
- SuperAdmin can trigger a manual resend from the License screen.

### 5.5 License Expiry Behaviour
| User Type | Effect When License Expires |
|-----------|---------------------------|
| SuperAdmin | **No restriction.** Full access retained. |
| Patient | **No restriction.** Patient Portal remains accessible. |
| All other staff roles | Blocked from staff modules; redirected to a license-expired information screen. |

### 5.6 Exporting the Entitlement Matrix
- Click **Export CSV** on the License screen to download a snapshot of all module entitlements.
- Useful for record-keeping and compliance evidence.

### 5.7 Full Module Catalog (30 modules)

The following module keys are available in `MedyxHMS-Lic` when generating a license. All 30 are licensable; the 8 marked as **Initial Package** are always included by the vendor tool:

| Module Key | Display Name | Initial Package |
|------------|--------------|-----------------|
| Dashboard | Dashboard | Yes |
| Patient | Patient Management | Yes |
| Appointment | Appointments | Yes |
| Billing | Billing | Yes |
| FrontOffice | Front Office | Yes |
| Referral | Referrals | Yes |
| Report | Reports | Yes |
| PatientPortal | Patient Portal | Yes |
| OPD | Outpatient Department | — |
| IPD | Inpatient Department | — |
| Prescription | Prescriptions / Pharmacy | — |
| Lab | Pathology / Laboratory | — |
| Radiology | Radiology | — |
| BloodBank | Blood Bank | — |
| OperationTheatre | Operation Theatre | — |
| Attendance | Attendance | — |
| Leave | Leave Management | — |
| Payroll | Payroll | — |
| Certificate | Certificates | — |
| Ambulance | Ambulance | — |
| Chatbot | AI Chatbot | — |
| CMS | Content Management | — |
| License | License Management | — |
| BirthDeath | Birth & Death Records | — |
| TPA | Third-Party Admin | — |
| Messaging | Internal Messaging | — |
| Inventory | Inventory Management | — |
| DownloadCenter | Download Center | — |
| LiveConsultation | Live Consultation | — |
| BedManagement | Bed Management | — |

---

## 6. Module Management

Navigate to **Module Management** to configure per-user module access:

1. View the global module enable/disable status.
2. Select a user to configure their individual module access (override defaults).
3. The **effective** access for any user = global status + per-user override.
4. All changes are logged to the audit trail.

---

## 7. Reports

Navigate to **Reports** from the main navigation.

### Available Report Types
| Report | Description |
|--------|-------------|
| Department Report | Staff count and activity per department |
| Financial Report | Revenue breakdown by department and period |
| Occupancy Report | Bed/ward utilization rates |
| Staff Report | Staff attendance analytics |
| Payroll Report | Payroll processing summary |

### Report Builder
1. Click **Builder** to access the custom report designer.
2. Create a new template: choose data source, select fields, apply filters.
3. Click **Save Template**. Templates appear on the report list for scheduling.
4. Use **Clone** to duplicate an existing template.
5. Use **Design** to modify the layout of an existing template.

### Scheduling Reports
1. Open a report template and click **Schedule**.
2. Set frequency (daily / weekly / monthly), recipient email addresses, and output format (CSV / PDF).
3. Save the schedule. The Quartz.NET background job will generate and email the report automatically.
4. View scheduled reports under **Report Schedules**.
5. Delete schedules from the same screen.

### Exporting
- Every report table can be exported as **CSV** or **PDF** from the export toolbar.
- Click **Preview** to view the report output before exporting.

---

## 8. Audit Trail

Navigate to **Audit** from the navigation.

- View all sensitive operation logs: login events, billing changes, patient record edits, license actions, chatbot events.
- Filter by: **Date Range**, **Entity Type**, **User**, or **Action**.
- Click any audit entry for full details.
- The **User Actions** tab shows a per-user chronological log with status badges.
- All audit records are immutable — they cannot be deleted through the UI.

---

## 9. CMS & Public Website

Navigate to **CMS** (Content Management System).

### Pages
1. Click **Pages → Add New Page** to create a public-facing web page.
2. Enter title, slug (URL), body content, and publish status.
3. Use the **Style Controls** panel to set font, colour, and layout.
4. Toggle the **Responsive Preview** to verify mobile layout.

### Navigation Menu
1. Click **Menu Items → Add Menu Item**.
2. Link to a CMS page, external URL, or system route.
3. Drag to reorder menu items.

### Notices
1. Click **Notices → Add Notice** to post hospital announcements.
2. Notices appear on the public site automatically.

### Notification Settings
1. Click **Notification Settings** in the CMS section.
2. Configure SMTP (host, port, credentials) and SMS provider (Twilio / Africa's Talking) credentials.
3. Use **Send Test Email** and **Send Test SMS** to verify delivery.
4. The last test status panel shows the most recent delivery result.
5. Manage email and SMS **opt-out lists** to suppress recipients who have unsubscribed.

### Appointment Request Management
1. Click **Appointment Requests** to see all public booking form submissions.
2. Approve or reject requests with an optional note.
3. Approved requests trigger an email + SMS confirmation to the patient.
4. Use **Duplicate Review** to identify and manage overlapping booking requests.

---

## 10. Chatbot Administration

Navigate to **Chatbot Admin**.

### Settings
- Enable or disable the chatbot globally, or by role group (patients / staff / admin).
- Set the OpenAI model, temperature, and maximum token limit.
- Edit the system prompt used to shape chatbot behaviour.
- Configure per-user usage limits (hourly query cap).

### Analytics
- View total sessions, questions asked, satisfaction scores, escalation rate, and unresolved conversation rate.
- Use the analytics dashboard to identify common unresolved topics and improve the knowledge base.

### Escalations
- The **Escalations** tab lists all chatbot sessions that were escalated to a human agent.
- Click **Resolve** next to any escalation to mark it handled.
- Add a resolution note before closing.

---

## 11. Settings

Navigate to **Settings** from the navigation.

| Setting Group | What You Can Configure |
|---------------|------------------------|
| Hospital Profile | Hospital name, address, logo, contact details |
| Branding & Theme | Colour scheme, sidebar theme |
| Regional | Time zone, date format, currency symbol |
| Language | Default system language |
| Email | SMTP host, port, TLS, sender address |
| SMS | Provider selection (Twilio / Africa's Talking), credentials |
| Security | Session timeout, password policy, rate limit thresholds |
| Backup | Database backup schedule and storage path |

---

## 12. AI Assistant (Chatbot)

The floating AI button (bottom-right corner) is available on all admin pages.

- Type a question in natural language (e.g., "How do I generate a payroll report?" or "Where is the blood bank module?").
- The chatbot uses the approved CMS/help content and system knowledge to answer.
- If the chatbot cannot answer confidently, it offers escalation to a support agent.
- All chatbot sessions are logged. SuperAdmin sessions are not subject to usage quotas.

---

## 13. Logging Out

- Click your name/avatar in the top-right navigation bar.
- Click **Logout**.
- You are redirected to `/Account/Login`.
- All session data is cleared.

---

## Quick Reference

| Task | Where |
|------|-------|
| Approve pending user | Accounts Approval |
| Reset any password | Staff → Edit User → Reset Password |
| Upload license | License screen |
| Enable/disable module globally | Dashboard → Manage Modules or Module Management |
| Generate financial report | Reports → Financial Report |
| View audit logs | Audit |
| Configure SMTP/SMS | CMS → Notification Settings |
| Manage chatbot | Chatbot Admin → Settings |
| Edit public website pages | CMS → Pages |
