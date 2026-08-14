# User Acceptance Test (UAT) — MedyxHMS

> **Date:** 2026-06-24 | **Version:** 1.0 | **Status:** Ready for Testing

---

## Role-by-Role Acceptance Criteria

---

## SuperAdmin

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| SA-01 | Login with superadmin@hospital.com / Medyx147 | Forced password change | |
| SA-02 | Change password (8+ chars) | Redirected to Dashboard | |
| SA-03 | Toggle sidebar collapse/expand | Sidebar collapses to icons, persists on reload | |
| SA-04 | Upload profile picture (JPG ≤ 2MB) | Shows in navbar and Profile page | |
| SA-05 | Enable MFA from Profile page | QR code displayed, scannable by authenticator | |
| SA-06 | Logout, login with MFA | OTP required, login completes after verification | |
| SA-07 | Access all 30 modules | All sidebar items visible | |
| SA-08 | View Audit Logs (sidebar → Audit Logs) | Filterable list with color-coded badges | |
| SA-09 | Export audit logs to CSV | Valid CSV file downloads | |
| SA-10 | Manage modules (enable/disable) | Module Management functional | |
| SA-11 | Create new staff user | Staff created with role assignment | |
| SA-12 | Approve pending account request | User activated | |
| SA-13 | Reset another user's password | Password reset successful | |
| SA-14 | View License page | License status, expiry, audit visible | |
| SA-15 | Access Chatbot Admin | Settings, Analytics, Escalations available | |
| SA-16 | Manage CMS pages | Create/edit/delete pages and notices | |
| SA-17 | View System Management | Report management, theme management, user management | |

---

## Admin

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| AD-01 | Login with admin.uat@hospital.com / Medyx147 | Forced password change | |
| AD-02 | Dashboard KPIs visible | Patient count, appointments, revenue, pending bills | |
| AD-03 | View Audit Logs in sidebar | Filterable, color-coded, CSV export | |
| AD-04 | Create patient record | Patient added successfully | |
| AD-05 | Schedule appointment | Appointment created with doctor selection | |
| AD-06 | Create bill | Bill with line items created | |
| AD-07 | Process payment | Payment recorded, bill status updated | |
| AD-08 | Generate report | Report generated with data | |
| AD-09 | Manage staff (edit/delete) | Staff records modified | |
| AD-10 | Approve pending account | Account activated, user notified | |
| AD-11 | Upload profile picture | Image shown in navbar | |
| AD-12 | Enable MFA (optional) | Setup completes, login requires OTP | |
| AD-13 | Cannot access License page | License menu hidden/restricted | |

---

## Doctor

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| DR-01 | Login with doctor.uat@hospital.com / UatRole@123! | Redirected to Dashboard | |
| DR-02 | View OPD visits | Patient list and visit details visible | |
| DR-03 | Create OPD visit | Visit record created with diagnosis | |
| DR-04 | View IPD admissions | Admission list with patient details | |
| DR-05 | Create prescription | Prescription with medicines created | |
| DR-06 | Order lab test | Lab test ordered for patient | |
| DR-07 | Order radiology test | Radiology test ordered | |
| DR-08 | View patient medical history | Previous visits and results shown | |
| DR-09 | Cannot access Billing admin | Billing management restricted | |
| DR-10 | Cannot access Audit Logs | Audit Logs not in sidebar | |
| DR-11 | Upload profile picture | Image shows in navbar | |
| DR-12 | Enable MFA (optional) | Setup completes normally | |

---

## Nurse

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| NU-01 | Login with nurse.uat@hospital.com / UatRole@123! | Redirected to Dashboard | |
| NU-02 | View IPD admissions | Patient admission list visible | |
| NU-03 | View patient details | Patient information accessible | |
| NU-04 | Check attendance (check-in/out) | Attendance recorded | |
| NU-05 | View bed management | Bed overview and status visible | |
| NU-06 | Cannot access Billing admin | Billing restricted | |
| NU-07 | Cannot access Audit Logs | Audit Logs not in sidebar | |

---

## Pharmacist

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| PH-01 | Login with pharmacist.uat credentials | Redirected to Dashboard | |
| PH-02 | View prescriptions | Prescription list with patient details | |
| PH-03 | View medicine inventory | Medicine stock levels shown | |
| PH-04 | Dispense medicine | Pharmacy bill created | |
| PH-05 | Cannot access IPD/OPD | Clinical modules restricted | |

