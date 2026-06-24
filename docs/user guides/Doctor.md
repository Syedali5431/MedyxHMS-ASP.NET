# User Guide — Doctor

**Role:** Doctor  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-06-24  

---

## Overview

Doctors are the primary clinical users of Medyx HMS. After logging in, a Doctor is directed to the clinical workflow area covering outpatient consultations, inpatient management, prescriptions, and diagnostic ordering. Doctors have read access to patient histories, lab results, and radiology reports to support informed clinical decisions.

**Key responsibilities:**

- Managing OPD (outpatient) consultations
- Supervising assigned IPD (inpatient) cases
- Writing and managing prescriptions
- Ordering laboratory and radiology tests
- Reviewing diagnostic results

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **Doctor** when prompted to choose a role.
4. Click **Sign In**. You are directed to the OPD/clinical dashboard.

> Your account must be approved by an Admin or SuperAdmin before you can log in. Newly registered doctor accounts are inactive until approved.

---

## 2. Clinical Dashboard

Your dashboard shows:

| Widget | Description |
| -------- | ------------- |
| Today's Appointments | All appointments scheduled for you today |
| Active OPD Encounters | Open consultations requiring completion |
| Admitted Patients | Your currently admitted IPD patients |
| Pending Lab Results | Lab orders awaiting results |
| Pending Radiology | Radiology orders awaiting results |

---

## 3. Appointments

Navigate to **Appointments** from the navigation.

### Viewing Your Appointments
- The appointment list is filtered to show your appointments by default.
- Use the **Date** and **Status** filters to locate specific appointments.
- Upcoming appointments show patient name, time, type (OPD/IPD), and current status.

### Appointment Statuses
| Status | Meaning |
|--------|---------|
| Pending | Booked but not yet confirmed |
| Confirmed | Confirmed — patient is expected |
| Completed | Visit has been completed |
| Cancelled | Cancelled |
| Rescheduled | Moved to a new time |

### Confirming an Appointment
1. Click on a **Pending** appointment.
2. Review patient details.
3. Click **Confirm** to accept.
4. The patient receives an email + SMS notification.

### Starting a Consultation
When a patient arrives for their appointment:
1. Find the confirmed appointment and click **Start Consultation**.
2. This creates a new OPD Encounter linked to the appointment.
3. You are taken directly to the encounter form.

---

## 4. OPD — Outpatient Consultations

Navigate to **OPD** from the navigation.

### Creating an OPD Encounter
1. Click **New Encounter** (or continue from an appointment).
2. Select or confirm the patient.
3. Complete the consultation form:

| Field | Description |
|-------|-------------|
| Chief Complaint | Patient's primary presenting complaint |
| Symptoms | Detailed symptom description |
| Examination Findings | Your clinical examination observations |
| Diagnosis | Primary and secondary diagnoses |
| Treatment Plan | Planned interventions, procedures, referrals |
| Prescription | Add medicines (or create a separate prescription) |
| Follow-up Date | Next appointment recommendation |
| Notes | Internal clinical notes |

4. Click **Save Encounter**.
5. A consultation bill is automatically generated and linked to the encounter.

### Ordering Diagnostic Tests from OPD
Within an open encounter:
- Click **Order Lab Test** to create a pathology order.
- Click **Order Radiology** to create a radiology order.
- Both orders appear in the Lab and Radiology queues for respective technicians.

### Viewing Previous Encounters
- Open the patient's profile and click the **OPD** tab.
- You can see all past consultations with their details, diagnoses, and outcomes.

---

## 5. IPD — Inpatient Management

Navigate to **IPD** from the navigation.

### Your Admitted Patients
- The IPD list shows all currently admitted patients assigned to you.
- Click on an admission to view full details: admission date, ward, bed, current diagnosis, and daily charge summary.

### Adding Clinical Notes
1. Open an IPD admission.
2. Click **Add Clinical Note**.
3. Enter the date, note type (progress note, consultation, nursing, etc.), and content.
4. Save. Notes are added to the patient's IPD record chronologically.

### Ordering Tests During Admission
- Within an IPD admission, click **Order Lab** or **Order Radiology** to place diagnostic orders.
- Results are linked to the admission record once entered by the technician.

### Discharging a Patient
1. Open the admission and click **Discharge Patient**.
2. Enter: discharge date, final diagnosis, discharge summary, follow-up instructions.
3. Confirm the discharge. All accumulated charges are compiled into a final bill automatically.

---

## 6. Prescriptions

Navigate to **Prescriptions** from the navigation.

