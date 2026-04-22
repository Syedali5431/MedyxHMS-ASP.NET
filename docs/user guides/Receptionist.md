# User Guide — Receptionist

**Role:** Receptionist  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-04-22  

---

## Overview

Receptionists are the front-line operational staff in Medyx HMS. They handle patient registration, appointment scheduling, visitor management, and basic front-office administration. The Receptionist role is redirected to `/FrontOffice` after login and has access to the modules needed for daily patient-facing operations.

**Key responsibilities:**
- Registering new patients
- Booking and managing appointments
- Handling front-office operations (visitors, complaints, dispatch)
- Birth and death record registration
- Ambulance dispatch tracking
- Bed assignment assistance

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **Receptionist** when prompted.
4. Click **Sign In**. You are directed to `/FrontOffice`.

---

## 2. Front Office Dashboard

Your dashboard shows:

| Widget | Description |
|--------|-------------|
| Today's Appointments | Appointments booked for today |
| Pending Registrations | New patients not yet fully registered |
| Visitors In | Visitors currently inside the facility |
| Complaints Open | Unresolved complaints |
| Beds Available | Current available bed count |

---

## 3. Patient Registration

Navigate to **Patients** from the navigation.

### Registering a New Patient
1. Click **Add New Patient**.
2. Fill in the registration form:

| Field | Required? | Notes |
|-------|-----------|-------|
| Full Name | Yes | First + Last name |
| Date of Birth | Yes | Used to calculate age |
| Gender | Yes | |
| Contact Number | Yes | Primary phone |
| Email Address | No | Used for notifications |
| Address | Yes | |
| Blood Group | Recommended | |
| Emergency Contact Name | Recommended | |
| Emergency Contact Number | Recommended | |
| Insurance / TPA Details | If applicable | Insurer name + policy number |
| `user_name` | Yes (unique) | For patient portal access |

3. A numeric Patient ID is assigned automatically.
4. Click **Save**. The patient account is created.

> **Note:** If the patient wants Patient Portal access, share their `user_name` and a temporary password. Advise them to log in at the Patient Portal and change their password.

### Searching for an Existing Patient
- Use the **Search** bar at the top of the patient list.
- Search by name, patient ID, phone number, or email.
- Click the patient row to open their profile.

---

## 4. Appointment Booking

Navigate to **Appointments** from the navigation.

### Booking a New Appointment
1. Click **New Appointment**.
2. Search for and select the patient (by name or patient ID).
3. Select the **Doctor**.
4. Choose the **Date** and **Time Slot** from the available slots.
5. Select the appointment **Type** (OPD consultation, follow-up, specialist, etc.).
6. Add any notes from the patient.
7. Click **Save**. The appointment is created in **Pending** status.

### Confirming an Appointment
- Once the doctor or admin confirms, the status changes to **Confirmed**.
- The patient receives an email + SMS notification automatically.

### Rescheduling
1. Open the appointment and click **Reschedule**.
2. Choose the new date and time slot.
3. Confirm. The patient is notified of the change.

### Cancelling an Appointment
1. Open the appointment and click **Cancel**.
2. Enter a cancellation reason.
3. Confirm. The slot becomes available for rebooking.

### Walk-in Patients
For unscheduled arrivals:
1. Register the patient if they are new.
2. Create a new appointment with today's date and the earliest available slot.
3. Mark it as **Confirmed** immediately (for walk-ins, no waiting for doctor confirmation needed, depending on hospital policy).

---

## 5. Bed & Ward Management

Navigate to **IPD → Beds** or through the Bed Management panel.

### Viewing Available Beds
- The bed map shows all wards with bed status: **Available** (green), **Occupied** (red), or **Maintenance** (grey).
- Click any bed to see current occupant or bed details.

### Assigning a Bed
1. Navigate to the active IPD admission (usually created by a doctor).
2. If no bed is assigned, click **Assign Bed**.
3. Select the ward and available bed from the list.
4. Confirm assignment.

> Bed assignment is done by Receptionists and Admins. Doctors initiate the admission, but Receptionists typically assign the physical bed.

---

## 6. Front Office Operations

Navigate to **Front Office** from the navigation.

### Visitor Registration
1. Click **New Visitor**.
2. Enter:
   - Visitor name and contact number
   - Purpose of visit
   - Patient they are visiting (search by name/ID)
   - Entry time (auto-filled)
3. Click **Save**.
4. When the visitor leaves, open the record and click **Mark Exit** to record the exit time.

### Complaints Management
1. Click **New Complaint** to log a patient or visitor complaint.
2. Enter:
   - Complainant name and contact
   - Nature of complaint
   - Department or staff involved
   - Priority (Low / Medium / High)
3. Save. The complaint is assigned a reference number.
4. Follow up by updating the complaint status (Under Review → Resolved) as it progresses.

### Dispatch (Outgoing)
1. Click **New Dispatch** to log an outgoing item or document.
2. Enter: item description, destination, sent by, sent to, and dispatch date/time.
3. Save.

### Receive (Incoming)
1. Click **New Receive** to log incoming correspondence or items.
2. Enter: item description, sender, received by, and receipt date/time.
3. Save.

---

## 7. Birth & Death Records

Navigate to **Front Office → Birth Records** or **Death Records**.

### Registering a Birth
1. Click **New Birth Record**.
2. Enter:
   - Baby's date/time of birth
   - Gender
   - Birth weight
   - Mother's patient record (link by name/ID)
   - Attending doctor
   - Delivery type (normal, C-section, etc.)
3. Save. A birth certificate can be generated from **Certificates → Birth Certificate**.

### Registering a Death
1. Click **New Death Record**.
2. Enter:
   - Patient name and record (link)
   - Date/time of death
   - Cause of death
   - Attending doctor
   - Next of kin contact
3. Save. A death certificate can be generated from **Certificates → Death Certificate**.

---

## 8. Ambulance / Transport

Navigate to **Front Office → Ambulance** from the navigation.

### Logging an Ambulance Dispatch
1. Click **New Dispatch**.
2. Enter:
   - Vehicle/ambulance ID
   - Driver name and contact
   - Patient name or pickup location
   - Dispatch date/time
   - Destination
3. Save.

### Logging Return
1. Open the dispatch record and click **Mark Returned**.
2. Enter the return date/time.
3. Save.

---

## 9. Referrals

Navigate to **Referrals** from the navigation.

### Creating a Referral
1. Click **New Referral**.
2. Select the patient.
3. Select the referring doctor and the referral destination (internal department or external specialist).
4. Add referral notes.
5. Save.

---

## 10. Reports

Navigate to **Reports** from the navigation. Receptionist-relevant reports:

| Report | Description |
|--------|-------------|
| Appointment Report | All appointments by date range |
| Patient Visit Report | Patient visit frequency and history |
| Birth Report | Registered births by date range |
| Death Report | Registered deaths by date range |
| Ambulance Report | Ambulance dispatch history |

---

## 11. AI Assistant

The floating AI button (bottom-right):
- Ask for help with appointment booking, patient registration, or front office workflows.

---

## 12. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**.

---

## Quick Reference

| Task | Where |
|------|-------|
| Register a new patient | Patients → Add New Patient |
| Book an appointment | Appointments → New Appointment |
| Log a visitor | Front Office → New Visitor |
| Log a complaint | Front Office → New Complaint |
| Assign a bed | IPD → Beds or open IPD admission |
| Register a birth | Front Office → Birth Records → New |
| Register a death | Front Office → Death Records → New |
| Log ambulance dispatch | Front Office → Ambulance → New Dispatch |
| Run appointment report | Reports → Appointment Report |
