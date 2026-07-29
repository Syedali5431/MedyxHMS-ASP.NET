# Medyx HMS — Complete Page Directory & Documentation

> **Generated:** 2026-07-29  
> **Application:** Medyx Hospital Management System (ASP.NET Core MVC)  
> **Scope:** All pages/routes across the staff portal, public website, patient portal, and API endpoints.

---

## Table of Contents

1. [Authentication & Account Management](#1-authentication--account-management)
2. [Dashboard](#2-dashboard)
3. [Patient Management](#3-patient-management)
4. [Appointment Management](#4-appointment-management)
5. [OPD (Outpatient Department)](#5-opd-outpatient-department)
6. [IPD (Inpatient Department)](#6-ipd-inpatient-department)
7. [Billing & Payments](#7-billing--payments)
8. [Prescription & Pharmacy](#8-prescription--pharmacy)
9. [Laboratory (Lab)](#9-laboratory-lab)
10. [Radiology](#10-radiology)
11. [Blood Bank](#11-blood-bank)
12. [Operation Theatre](#12-operation-theatre)
13. [Bed Management](#13-bed-management)
14. [Ambulance Management](#14-ambulance-management)
15. [Front Office](#15-front-office)
16. [Staff Management](#16-staff-management)
17. [Attendance](#17-attendance)
18. [Leave Management](#18-leave-management)
19. [Payroll](#19-payroll)
20. [Certificates & ID Cards](#20-certificates--id-cards)
21. [Referral Management](#21-referral-management)
22. [Reports & Analytics](#22-reports--analytics)
23. [Messaging (Internal)](#23-messaging-internal)
24. [Inventory Management](#24-inventory-management)
25. [Download Center](#25-download-center)
26. [Live Consultation (Telemedicine)](#26-live-consultation-telemedicine)
27. [Birth & Death Records](#27-birth--death-records)
28. [TPA (Third Party Administrator)](#28-tpa-third-party-administrator)
29. [Chatbot / AI Assistant](#29-chatbot--ai-assistant)
30. [CMS (Content Management System)](#30-cms-content-management-system)
31. [Public Website (Site)](#31-public-website-site)
32. [Patient Portal](#32-patient-portal)
33. [System Management](#33-system-management)
34. [License Management](#34-license-management)
35. [Notifications](#35-notifications)
36. [Audit Logs](#36-audit-logs)
37. [Module Management](#37-module-management)
38. [Account Approvals](#38-account-approvals)
39. [Public Site Admin](#39-public-site-admin)
40. [Mobile API Endpoints](#40-mobile-api-endpoints)

---

## 1. Authentication & Account Management

**Controller:** `AccountController`  
**Route:** `/Account/{action}`

### Pages

#### Login (`/Account/Login`)
- **Access:** AllowAnonymous
- **Description:** Main login page for all staff and admin roles. Accepts email or Employee ID with password. Supports role selection for multi-role users. Includes bcrypt password migration for legacy accounts, concurrent session enforcement, and license expiry gating. On success, redirects to the appropriate dashboard based on the user's role.

#### Register (`/Account/Register`)
- **Access:** AllowAnonymous (or SuperAdmin for elevated roles)
- **Description:** User self-registration page. Allows users to sign up with username, email, password, Employee ID, and role selection (Staff, Doctor, Nurse, Receptionist, Accountant, Pharmacist, LabTechnician, Radiologist, Patient). Admin/SuperAdmin creation requires an authenticated SuperAdmin. New accounts are created as inactive and must be approved via Accounts Approval workflow.

#### Logout (`/Account/Logout`)
- **Access:** Authenticated
- **Description:** Logs out the current user, ends the concurrent session, removes the ActiveRole from session, and redirects to the login page.

#### License Expired (`/Account/LicenseExpired`)
- **Access:** Admin, SuperAdmin
- **Description:** Shown when the system license has expired. Admin users are redirected here instead of the dashboard. Provides a way to request a new license file.

#### ValidateCredentials (AJAX endpoint)
- **Access:** AllowAnonymous (POST)
- **Description:** AJAX endpoint that validates login credentials without signing in. Returns the user's roles for the UI to display role selection options. Used by the login page for role-aware UX.

---

## 2. Dashboard

**Controller:** `DashboardController`  
**Route:** `/Dashboard/{action}`  
**Access:** Authorized (all authenticated users)

### Pages

#### Dashboard Overview (`/Dashboard/Index`)
- **Description:** The main landing page after login. Displays a comprehensive KPI dashboard with:
  - Total Patients count
  - Today's Appointments count
  - Monthly Revenue summary
  - Pending Bills count
  - Recent Appointments list (last 5)
  - Last 7 days charts (appointments per day, revenue per day)
  - Module Explorer navigation grid (role-aware, permission-filtered modules)
  - Hospital settings and feature toggles
  - User permissions and roles overview

#### Admin Dashboard View (`/Dashboard/Admin`)
- **Access:** ManageUsers permission
- **Description:** Admin-focused dashboard view.

#### Patient Dashboard View (`/Dashboard/Patient`)
- **Access:** ViewPatients permission
- **Description:** Patient-focused dashboard view.

#### Appointment Dashboard View (`/Dashboard/Appointment`)
- **Access:** ViewAppointments permission
- **Description:** Appointment-focused dashboard view.

#### Billing Dashboard View (`/Dashboard/Billing`)
- **Access:** ViewBills permission
- **Description:** Billing-focused dashboard view.

---

## 3. Patient Management

**Controller:** `PatientController`  
**Route:** `/Patient/{action}`  
**Access:** Authorized (ViewPatients permission required)

### Pages

#### Patient List (`/Patient/Index`)
- **Description:** Displays a searchable, filterable list of all registered patients. Shows Patient ID, Name, Email, Phone, Gender, Blood Group, Status (Active/Inactive), and Created date. Provides search functionality by name, ID, or contact. Displays total, active, and inactive patient counts.

#### Create Patient (`/Patient/Create`)
- **Description:** Registration form for adding a new patient to the system. Collects personal details, contact information, medical history, and demographics.

#### Patient Details (`/Patient/Details/{id}`)
- **Description:** Full patient profile view showing all registered information, medical history, appointments, bills, and visit history.

#### Edit Patient (`/Patient/Edit/{id}`)
- **Description:** Edit form to update existing patient information.

#### Delete Patient (`/Patient/Delete/{id}`)
- **Description:** Delete confirmation and removal of a patient record.

#### Export Patients (`/Patient/Export`)
- **Description:** Export patient data in CSV or PDF format. Supports filtering by search term.

---

## 4. Appointment Management

**Controller:** `AppointmentController`  
**Route:** `/Appointment/{action}`  
**Access:** Authorized (ViewAppointments permission required)

### Pages

#### Appointment List (`/Appointment/Index`)
- **Description:** Comprehensive appointment management page with filtering by:
  - Search term (patient name/ID, doctor name)
  - Status filter (All, Scheduled, Completed, Cancelled, etc.)
  - Date filter (specific date)
  - Doctor filter
- Displays total, today's, upcoming, and completed appointment counts.

#### Appointment Calendar (`/Appointment/Calendar`)
- **Description:** Calendar view for visual appointment planning and scheduling.

#### Create Appointment (`/Appointment/Create`)
- **Description:** Form to schedule a new appointment. Selects patient, doctor, date, time, type, and records symptoms/notes.

#### Appointment Details (`/Appointment/Details/{id}`)
- **Description:** Detailed view of a specific appointment including patient info, doctor, date/time, status, symptoms, and notes.

#### Edit Appointment (`/Appointment/Edit/{id}`)
- **Description:** Modify an existing appointment's details.

#### Delete Appointment (`/Appointment/Delete/{id}`)
- **Description:** Cancel or remove an appointment.

#### Export Appointments (`/Appointment/Export`)
- **Description:** Export appointment data in CSV or PDF format with current filters applied.

---

## 5. OPD (Outpatient Department)

**Controller:** `OPDController`  
**Route:** `/OPD/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor

### Pages

#### OPD Visits List (`/OPD/Index`)
- **Description:** Paginated, filterable list of OPD visits. Filters include:
  - Payment status (all, paid, unpaid, pending)
  - Date range (start/end date)
  - Doctor filter
  - Patient filter
- Displays visit details: Patient, Doctor, Visit Date, Symptoms, Diagnosis, Treatment, Prescription, Consultation Fee, Payment Status.

#### OPD Visit Details (`/OPD/Details/{id}`)
- **Description:** Full details of a single OPD visit including patient info, doctor info, medical notes, diagnosis, treatment plan, and payment status.

#### Create OPD Visit (`/OPD/Create`)
- **Description:** Form to record a new OPD consultation. Includes patient selection, doctor assignment, symptoms, diagnosis, treatment, prescription, and consultation fee.

#### Edit OPD Visit (`/OPD/Edit/{id}`)
- **Description:** Update an existing OPD visit record.

#### Delete OPD Visit (`/OPD/Delete/{id}`)
- **Description:** Remove an OPD visit record.

---

## 6. IPD (Inpatient Department)

**Controller:** `IPDController`  
**Route:** `/IPD/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor, Nurse

### Pages

#### IPD Admissions List (`/IPD/Index`)
- **Description:** Paginated list of all IPD admissions with filters:
  - Status (all, admitted, discharged, transferred)
  - Date range (admission date)
  - Doctor filter
  - Patient filter
- Displays admission details including patient, doctor, admission date, discharge date, ward/bed info, status, and charges.

#### IPD Admission Details (`/IPD/Details/{id}`)
- **Description:** Comprehensive view of an IPD admission including:
  - Patient and doctor information
  - Assigned bed and ward details
  - Daily charges and total charges calculation
  - Related IPD bills
  - Admission and discharge dates

#### Create IPD Admission (`/IPD/Create`)
- **Description:** Admission form to admit a patient to IPD. Selects patient, doctor, ward, bed, admission type (planned/emergency), and sets initial status.

#### Discharge Patient (`/IPD/Discharge/{id}`)
- **Description:** Discharge workflow for an admitted patient, calculating final charges and updating bed availability.

#### Edit IPD Admission (`/IPD/Edit/{id}`)
- **Description:** Modify an existing IPD admission record.

---

## 7. Billing & Payments

**Controller:** `BillingController`  
**Route:** `/Billing/{action}`  
**Access:** Admin, SuperAdmin, Staff, Accountant, Receptionist

### Pages

#### Billing Dashboard (`/Billing/Index`)
- **Description:** Paginated list of all bills with payment status filter (all, paid, unpaid, partially paid). Displays:
  - Bill number, patient name, bill date, due date
  - Total amount, paid amount, pending amount
  - Status indicator
  - Bill items and breakdown
  - Payment records with methods and transaction IDs

#### Create Bill (`/Billing/Create`)
- **Description:** Generate a new bill for a patient. Add bill items with descriptions, quantities, unit prices, and categories. Links to patient and appointment.

#### Bill Details (`/Billing/Details/{id}`)
- **Description:** Complete bill view with all items, payments, and payment history.

#### Edit Bill (`/Billing/Edit/{id}`)
- **Description:** Modify an existing bill's items and amounts.

#### Record Payment (`/Billing/RecordPayment/{id}`)
- **Description:** Record a payment against a bill. Supports multiple payment methods (cash, card, online, insurance).

#### Download Receipt (`/Billing/DownloadReceipt/{id}`)
- **Description:** Download a printable PDF receipt for a paid bill.

#### Export Bills (`/Billing/Export`)
- **Description:** Export billing data in CSV or PDF format.

---

## 8. Prescription & Pharmacy

**Controller:** `PrescriptionController`  
**Route:** `/Prescription/{action}`  
**Access:** Admin, SuperAdmin, Doctor, Nurse, Staff, Pharmacist

### Pages

#### Prescriptions List (`/Prescription/Index`)
- **Description:** Paginated list of all prescriptions with optional patient filter. Shows medicine name, dosage, frequency, duration, quantity, unit price, total price, and instructions.

#### Prescription Details (`/Prescription/Details/{id}`)
- **Description:** Full prescription details including medicine information, dosage instructions, and pricing.

#### Create Prescription (`/Prescription/Create`)
- **Description:** Form to write a new prescription for a patient. Select medicine, specify dosage, frequency, duration, quantity, and special instructions.

#### Edit Prescription (`/Prescription/Edit/{id}`)
- **Description:** Modify an existing prescription.

#### Medicines Catalog (`/Prescription/Medicines`)
- **Description:** Medicine catalog management — view, add, edit, and manage the pharmacy's medicine inventory.

---

## 9. Laboratory (Lab)

**Controller:** `LabController`  
**Route:** `/Lab/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor, LabTechnician

### Pages

#### Lab Tests (`/Lab/Index`)
- **Description:** Paginated catalog of all lab tests. Lists test names, categories, and prices. Supports creating, editing, and deleting lab test definitions.

#### Create Lab Test (`/Lab/CreateTest`)
- **Description:** Define a new lab test type (name, category, price, normal ranges).

#### Edit Lab Test (`/Lab/EditTest/{id}`)
- **Description:** Modify an existing lab test definition.

#### Delete Lab Test (`/Lab/DeleteTest/{id}`)
- **Description:** Remove a lab test type.

#### Lab Results (`/Lab/Results`)
- **Description:** Paginated list of lab test results with status filtering (All, Pending, Completed, Reviewed). Tracks the workflow from order to result.

#### Create Lab Result (`/Lab/CreateResult`)
- **Description:** Record lab test results for a patient. Associate with a lab test order, enter result values, add notes.

#### Edit Lab Result (`/Lab/EditResult/{id}`)
- **Description:** Update existing lab result entries.

---

## 10. Radiology

**Controller:** `RadiologyController`  
**Route:** `/Radiology/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor, Radiologist

### Pages

#### Radiology Tests (`/Radiology/Index`)
- **Description:** Paginated catalog of radiology tests (X-ray, MRI, CT Scan, Ultrasound, etc.). Manage test definitions, categories, and prices.

#### Create Radiology Test (`/Radiology/CreateTest`)
- **Description:** Define a new radiology test type.

#### Edit Radiology Test (`/Radiology/EditTest/{id}`)
- **Description:** Modify existing radiology test definitions.

#### Radiology Results (`/Radiology/Results`)
- **Description:** Manage radiology reports and results. Track status from order to report delivery.

#### Create Radiology Result (`/Radiology/CreateResult`)
- **Description:** Record radiology findings and upload associated images/files.

---

## 11. Blood Bank

**Controller:** `BloodBankController`  
**Route:** `/BloodBank/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor, Nurse

### Pages

#### Blood Inventory (`/BloodBank/Index`)
- **Description:** Blood stock management page. Displays inventory by blood group (A+, A-, B+, B-, AB+, AB-, O+, O-) with units available and minimum level thresholds. Shows recent blood issue history. Allows updating inventory levels.

#### Issue Blood (`/BloodBank/Issue`)
- **Description:** Form to issue blood units to a patient. Selects patient, blood group, units to issue, and records the issue. Automatically creates a billing entry.

---

## 12. Operation Theatre

**Controller:** `OperationTheatreController`  
**Route:** `/OperationTheatre/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor, Nurse

### Pages

#### OT Schedule (`/OperationTheatre/Index`)
- **Description:** List of all operation theatre schedules. Shows patient, procedure, scheduled date/time, status, and assigned surgical team.

#### Create OT Booking (`/OperationTheatre/Create`)
- **Description:** Schedule a new operation. Select patient, procedure name, date/time, theatre number, surgeon, anaesthetist, and notes. Automatically generates billing entry.

#### Update OT Status (`/OperationTheatre/UpdateStatus`)
- **Description:** Update the status of a scheduled operation (Scheduled, In Progress, Completed, Cancelled).

---

## 13. Bed Management

**Controller:** `BedManagementController`  
**Route:** `/BedManagement` and `/bed-management`  
**Access:** All roles (management actions: Admin, SuperAdmin, Nurse)

### Pages

#### Bed Overview (`/BedManagement/Index`)
- **Description:** Comprehensive bed management dashboard showing:
  - All beds organized by block, floor, ward, room, and bed number
  - Bed status indicators (Available, Occupied, Cleaning, Maintenance)
  - Occupied beds show assigned patient information
  - Summary statistics (total beds, available, occupied, maintenance)
  - Ward list with counts
  - Patient lookup for bed assignment
  - Filter by block, floor, room

#### Assign Bed (`/BedManagement/Assign`)
- **Description:** Assign an available bed to a patient. Enforces ICU bed assignment rules based on user role.

#### Update Bed Status
- **Description:** Change bed status to Available, Cleaning, or Maintenance.

---

## 14. Ambulance Management

**Controller:** `AmbulanceController`  
**Route:** `/Ambulance/{action}`  
**Access:** Authorized

### Pages

#### Ambulance Vehicles (`/Ambulance/Index`)
- **Description:** Fleet management page listing all ambulance vehicles with vehicle numbers, status (Available, On Dispatch, Maintenance), driver info, and last service date.

#### Add Vehicle (`/Ambulance/Create`)
- **Access:** Admin, SuperAdmin
- **Description:** Register a new ambulance vehicle.

#### Edit Vehicle (`/Ambulance/Edit/{id}`)
- **Access:** Admin, SuperAdmin
- **Description:** Modify vehicle details.

#### Dispatch Log (`/Ambulance/Dispatches`)
- **Description:** History of all ambulance dispatches including patient, vehicle, dispatch time, return time, destination, and driver.

#### New Dispatch (`/Ambulance/Dispatch`)
- **Description:** Dispatch an available ambulance. Select vehicle, patient, driver, destination, and record dispatch time.

---

## 15. Front Office

**Controller:** `FrontOfficeController`  
**Route:** `/FrontOffice/{action}`  
**Access:** Admin, SuperAdmin, Staff, Receptionist

### Pages

#### Front Office Home (`/FrontOffice/Index`)
- **Description:** Daily front office dashboard showing:
  - Visitor logs for the selected date
  - Recent complaints (last 10)
  - Recent dispatch/receive records (last 10)

#### Visitors (`/FrontOffice/Visitors`)
- **Description:** Manage visitor logs — record visitor check-in/check-out, visitor name, contact, person to visit, purpose, and time.

#### Add Visitor (`/FrontOffice/AddVisitor`)
- **Description:** Log a new visitor entry.

#### Check Out Visitor (`/FrontOffice/CheckOutVisitor`)
- **Description:** Record visitor departure time.

#### Complaints (`/FrontOffice/Complaints`)
- **Description:** Manage complaint records. Filter by status (Open, In Progress, Resolved, Closed). View complaint details and resolution history.

#### Add Complaint (`/FrontOffice/AddComplaint`)
- **Description:** Register a new complaint from a patient or visitor.

#### Dispatch/Receive (`/FrontOffice/DispatchReceive`)
- **Description:** Track outgoing dispatches and incoming deliveries (documents, parcels, etc.).

---

## 16. Staff Management

**Controller:** `StaffController`  
**Route:** `/Staff/{action}`  
**Access:** Authorized (ManageUsers permission)

### Pages

#### Staff List (`/Staff/Index`)
- **Description:** Comprehensive staff directory with filtering by:
  - Search term (name, Employee ID, email)
  - Department filter
  - Role filter
  - Active/Inactive status filter
- Paginated display with staff photo, name, department, role, contact, and status.

#### Create Staff (`/Staff/Create`)
- **Description:** Register a new staff member. Links to user account creation.

#### Staff Details (`/Staff/Details/{id}`)
- **Description:** Full staff profile with personal info, employment details, role assignments, and activity log.

#### Edit Staff (`/Staff/Edit/{id}`)
- **Description:** Update staff member information and role.

#### Delete Staff (`/Staff/Delete/{id}`)
- **Description:** Deactivate or remove a staff record.

---

## 17. Attendance

**Controller:** `AttendanceController`  
**Route:** `/Attendance/{action}`  
**Access:** Admin, SuperAdmin, Staff, Nurse, Doctor

### Pages

#### Attendance Records (`/Attendance/Index`)
- **Description:** Daily attendance management page. Shows:
  - Date selector
  - Staff filter (individual or all)
  - Attendance records for selected date/staff
  - Summary statistics (present, absent, late, leave counts)
  - Manual attendance marking form

#### Mark Attendance (`/Attendance/MarkAttendance`)
- **Description:** Record attendance for a staff member (Present, Absent, Late, Half-Day).

#### Check In (`/Attendance/CheckIn`)

- **Description:** Self-service check-in for staff members.

#### Check Out (`/Attendance/CheckOut`)
- **Description:** Self-service check-out for staff members.

---

## 18. Leave Management

**Controller:** `LeaveController`  
**Route:** `/Leave/{action}`  
**Access:** Admin, SuperAdmin, Staff, Nurse, Doctor

### Pages

#### Leave Requests (`/Leave/Index`)
- **Description:** Leave management dashboard with filters:
  - Date range (start/end)
  - Staff filter
  - Status filter (Pending, Approved, Rejected, Cancelled)
- Lists all leave requests with staff name, leave type, dates, duration, status, and notes.

#### Request Leave (`/Leave/Request`)
- **Description:** Form to submit a new leave request. Select staff, leave type, dates, reason, and attach supporting documents.

#### Leave Types (`/Leave/Types`)
- **Access:** Admin
- **Description:** Configure leave types (Annual, Sick, Personal, Maternity, etc.) with default allowances.

#### Leave Balances (`/Leave/Balances`)
- **Access:** Admin
- **Description:** Track and manage staff leave balances by leave type.

#### Approve/Reject Leave (`/Leave/Review/{id}`)
- **Description:** Approve or reject pending leave requests.

---

## 19. Payroll

**Controller:** `PayrollController`  
**Route:** `/Payroll/{action}`  
**Access:** Admin, SuperAdmin, Staff, Accountant

### Pages

#### Payroll Records (`/Payroll/Index`)
- **Description:** Monthly payroll records view. Filter by month and staff member. Displays salary components, allowances, deductions, net pay, and payment status.

#### Generate Payroll (`/Payroll/Generate`)
- **Access:** Admin, SuperAdmin
- **Description:** Generate payroll for a staff member for a specific month. Configure allowances, deductions, and add notes.

#### Payroll Details (`/Payroll/Details/{id}`)
- **Description:** Detailed breakdown of a payroll record including all salary components.

#### Edit Payroll (`/Payroll/Edit/{id}`)
- **Description:** Modify an existing payroll record.

---

## 20. Certificates & ID Cards

**Controller:** `CertificateController`  
**Route:** `/Certificate/{action}`  
**Access:** Admin, SuperAdmin, Doctor, Nurse, Pharmacist, Accountant, Receptionist, LabTechnician, Radiologist, Staff

### Pages

#### Certificates List (`/Certificate/Index`)
- **Description:** Management page for certificates and ID cards. Filter by staff member. Shows all issued certificates and ID cards with their status.

#### Birth Certificate (`/Certificate/Birth`)
- **Description:** Birth certificate landing page and generator. Create printable birth certificates.

#### Death Certificate (`/Certificate/Death`)
- **Description:** Death certificate page.

#### Generate Certificate (`/Certificate/GenerateCertificate`)
- **Access:** Admin
- **Description:** Create a printable certificate (experience, character, etc.) for a staff member.

#### Generate ID Card (`/Certificate/GenerateIdCard`)
- **Access:** Admin
- **Description:** Generate printable staff ID cards with photo and details.

---

## 21. Referral Management

**Controller:** `ReferralController`  
**Route:** `/Referral/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor

### Pages

#### Referrals List (`/Referral/Index`)
- **Description:** List of all patient referrals. Shows patient, referral type (Internal/External), referred to, referral date, status, and notes.

#### Create Referral (`/Referral/Create`)
- **Description:** Create a new patient referral. Select patient, referral type, referred doctor/institution, reason, and notes.

#### Update Referral Status (`/Referral/UpdateStatus`)
- **Description:** Update referral status (Pending, Accepted, Declined, Completed).

---

## 22. Reports & Analytics

**Controller:** `ReportController`  
**Route:** `/Report/{action}`  
**Access:** Authorized

### Pages

#### Reports Dashboard (`/Report/Index`)
- **Access:** Admin, SuperAdmin, Accountant
- **Description:** Consolidated report hub. Lists all available report types from the report catalog. Supports selecting a report, applying date filters, and generating output. Integrates with legacy PHP report templates.

#### Department Report (`/Report/DepartmentReport`)
- **Description:** Department-wise analytics and performance metrics.

#### Financial Report (`/Report/FinancialReport`)
- **Description:** Revenue and financial analysis reports.

#### Occupancy Report (`/Report/OccupancyReport`)
- **Description:** Bed and ward occupancy analytics over time.

#### Staff Report (`/Report/StaffReport`)
- **Description:** Staff attendance and staffing trend analysis.

#### Payroll Report (`/Report/PayrollReport`)
- **Description:** Payroll summary reports and cost trends.

#### Generated Reports (`/Report/GeneratedReports`)
- **Description:** Review previously generated report history and output.

#### Report Builder (`/Report/Builder`)
- **Access:** Admin
- **Description:** Create and edit custom report templates with the built-in report editor.

---

## 23. Messaging (Internal)

**Controller:** `MessagingController`  
**Route:** `/Messaging/{action}`  
**Access:** Authorized

### Pages

#### Inbox (`/Messaging/Index`)
- **Description:** Internal messaging inbox. Shows received messages (personal and broadcast). Messages display sender, subject, timestamp, and read status.

#### Sent Messages (`/Messaging/Sent`)
- **Description:** List of sent messages. Shows recipients, subject, timestamp, and delivery status.

#### Compose Message (`/Messaging/Compose`)
- **Description:** Compose and send an internal message. Select recipient(s) from staff list, add subject and body. Supports replying to existing messages (auto-populates "Re:" subject).

#### Broadcast (`/Messaging/Broadcast`)
- **Access:** Admin
- **Description:** Send a broadcast message to all staff members simultaneously.

---

## 24. Inventory Management

**Controller:** `InventoryController`  
**Route:** `/Inventory/{action}`  
**Access:** Authorized

### Pages

#### Inventory Items (`/Inventory/Index`)
- **Description:** Browse and search inventory items by name, code, or category. Shows stock levels, reorder levels, and supplier info. Highlights low-stock items.

#### Add Item (`/Inventory/Create`)
- **Access:** Admin, SuperAdmin
- **Description:** Register a new inventory item with name, code, category, unit, stock level, reorder level, and supplier.

#### Edit Item (`/Inventory/Edit/{id}`)
- **Access:** Admin, SuperAdmin
- **Description:** Modify existing inventory item details.

#### Transactions (`/Inventory/Transactions`)
- **Description:** Review stock movement history — incoming (purchase/receive) and outgoing (issue/consumption) transactions.

#### Low Stock (`/Inventory/LowStock`)
- **Description:** View all items that are at or below their reorder level for prioritized restocking.

---

## 25. Download Center

**Controller:** `DownloadCenterController`  
**Route:** `/DownloadCenter/{action}`  
**Access:** Authorized

### Pages

#### Download Center (`/DownloadCenter/Index`)
- **Description:** Browse shared files and documents organized by category. Public files available to all staff; admin-only files visible to Admin/SuperAdmin roles. Supports downloading uploaded documents.

#### Upload File (`/DownloadCenter/Upload`)
- **Access:** Admin, SuperAdmin
- **Description:** Upload a new file/document for staff. Set category, visibility (public/admin), and file description.

---

## 26. Live Consultation (Telemedicine)

**Controller:** `LiveConsultationController`  
**Route:** `/LiveConsultation/{action}`  
**Access:** Authorized

### Pages

#### Consultations List (`/LiveConsultation/Index`)
- **Description:** List of all scheduled live consultation sessions. Shows patient, doctor/specialist, scheduled date/time, duration, platform (Zoom, etc.), and status.

#### Schedule Session (`/LiveConsultation/Schedule`)
- **Description:** Schedule a new telemedicine/live consultation. Select patient, doctor, date/time, duration, platform, and provide meeting link or details.

---

## 27. Birth & Death Records

**Controller:** `BirthDeathController`  
**Route:** `/BirthDeath/{action}`  
**Access:** Authorized

### Pages

#### Birth Records (`/BirthDeath/Index`)
- **Description:** List of all birth records. Shows baby name, date of birth,性别, mother's name, father's name, certificate number, and attending doctor.

#### Create Birth Record (`/BirthDeath/CreateBirth`)
- **Description:** Register a new birth. Enter baby details, parent information, birth weight, delivery type, and auto-generates certificate number.

#### Birth Details (`/BirthDeath/BirthDetails/{id}`)
- **Description:** View full birth record details with certificate information.

#### Death Records (`/BirthDeath/Deaths`)
- **Description:** List of all death records. Shows deceased name, date of death, cause, and certificate details.

#### Create Death Record (`/BirthDeath/CreateDeath`)
- **Description:** Register a death record with deceased details, cause of death, date, and attending physician.

#### Death Details (`/BirthDeath/DeathDetails/{id}`)
- **Description:** View full death record details.

---

## 28. TPA (Third Party Administrator)

**Controller:** `TpaController`  
**Route:** `/Tpa/{action}`  
**Access:** Admin, SuperAdmin, Staff, Doctor, Receptionist

### Pages

#### TPA Providers (`/Tpa/Index`)
- **Description:** List of registered Third Party Administrator (insurance) providers. Shows provider name, contact details, coverage types, and status.

#### Create TPA Provider (`/Tpa/Create`)
- **Access:** Admin, SuperAdmin
- **Description:** Register a new TPA/insurance provider.

#### Edit TPA Provider (`/Tpa/Edit/{id}`)
- **Access:** Admin, SuperAdmin
- **Description:** Modify TPA provider details.

#### TPA Claims (`/Tpa/Claims`)
- **Description:** Track insurance and TPA claims. Shows patient, provider, claim amount, status, and dates.

#### Create Claim (`/Tpa/CreateClaim`)
- **Description:** Submit a new TPA claim for a patient's treatment.

---

## 29. Chatbot / AI Assistant

**Controller:** `ChatbotController`  
**Route:** `/Chatbot/{action}`  
**Access:** AllowAnonymous (feature-gated per role)

### Pages

#### Chatbot Interface (`/Chatbot/Index`)
- **Description:** AI-powered hospital assistant chatbot interface. Features:
  - Session-based conversations
  - History retrieval for authenticated users
  - Consent management for data usage
  - Supports appointment guidance, billing guidance, and general inquiries
  - Multilingual support
  - Role-based access (patients, staff, admins)

#### Consent Request (`/Chatbot/RequestConsent`)
- **Description:** Page to request/renew user consent for chatbot data usage.

### Chatbot Admin

**Controller:** `ChatbotAdminController`  
**Route:** `/ChatbotAdmin/{action}`  
**Access:** Admin, SuperAdmin

#### Chatbot Settings (`/ChatbotAdmin/Settings`)
- **Description:** Full chatbot configuration page:
  - Enable/disable chatbot globally
  - Enable escalation to human support
  - Enable appointment and billing guidance
  - Multilingual support toggle
  - Role-based enablement (patients, staff, admins)
  - AI model selection and parameters (temperature, max tokens)
  - Usage limits (hourly, unresolved threshold)
  - Data retention policies
  - PII redaction settings and levels
  - Supported languages configuration

---

## 30. CMS (Content Management System)

**Controller:** `CmsController`  
**Route:** `/Cms/{action}`  
**Access:** Admin (RequireAdminRole policy)

### Pages

#### CMS Pages (`/Cms/Index`)
- **Description:** Manage all public website CMS pages. Lists pages with title, slug, status (Draft/Published), menu visibility, sort order, and dates. Supports search and status filtering.

#### Create CMS Page (`/Cms/CreatePage`)
- **Description:** Create a new public website page with title, slug, content (rich text), meta description, featured image, font settings, status, and menu options.

#### Edit CMS Page (`/Cms/EditPage/{id}`)
- **Description:** Modify an existing CMS page's content and settings.

#### Delete CMS Page (`/Cms/DeletePage/{id}`)
- **Description:** Remove a CMS page and its linked menu items.

#### Export CMS Pages (`/Cms/IndexExport`)
- **Description:** Export CMS pages list in CSV or PDF format.

#### CMS Notices (`/Cms/Notices`)
- **Description:** Manage notices, news, events, and programs for the public website. Filterable by type and search term. Lists title, slug, type, active status, and publish date.

#### Create Notice (`/Cms/CreateNotice`)
- **Description:** Create a new notice/news/event with title, slug, summary, content, type, active status, and publish date.

#### Edit Notice (`/Cms/EditNotice/{id}`)
- **Description:** Modify an existing notice.

#### Delete Notice (`/Cms/DeleteNotice/{id}`)
- **Description:** Remove a notice.

#### Export Notices (`/Cms/NoticesExport`)
- **Description:** Export notices list in CSV or PDF format.

#### CMS Menu (`/Cms/Menu`)
- **Description:** Manage the public website navigation menu. View all menu items with order, label, URL, linked page, and active status.

#### Create Menu Item (`/Cms/CreateMenuItem`)
- **Description:** Add a new navigation menu item. Can link to a CMS page or external URL. Configure sort order, open-in-new-tab, and active status.

#### Delete Menu Item (`/Cms/DeleteMenuItem/{id}`)
- **Description:** Remove a menu item.

#### Export Menu (`/Cms/MenuExport`)
- **Description:** Export menu items in CSV or PDF format.

#### Notification Settings (`/Cms/NotificationSettings`)
- **Description:** Comprehensive notification configuration page:
  - Email notification enable/disable
  - SMS notification enable/disable
  - SMS provider selection (Twilio, Africa's Talking)
  - Twilio account configuration (Account SID, Auth Token, From Phone)
  - Africa's Talking configuration (Username, API Key, Sender ID)
  - Live send toggles (test mode vs production)
  - Opt-out lists for email and SMS
  - Appointment notification templates (email subject/body, SMS body)
  - Test SMS/email sending functionality
  - SMTP health check
  - Test history management

#### Delivery Logs (`/Cms/DeliveryLogs`)
- **Description:** View notification delivery logs filtered by channel (email/SMS), status, recipient, test flag, and date range.

#### Page Builder
- **Description:** Rich page editing interface for CMS content management.

---

## 31. Public Website (Site)

**Controller:** `SiteController`  
**Route:** `/` and `/Site/{action}`  
**Access:** AllowAnonymous (public)

### Pages

#### Homepage (`/` or `/Site/Index`)
- **Description:** Public-facing hospital website homepage. Displays:
  - Recent notices and announcements
  - Latest news articles
  - Upcoming events
  - Hero section with customizable content
  - Navigation menu with CMS-managed items
  - Careers content and contact information
  - Configurable colors, fonts, and images

#### About Us (`/Site/About`)
- **Description:** Hospital information page with configurable content from CMS.

#### Contact Us (`/Site/Contact`)
- **Description:** Contact page displaying hospital phone, email, address, and a contact form. Configurable via Public Site Admin.

#### Location (`/Site/Location`)
- **Description:** Hospital location page with embedded Google Map, address, and contact details.

#### Careers (`/Site/Careers`)
- **Description:** Careers/jobs page showing available positions and application instructions.

#### CMS Page by Slug (`/Site/Page/{slug}`)
- **Description:** Dynamic page rendering for any published CMS page by its URL slug.

#### Notices / News / Events (`/Site/Notices`)
- **Description:** Public listing of notices, news, events, or programs. Filterable by type. Paginated display with search functionality.

#### Notice Detail (`/Site/Notice/{slug}`)
- **Description:** Full view of a single notice, news article, or event.

#### Doctors Listing (`/Site/Doctors`)
- **Description:** Public directory of hospital doctors. Filterable by department. Shows doctor name, photo, qualifications, specialization, and available shift timings.

#### Book Appointment (`/Site/BookAppointment`)
- **Description:** Public online appointment booking form. Select doctor, date, time, and provide patient details. Includes CAPTCHA challenge for spam prevention.

---

## 32. Patient Portal

**Area:** `PatientPortal`  
**Route Prefix:** `/PatientPortal/{controller}/{action}`  
**Access:** Patient role (except Login)

### Pages

#### Login (`/PatientPortal/Account/Login`)
- **Access:** AllowAnonymous
- **Description:** Patient-specific login page. Authenticates patients and redirects to the patient dashboard.

#### Dashboard (`/PatientPortal/Dashboard`)
- **Description:** Patient portal home page showing:
  - Welcome message with patient name
  - Patient profile information
  - Upcoming appointments count
  - Pending bills count and outstanding amount
  - Recent appointments list (last 5)
  - Quick links to appointments, bills, and medical records

#### My Appointments (`/PatientPortal/Appointments/Index`)
- **Description:** Patient's appointment list with filtering (all, upcoming, past, cancelled). Paginated display with appointment details, doctor name, department, date, time, status, and notes.

#### Appointment Details (`/PatientPortal/Appointments/Details/{id}`)
- **Description:** Full details of a specific appointment for the patient.

#### My Bills (`/PatientPortal/Bills/Index`)
- **Description:** Patient's billing history with filtering (all, paid, unpaid). Paginated list showing bill number, date, total amount, paid amount, status, and due date.

#### Bill Details (`/PatientPortal/Bills/Details/{id}`)
- **Description:** Detailed bill view for the patient with itemized charges.

#### Medical Records (`/PatientPortal/MedicalRecords/Index`)
- **Description:** Patient's medical records with date range filtering. Shows diagnosis, treatment, prescriptions, doctor name, department, and visit notes.

#### Settings (`/PatientPortal/Settings/Index`)
- **Description:** Patient account settings page:
  - View and edit profile information
  - Update contact details
  - Notification preferences (email, SMS, appointment reminders, bill notifications, test results)
  - Preferred language and timezone
  - Profile image upload

#### Update Settings (`/PatientPortal/Settings/Update`)
- **Description:** Save changes to patient profile and notification preferences.

---

## 33. System Management

**Controller:** `SystemManagementController`  
**Route:** `/SystemManagement/{action}`  
**Access:** Admin, SuperAdmin, and staff roles

### Pages

#### Report Management (`/SystemManagement/ReportManagement`)
- **Description:** Report catalog management page. Allows searching and managing report visibility settings for different user roles. Supports toggling report items on/off per role, and exporting the report catalog.

#### Theme Settings (`/SystemManagement/Themes`)
- **Description:** Staff UI theme configuration. Offers 23 themes:
  - Dark, Light, Sunflower, Snowflake, Ocean, Forest, Midnight, Sunset, Lavender, Graphite, Emerald, Peach, Sky, Rose, Sand, Plum, Aqua, Crimson, Amber, Arctic, Chocolate, Indigo, Lime
- Each theme has a preview and description. Users can select their preferred theme for the staff portal interface.

#### Role Permissions (`/SystemManagement/RolePermissions`)
- **Description:** Manage role-based permissions for all system modules. Assign which roles can access which features/pages.

#### Hospital Settings (`/SystemManagement/HospitalSettings`)
- **Description:** Configure global hospital settings including name, address, contact info, default language, supported languages, and feature toggles.

---

## 34. License Management

**Controller:** `LicenseController`  
**Route:** `/License/{action}`  
**Access:** SuperAdmin

### Pages

#### License Management (`/License/Index`)
- **Description:** Complete license lifecycle management page:
  - Current license snapshot (reference, status, expiry, state)
  - License file upload and validation
  - Module entitlement matrix (which modules are licensed)
  - Legacy full-access license mode indicator
  - Audit history of license changes
  - Reminder history for license expiry
  - Public key configuration (modulus, exponent, verification key)
  - Export entitlement matrix as CSV

---

## 35. Notifications

**Controller:** `NotificationsController`  
**Route:** `/Notifications/{action}`  
**Access:** Authorized

### Pages

#### Notifications List (`/Notifications/Index`)
- **Description:** User's system notifications page. Shows all notifications with title, message, timestamp, and read/unread status.

#### Unread Count (AJAX)
- **Description:** JSON endpoint returning the count of unread notifications for the current user.

#### Mark as Read (`/Notifications/MarkRead/{id}`)
- **Description:** Mark a single notification as read. Supports AJAX and normal POST.

#### Mark All Read (`/Notifications/MarkAllRead`)
- **Description:** Mark all notifications as read for the current user.

---

## 36. Audit Logs

**Controller:** `AuditController`  
**Route:** `/Audit/{action}`  
**Access:** Authorized (Permission:* policy)

### Pages

#### Audit Logs (`/Audit/Index`)
- **Description:** Comprehensive audit trail viewer with filters:
  - Date range (start/end)
  - Entity type filter (Patient, Appointment, Bill, User, etc.)
  - User filter
- Displays timestamp, user, action type (Create, Update, Delete, Login, etc.), entity name/ID, old values, and new values.

#### Audit Log Details (`/Audit/Details/{id}`)
- **Description:** Detailed view of a single audit log entry with formatted old/new value JSON comparison.

---

## 37. Module Management

**Controller:** `ModuleManagementController`  
**Route:** `/ModuleManagement/{action}`  
**Access:** SuperAdmin (most actions)

### Pages

#### System Modules (`/ModuleManagement/Index`)
- **Access:** SuperAdmin
- **Description:** Lists all system modules with global enable/disable toggle. SuperAdmin can enable or disable entire modules (Patient, Appointment, OPD, IPD, Billing, Lab, Radiology, etc.) across the system.

#### Toggle Module (AJAX) (`/ModuleManagement/ToggleGlobal`)
- **Access:** SuperAdmin
- **Description:** AJAX endpoint to toggle a module's global enabled state.

#### Users for Module Assignment (`/ModuleManagement/Users`)
- **Access:** SuperAdmin
- **Description:** Searchable list of active users for per-user module assignment configuration.

#### User Module Map (`/ModuleManagement/UserModuleMap/{userId}`)
- **Access:** SuperAdmin
- **Description:** Configure which modules a specific user can access, overriding global settings.

---

## 38. Account Approvals

**Controller:** `AccountsApprovalController`  
**Route:** `/AccountsApproval/{action}`  
**Access:** Admin, SuperAdmin

### Pages

#### Approval Requests (`/AccountsApproval/Index`)
- **Description:** Lists all pending/reviewed account approval requests from user self-registrations. Shows:
  - User full name, email, Employee ID
  - Requested role
  - Status (Pending, Approved, Rejected)
  - Request date
  - Approval/rejection details
- Filterable by status (Pending, Approved, Rejected, All).

#### Approve Request (`/AccountsApproval/Approve/{id}`)
- **Description:** Approve a pending account request, activating the user.

#### Reject Request (`/AccountsApproval/Reject/{id}`)
- **Description:** Reject a pending account request with optional notes.

---

## 39. Public Site Admin

**Controller:** `PublicSiteAdminController`  
**Route:** `/PublicSiteAdmin/{action}`  
**Access:** Admin, SuperAdmin

### Pages

#### Public Site Settings (`/PublicSiteAdmin/Settings`)
- **Description:** Configure the public-facing hospital website:
  - Contact information (address, phone, email)
  - Google Maps embed URL
  - Careers page content
  - Homepage content (title, tagline, description, font)
  - Contact page content (description, font)
  - Location page content (description, font)
  - Hero images for home, contact, and location pages
  - Color scheme (primary, accent, surface colors)
  - Theme preset selection
  - Heading and button style configuration
  - Image upload for various sections

---

## 40. Mobile API Endpoints

**Controller:** `AppController`  
**Route:** API endpoints  
**Access:** AllowAnonymous (feature-gated)

### API Endpoints

#### App Index (`/App/Index` or `/api/v1/app`)
- **Description:** Mobile API v1 endpoint. Returns hospital app configuration including:
  - API base URL
  - Site URL
  - App logo URL
  - Primary and secondary color codes
  - Default language code
- Feature-gated by MobileAPI toggle.

#### App Config (`/api/v2/app/config`)
- **Description:** Mobile API v2 endpoint. Returns enhanced app configuration including:
  - Base URL and site URL
  - App logo URL
  - Primary and secondary colors
  - Default and supported languages
  - Feature capabilities (Patient Portal, Appointment System, Billing Module, Public Website, Mobile API)
- Feature-gated by MobileAPI toggle.

---

## Home & Utility Pages

**Controller:** `HomeController`  
**Route:** `/Home/{action}`

### Pages

#### Home/Index (`/Home/Index`)
- **Description:** Default application home page (may be used as a splash/landing page).

#### Privacy Policy (`/Home/Privacy`)
- **Description:** Privacy policy page.

#### Error Page (`/Home/Error`)
- **Description:** Generic error page displaying the Request ID for debugging.

---

## Component: Sidebar Navigation

**File:** `Components/SidebarNavViewComponent.cs`

- **Description:** A view component that renders the sidebar navigation for the staff portal. Dynamically generates navigation items based on the user's role, permissions, and module access. Integrates with the Dashboard's Module Explorer to show only accessible modules and actions.

---

## Summary

| Category | Controller | Key Pages |
|---|---|---|
| Auth | AccountController | Login, Register, Logout, LicenseExpired |
| Dashboard | DashboardController | Overview, Admin, Patient, Appointment, Billing |
| Patient | PatientController | List, Create, Details, Edit, Export |
| Appointment | AppointmentController | List, Calendar, Create, Details, Export |
| OPD | OPDController | List, Details, Create, Edit |
| IPD | IPDController | Admissions, Details, Create, Discharge |
| Billing | BillingController | Dashboard, Create, Details, Payment, Receipt, Export |
| Prescription | PrescriptionController | List, Details, Create, Medicines |
| Lab | LabController | Tests, Results, Create/Edit/Delete |
| Radiology | RadiologyController | Tests, Results, Create/Edit |
| Blood Bank | BloodBankController | Inventory, Issue Blood |
| Operation Theatre | OperationTheatreController | Schedule, Create, Update Status |
| Bed Management | BedManagementController | Overview, Assign, Update Status |
| Ambulance | AmbulanceController | Vehicles, Dispatches, Dispatch |
| Front Office | FrontOfficeController | Dashboard, Visitors, Complaints |
| Staff | StaffController | List, Create, Details, Edit |
| Attendance | AttendanceController | Records, Check In/Out |
| Leave | LeaveController | Requests, Types, Balances |
| Payroll | PayrollController | Records, Generate |
| Certificates | CertificateController | List, Birth, Death, Generate |
| Referral | ReferralController | List, Create, Status |
| Reports | ReportController | Dashboard, Dept/Financial/Occupancy/Staff/Payroll Reports |
| Messaging | MessagingController | Inbox, Sent, Compose, Broadcast |
| Inventory | InventoryController | Items, Transactions, Low Stock |
| Download Center | DownloadCenterController | Browse, Upload |
| Live Consultation | LiveConsultationController | Consultations, Schedule |
| Birth/Death | BirthDeathController | Birth Records, Death Records |
| TPA | TpaController | Providers, Claims |
| Chatbot | ChatbotController | Interface |
| Chatbot Admin | ChatbotAdminController | Settings |
| CMS | CmsController | Pages, Notices, Menu, Notification Settings, Delivery Logs |
| Site (Public) | SiteController | Home, About, Contact, Location, Notices, Doctors, Book Appointment |
| Patient Portal | Area: PatientPortal | Dashboard, Appointments, Bills, Medical Records, Settings |
| System Mgmt | SystemManagementController | Report Mgmt, Themes, Role Permissions, Hospital Settings |
| License | LicenseController | License Management |
| Notifications | NotificationsController | List, Mark Read |
| Audit | AuditController | Logs, Details |
| Module Mgmt | ModuleManagementController | System Modules, User Module Map |
| Account Approvals | AccountsApprovalController | Approval Requests |
| Public Site Admin | PublicSiteAdminController | Settings |
| Mobile API | AppController | API v1, API v2 Config |

**Total Controllers:** 40 (38 MVC + 2 API)  
**Total Distinct Pages/Views:** ~180+
