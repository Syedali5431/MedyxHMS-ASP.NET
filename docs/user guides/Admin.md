# User Guide — Admin

**Role:** Admin  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-06-24  

---

## Overview

The Admin role is responsible for the day-to-day operational administration of the hospital.

**Key responsibilities:**
- Patient and staff record management
- Appointment and scheduling oversight
- Billing, payments, and financial reconciliation
- Report generation and distribution
- Accounts approval for new staff registrations
- CMS and public website content management
- Audit log review (sidebar → Audit Logs)

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. **If using default password "Medyx147":** you will be redirected to change your password immediately.
4. Select the **Admin** role when prompted.
5. Click **Sign In**. You are redirected to `/Dashboard`.

### Sidebar & Profile
- Click ☰ in the navbar to collapse/expand the sidebar (state persists).
- Upload a profile picture via **Profile** in the navbar (JPG/PNG, max 2MB).
- Optionally enable **MFA** on your Profile page for extra security.
- **Audit Logs** are available in the Admin sidebar section.

---

## 2. Dashboard

The Admin dashboard displays operational KPIs:

| Widget | Description |
|--------|-------------|
| Total Patients | Registered patients count |
| Today's Appointments | Appointments booked for today |
| Pending Approvals | Staff accounts awaiting your review |
| Active IPD Beds | Currently occupied beds |
| Recent Activity | Latest operational events |
| Module Quick Links | Shortcuts to most-used modules |

---

## 3. Accounts Approval

When staff members register via the signup form, they are placed in a **Pending** state. The Admin processes these:

1. Navigate to **Accounts Approval** in the navigation.
2. Review the pending list: name, email, requested role, registration date.
3. Click **Approve** — the account activates immediately and the user can log in.
4. Click **Reject** — a **rejection reason is mandatory**. The reason is stored in the audit trail and the user is notified.

> Admin cannot approve or manage **SuperAdmin** accounts. Those are handled by SuperAdmin only.

---

## 4. Password Management

- Admin can reset passwords for all users **except SuperAdmin**.
- Navigate to the user's profile under **Staff** and click **Reset Password**.
- Enter and confirm the new temporary password.
- Optionally require the user to change it on next login.

---

## 5. Patient Management

Navigate to **Patients** from the navigation.

### Registering a New Patient
1. Click **Add New Patient**.
2. Fill in: Full Name, Date of Birth, Gender, Contact Number, Email, Address, Blood Group, Emergency Contact, Insurance details (if applicable).
3. A unique `user_name` is required. The system blocks duplicates.
4. A numeric Patient ID is auto-assigned.
5. Click **Save**.

### Searching Patients
- Use the search bar at the top of the patient list.
- Filter by name, patient ID, phone number, or registration date.
- Click a patient row to open the full profile.

### Patient Profile
The patient profile shows:
- **Demographics** — personal and contact details
- **Appointments** — all past and upcoming appointments
- **OPD Encounters** — outpatient visit history
- **IPD Admissions** — inpatient stay history
- **Bills** — all bills and payment status
- **Prescriptions** — issued prescriptions
- **Lab Results** — pathology test results
- **Radiology Reports** — radiology results
- **Documents** — uploaded clinical documents

### Editing Patient Details
1. Open the patient profile and click **Edit**.
2. Modify the required fields.
3. Click **Save Changes**. All changes are audit-logged.

### Custom Fields
Custom fields added in **Settings → Custom Fields** appear on the patient form.

---

## 6. Appointment Management

Navigate to **Appointments** from the navigation.

### Creating an Appointment
1. Click **New Appointment**.
2. Select the patient (search by name or ID).
3. Select the doctor.
4. Choose the date and time slot.
5. Add notes if required.
6. Click **Save**.

### Appointment Lifecycle
| Status | Meaning |
|--------|---------|
| Pending | Booked, awaiting doctor confirmation |
| Confirmed | Approved by doctor or admin |
| Completed | Patient has been seen |
| Cancelled | Cancelled before visit |
| Rescheduled | Moved to a new date/time |

### Approving / Rejecting Appointments
1. Find the appointment in the list.
2. Click **Approve** to confirm or **Reject** with an optional reason.
3. Patient receives an email + SMS notification upon approval.

### Rescheduling
1. Open the appointment and click **Reschedule**.
2. Select the new date and time.
3. Save. The patient is notified automatically.

### Public Booking Requests
Appointments submitted through the public website appear under **CMS → Appointment Requests**.
- Review, approve, or reject each request.
- Duplicate requests (same patient, doctor, date, time) are flagged for review.
- Approval triggers an email + SMS confirmation to the patient.

---

## 7. OPD — Outpatient Department

Navigate to **OPD** from the navigation.

### Creating an OPD Encounter
1. Click **New OPD Encounter**.
2. Select the patient and attending doctor.
3. Fill in consultation details:
   - **Chief Complaint / Symptoms**
   - **Examination Findings**
   - **Diagnosis**
   - **Treatment Plan**
   - **Prescription** (or link to a prescription)
   - **Notes**
4. Save. The system automatically generates a consultation bill.

### Viewing OPD History
- Open a patient's profile and click the **OPD** tab to see all past encounters.
- Each encounter shows date, doctor, diagnosis, and bill status.

---

## 8. IPD — Inpatient Department

Navigate to **IPD** from the navigation.

### Admitting a Patient
1. Click **New Admission**.
2. Select the patient and admitting doctor.
3. Assign a **Ward** and **Bed** from the available list.
4. Enter the admission date, diagnosis, and initial notes.
5. Click **Admit**.

