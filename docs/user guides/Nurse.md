# User Guide — Nurse

**Role:** Nurse  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-06-24  

---

## Overview

Nurses are primarily responsible for inpatient care within the IPD (Inpatient Department). Their Medyx HMS access covers patient monitoring during admission, clinical note documentation, and coordination with doctors for orders and results. Nurses also support basic front-office tasks during their shifts.

**Key responsibilities:**
- Monitoring and documenting IPD patient status
- Adding nursing notes and care observations
- Tracking doctor orders (lab, radiology, medication)
- Assisting with patient admission and discharge preparation

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **Nurse** when prompted to choose a role.
4. Click **Sign In**. You are directed to the IPD/nursing dashboard.

> Your account must be approved by Admin or SuperAdmin before you can log in.

---

## 2. Nursing Dashboard

Your dashboard displays:

| Widget | Description |
|--------|-------------|
| Active IPD Patients | Patients currently admitted and under care |
| Patients by Ward | Breakdown of admissions by ward |
| Pending Doctor Orders | Lab/radiology/medication orders awaiting action |
| Low-Stock Alerts | Pharmacy stock alerts relevant to nursing |

---

## 3. IPD — Inpatient Department

Navigate to **IPD** from the navigation.

### Viewing Admitted Patients
- The IPD list shows all currently admitted patients across wards.
- Filter by **Ward** or **Bed** to view your assignment area.
- Click any patient row to open the full admission record.

### IPD Admission Record Contents
| Section | What You Can See |
|---------|-----------------|
| Patient Details | Name, age, gender, blood group, emergency contact |
| Admission Info | Admitting doctor, admission date, ward, bed number |
| Diagnosis | Primary and secondary diagnoses |
| Clinical Notes | All notes (doctor + nursing) in chronological order |
| Doctor Orders | Active lab, radiology, and medication orders |
| Vital Signs | Recorded observations (temperature, BP, pulse, etc.) |
| Billing Summary | Accumulated charges (view only) |

### Adding a Nursing Note
1. Open the patient's IPD admission record.
2. Click **Add Clinical Note**.
3. Select the note type: **Nursing Observation**, **Handover Note**, or **Progress Note**.
4. Enter date/time and the note content.
5. Click **Save Note**. The note is added to the chronological care record.

### Recording Vital Signs
1. Open the IPD admission and click **Add Vital Signs**.
2. Enter:
   - Temperature (°C / °F)
   - Blood Pressure (systolic / diastolic mmHg)
   - Pulse rate (bpm)
   - Respiratory rate
   - SpO₂ (%)
   - Weight and height (if required)
3. Click **Save**. Vitals are timestamped and appended to the monitoring history.

### Viewing Doctor Orders
1. Open the admission record and click the **Orders** tab.
2. Active lab orders, radiology orders, and medication orders are listed with their status.
3. Mark a medication order as **Administered** once given:
   - Click the order row.
   - Click **Mark Administered**.
   - Enter the administration time and the nurse's initials.

---

## 4. Monitoring Lab & Radiology Orders

You can view the status of diagnostic orders placed by doctors:

| Status | Meaning |
|--------|---------|
| Ordered | Test/exam ordered, awaiting sample or scheduling |
| In Progress | Sample received / exam in progress |
| Result Available | Result entered by Lab/Radiology |
| Critical | Result flagged as critical — notify doctor immediately |

When a critical result appears:
1. Note the flagged order in the patient's record.
2. Notify the attending doctor immediately.
3. Add a nursing note documenting the notification.

---

## 5. Discharge Preparation

When a doctor initiates a patient discharge:
1. You will see the admission status change to **Discharge Pending**.
2. Prepare the discharge package:
   - Confirm all pending medication orders are cleared.
   - Ensure final vital signs are recorded.
   - Confirm the discharge summary note is available.
3. Assist the patient with departure procedures.
4. The final bill is generated automatically by the system after the doctor confirms discharge.

---

## 6. Ward & Bed Management

Navigate to **Bed Management** from the navigation (or through the sidebar).

Nurses have full operational access to the Bed Management module:

### Viewing the Bed Map
- The bed grid shows all rooms and wards with real-time status: **Available** (green), **Occupied** (red), **Cleaning** (yellow), **Maintenance** (grey), **ICU** / **Isolation** flagged.
- Click any bed icon to view occupant details or bed status.

### Assigning a Patient to a Bed
1. Right-click the target bed (or use the **Assign** action).
2. Search for and select the patient.
3. Confirm. The bed status updates to **Occupied** immediately.

> ICU bed assignment requires Admin approval.

### Releasing a Bed
1. Open the occupied bed record.
2. Click **Release**. The bed status moves to **Cleaning** automatically.
3. Once cleaning is confirmed, status returns to **Available**.

### Transferring a Patient
1. Open the currently occupied bed.
2. Click **Transfer**.
3. Select the destination bed (must be **Available**).
4. Confirm. The source bed is released and the destination bed is updated.

---

## 7. Appointments (View)

Navigate to **Appointments** from the navigation:
- View the appointment list for the ward/clinic area (read only).
- You do not create or modify appointments — this is handled by Receptionists and Admins.

---

## 8. Prescriptions (View)

Navigate to **Prescriptions** from the navigation:
- View active prescriptions for admitted patients.
- Use this to cross-reference medication orders with dispensed medicines.
- You cannot create or modify prescriptions — this is the Doctor's responsibility.

---

## 9. AI Assistant

The floating AI button (bottom-right corner):
- Ask for help with module navigation or workflow questions.
- Get guidance on how to record vital signs, add notes, or track orders.
- Chatbot answers are grounded in approved help content.

---

## 10. Account & Profile

### Changing Your Password
1. Click your name/avatar in the top-right.
2. Select **My Profile**.
3. Click **Change Password**.
4. Enter your current password, new password, and confirm.
5. Click **Save**.

---

## 11. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**. You are redirected to the login page.

---

## Quick Reference

| Task | Where |
|------|-------|
| View admitted patients | IPD |
| Add a nursing note | IPD → Open Admission → Add Clinical Note |
| Record vital signs | IPD → Open Admission → Add Vital Signs |
| View doctor orders | IPD → Open Admission → Orders tab |
| Mark medication administered | IPD → Orders tab → Mark Administered |
| View active prescriptions | Prescriptions |
| Check lab/radiology status | IPD → Orders tab or Lab / Radiology |