### Creating a Prescription
1. Click **New Prescription**.
2. Select the patient.
3. Add medicines:
   - Medicine name (from the approved catalogue)
   - Dosage (e.g., 500mg)
   - Frequency (e.g., twice daily)
   - Duration (e.g., 7 days)
   - Route (oral, IV, topical, etc.)
   - Special instructions
4. Add any additional notes for the pharmacist.
5. Click **Save Prescription**.

### Prescriptions in OPD
Prescriptions created inside an OPD encounter are automatically linked to that encounter.

### Viewing Prescription History
- Open any patient's profile and click the **Prescriptions** tab.
- All past prescriptions with dispensing status are shown.

### Prescription PDF
- Patients can download a PDF of their prescriptions from the Patient Portal.
- You can also print prescriptions directly from the prescription details screen.

---

## 7. Laboratory (Test Orders)

Navigate to **Lab** from the navigation.

### Ordering a Lab Test
1. Click **New Lab Order** (or order from within an OPD/IPD encounter).
2. Select the patient and test type from the approved pathology catalogue.
3. Add clinical notes for the lab technician (urgency, special requirements).
4. Click **Submit Order**.

### Viewing Lab Results
1. Open the lab order.
2. Once a LabTechnician enters the result, the status changes from **Ordered** to **Result Available**.
3. Click **View Result** to see the detailed test report.
4. Critical results are flagged for urgent review.

---

## 8. Radiology (Imaging Orders)

Navigate to **Radiology** from the navigation.

### Ordering a Radiology Exam
1. Click **New Radiology Order** (or order from within an OPD/IPD encounter).
2. Select the patient, examination type (X-Ray, CT, MRI, Ultrasound, etc.), and body part.
3. Add clinical indication and urgency notes.
4. Click **Submit Order**.

### Viewing Radiology Results
1. Open the radiology order.
2. Once the Radiologist enters the report, the status changes to **Report Ready**.
3. Click **View Report** to read the radiologist's findings.

---

## 9. Blood Bank (Requests)

For patients requiring blood:
1. Navigate to **Blood Bank → New Blood Request**.
2. Select the patient, required blood group, quantity (units), and urgency.
3. Submit. Blood bank staff will process the request and issue the blood.

---

## 10. Patient Records

You have read access to all relevant patient information:

| Section | Access |
|---------|--------|
| Demographics | View |
| OPD History | View all encounters |
| IPD History | View all admissions |
| Prescriptions | View all, create new |
| Lab Results | View all, order new |
| Radiology Reports | View all, order new |
| Bills | View only (no edit access) |
| Medical Documents | View |

---

## 11. AI Assistant

The floating AI button (bottom-right corner) provides clinical and workflow guidance:
- Ask about module navigation, clinical workflows, or how to order tests.
- Chatbot answers are grounded in approved help content.
- Medical diagnosis suggestions are **not** provided by the AI — clinical decisions remain entirely yours.
- If the chatbot cannot answer, it offers escalation to a support contact.

---

## 12. Account & Profile

### Updating Your Profile
1. Click your name/avatar in the top-right navigation.
2. Select **My Profile** or **Settings**.
3. Update your name, contact details, or password.

### Changing Your Password
1. Go to Profile → **Change Password**.
2. Enter your current password, then the new password twice.
3. Click **Save**.

---

## 13. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**. You are redirected to the login page.

---

## 14. Live Consultation

Navigate to **Live Consultation** from the navigation (if licensed).

- Schedule a remote consultation session linked to an appointment.
- Start the session at the scheduled time — the patient joins from their Patient Portal.
- All sessions are logged with start/end times for billing and audit purposes.

---

## 15. Messaging

Navigate to **Messaging** from the navigation.

- Send direct messages to colleagues (nurses, admins, pharmacists).
- View your inbox and conversation threads.
- Messages are internal only — not visible to patients.

---

## 16. Download Center

Navigate to **Download Center** from the navigation.

- Access authorized downloads: clinical forms, procedure guides, hospital protocols.
- Download files uploaded by Admin/SuperAdmin.
- Use search or filter by category to find the document you need.

---

## Quick Reference

| Task | Where |
|------|-------|
| See today's appointments | Dashboard or Appointments |
| Start a consultation | Appointments → Start Consultation |
| Create OPD encounter | OPD → New Encounter |
| Write a prescription | Prescriptions → New Prescription |
| Order a lab test | Lab → New Lab Order or from OPD/IPD |
| Order radiology | Radiology → New Radiology Order or from OPD/IPD |
| View lab results | Lab → Open Order → View Result |
| Discharge IPD patient | IPD → Open Admission → Discharge |
| View patient history | Patients → Open Patient → Tabs |
| Start live consultation | Live Consultation → Schedule or Start |
| Send a message | Messaging → New Message |
| Download clinical forms | Download Center |