### Daily Charges
- Daily bed and ward charges accumulate automatically from admission date.
- Additional charges (procedures, medicines, tests) are added to the IPD bill as they occur.

### Discharging a Patient
1. Open the IPD admission and click **Discharge**.
2. Enter the discharge date, final diagnosis, and discharge summary.
3. The system automatically compiles all accumulated charges into a final bill.
4. Click **Confirm Discharge**.

---

## 9. Billing

Navigate to **Billing** from the navigation.

### Creating a Bill
1. Click **New Bill**.
2. Select the patient.
3. Link to an appointment, OPD encounter, or IPD admission (optional).
4. Add line items: service name, quantity, unit price.
5. Apply discounts or insurance adjustments if applicable.
6. Click **Save Bill**.

### Processing a Payment
1. Open an unpaid bill and click **Pay**.
2. Select the payment method: **Cash**, **Card**, **Cheque**, **Online**, or **Insurance**.
3. Enter the amount received.
4. Click **Process Payment**. The bill status updates to **Paid** or **Partially Paid**.

### Generating Receipts
- After a payment is recorded, click **Print Receipt** to generate a PDF receipt.
- Receipts include: hospital details, patient name, bill items, amount paid, and payment method.

### Exporting Billing Data
- From the billing list, click **Export CSV** or **Export PDF** to download the current filtered list.

### TPA (Third-Party Administration) Bills
- Select **TPA** as the payer for insurance-linked bills.
- Attach the TPA reference number.
- TPA bills appear in the TPA Management module for reconciliation.

---

## 10. Pharmacy & Prescriptions

Navigate to **Prescriptions** from the navigation.

### Viewing Prescriptions
- The prescription list shows all prescriptions issued by doctors.
- Filter by patient, doctor, or date.

### Pharmacy Fulfillment
- Pharmacists mark prescriptions as **Dispensed** after providing medicines.
- Admins can view fulfillment status and billing.

### Stock Management
- Navigate to **Pharmacy Settings** to manage the medicine catalogue.
- Low-stock and expiry notifications appear in the notification panel.

---

## 11. Staff Management

Navigate to **Staff** from the navigation.

### Adding a New Staff Member
1. Click **Add New Staff**.
2. Enter: Full Name, Email, `user_name` (unique), Role, Department, Designation.
3. Set a temporary password.
4. Click **Save**. Account is active immediately.

### Editing Staff
1. Find the staff member and click **Edit**.
2. Modify fields as needed and save.

### Attendance
Navigate to **Attendance**:
- View daily attendance records.
- Mark manual check-in / check-out for staff.
- Filter by date range and department.
- Export attendance reports as CSV.

### Leave Management
Navigate to **Leave**:
- View leave requests submitted by staff.
- Approve or reject requests.
- Each rejection requires a reason.
- View leave balance per employee.

### Payroll
Navigate to **Payroll**:
- View the payroll calendar.
- Process monthly payroll runs.
- Generate payslips for individual employees.
- Export payroll summary reports.

---

## 12. Reports

Navigate to **Reports** from the navigation.

| Report | Description |
|--------|-------------|
| Department Report | Activity and staffing per department |
| Financial Report | Revenue by department and time period |
| Occupancy Report | Bed/ward utilization |
| Staff Report | Attendance analytics |
| Payroll Report | Payroll processing summary |

### Running a Report
1. Select the report type.
2. Set the date range and any applicable filters.
3. Click **Generate**.
4. Export as **CSV** or **PDF** from the report toolbar.

### Scheduled Reports
1. Open any report template and click **Schedule**.
2. Set frequency and recipient email addresses.
3. The report is automatically generated and emailed on schedule.
4. View and manage schedules under **Report Schedules**.

---

## 13. CMS & Public Website

Navigate to **CMS** from the navigation.

- **Pages** — Add, edit, and publish public web pages with style control and responsive preview.
- **Menu** — Manage public site navigation links.
- **Notices** — Post announcements displayed on the public site.
- **Appointment Requests** — Review and action public booking form submissions.
- **Notification Settings** — Configure SMTP and SMS, send test messages, view delivery logs.

---

## 14. Certificates & ID Cards

Navigate to **Certificates** from the navigation.

- **Generate Certificate** — Select certificate type (birth, death, medical, etc.), patient, and print.
- **Patient ID Card** — Generate and print a patient ID card with auto-assigned ID number.
- **Staff ID Card** — Generate and print staff ID cards.

---

## 15. Front Office

Navigate to **Front Office** from the navigation.

| Feature | Description |
|---------|-------------|
| Visitor Registration | Log visitor name, contact, purpose, and host |
| Complaints | Record and track patient/visitor complaints |
| Dispatch | Log outgoing items/documents |
| Receive | Log incoming items/documents |
| Birth Records | Register birth events |
| Death Records | Register death events |
| Ambulance | Log ambulance dispatch and return |

---

## 16. AI Assistant

The floating AI button (bottom-right) provides in-app guidance:
- Ask questions about module navigation, billing workflows, or reporting.
- The chatbot has read access to approved help content.
- If it cannot answer, it will offer to escalate to a support contact.

---

## 17. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**.
3. You are redirected to the login page. Your session is fully cleared.

---

## Quick Reference

| Task | Where |
|------|-------|
| Approve pending staff | Accounts Approval |
| Register a patient | Patients → Add New Patient |
| Book an appointment | Appointments → New Appointment |
| Create a bill | Billing → New Bill |
| Process a payment | Billing → Open Bill → Pay |
| Generate a report | Reports |
| Add a public notice | CMS → Notices |
| Review public booking requests | CMS → Appointment Requests |
| Manage leave requests | Leave |
| Run payroll | Payroll |
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