---

## Accountant

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| AC-01 | Login with accountant.uat@hospital.com / UatRole@123! | Redirected to Dashboard | |
| AC-02 | View all bills | Bill list with status filters | |
| AC-03 | Create bill | Bill with line items created | |
| AC-04 | Process payment | Payment recorded | |
| AC-05 | Download receipt (PDF) | PDF receipt generated correctly | |
| AC-06 | View financial reports | Revenue and payment reports | |
| AC-07 | Cannot access Audit Logs | Restricted | |

---

## Receptionist

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| RC-01 | Login with receptionist.uat@hospital.com / UatRole@123! | Redirected to Dashboard | |
| RC-02 | Register new patient | Patient record created | |
| RC-03 | Schedule appointment | Appointment created | |
| RC-04 | Log visitor (Front Office) | Visitor record added | |
| RC-05 | Register complaint | Complaint logged with status | |
| RC-06 | Check-in visitor | Check-in time recorded | |
| RC-07 | Check-out visitor | Check-out time recorded | |

---

## Lab Technician

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| LT-01 | Login with lab technician credentials | Redirected to Dashboard | |
| LT-02 | View lab test catalog | All tests listed | |
| LT-03 | View pending tests | Pending count and list shown | |
| LT-04 | Enter lab result | Result with value, normal range, interpretation | |
| LT-05 | Update result status | Ordered → In Progress → Completed | |
| LT-06 | Cannot access Audit Logs | Restricted | |

---

## Radiologist

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| RA-01 | Login with radiologist credentials | Redirected to Dashboard | |
| RA-02 | View radiology test catalog | Tests listed | |
| RA-03 | View pending radiology tests | Pending list shown | |
| RA-04 | Enter radiology result | Findings and impression recorded | |
| RA-05 | Cannot access Audit Logs | Restricted | |

---

## Staff (General)

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| ST-01 | Login with staff credentials | Redirected to Dashboard | |
| ST-02 | Check-in attendance | Check-in time recorded | |
| ST-03 | Check-out attendance | Check-out time recorded | |
| ST-04 | Submit leave request | Leave request with date range and reason | |
| ST-05 | View leave balance | Balance shown by year | |
| ST-06 | View notifications | Notification list with unread/read | |
| ST-07 | Cannot access Audit Logs | Restricted | |
| ST-08 | Cannot access Admin modules | CMS, Module Management hidden | |

---

## Patient (Portal)

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| PA-01 | Login to Patient Portal (/PatientPortal) | Patient dashboard displayed | |
| PA-02 | View own appointments | Appointment list shown | |
| PA-03 | Book new appointment | Appointment created with doctor selection | |
| PA-04 | Cancel appointment | Appointment cancelled | |
| PA-05 | View medical records | OPD visits, IPD admissions, lab results | |
| PA-06 | View bills | Bill history and payment status | |
| PA-07 | Cannot access Staff Portal (/Dashboard) | Access Denied or redirected | |

---

## Cross-Cutting Features (All Authenticated Users)

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| CC-01 | Sidebar toggle works for all staff roles | Collapse/expand functional | |
| CC-02 | Profile picture upload works | JPG/PNG upload on Profile page | |
| CC-03 | Default avatar shows when no image | SVG placeholder visible | |
| CC-04 | MFA setup available from Profile | QR code display, OTP validation | |
| CC-05 | Forced password change on Medyx147 | Redirect + password change flow | |
| CC-06 | Notifications badge updates | AJAX refresh shows unread count | |
| CC-07 | Chatbot FAB button visible | AI Assistant button on all pages | |

---

## Mobile Responsiveness

| # | Test | Expected | Pass/Fail |
|---|------|----------|-----------|
| MO-01 | Viewport ≤ 768px — sidebar becomes overlay | Toggle opens overlay sidebar | |
| MO-02 | Tables scroll horizontally | No content clipping | |
| MO-03 | Forms stack vertically | Labels above inputs | |
| MO-04 | Navbar collapses to hamburger | Navigation menu toggle functional | |
| MO-05 | Cards resize to full width | No horizontal overflow | |

---

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| SuperAdmin | | | |
| Admin | | | |
| Doctor | | | |
| Tester | | | |
| Project Manager | | | |
