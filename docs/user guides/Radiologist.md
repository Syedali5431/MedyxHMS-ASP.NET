# User Guide — Radiologist

**Role:** Radiologist  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-06-24  

---

## Overview

Radiologists manage the radiology/imaging workflow in Medyx HMS. They receive imaging orders placed by doctors, schedule examinations, upload image studies, write radiology reports, and make findings available to the requesting doctor and patient. Radiologists also manage the radiology examination catalogue.

**Key responsibilities:**
- Processing radiology imaging orders
- Scheduling and performing examinations
- Writing and submitting radiology reports
- Managing the radiology examination catalogue
- Uploading and linking imaging files (where supported)
- Supporting radiology billing

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **Radiologist** when prompted.
4. Click **Sign In**. You are directed to the Radiology module.

---

## 2. Radiology Dashboard

Your dashboard shows:

| Widget | Description |
|--------|-------------|
| Pending Orders | Imaging orders awaiting scheduling or examination |
| Scheduled | Examinations scheduled but not yet performed |
| Awaiting Report | Examinations done; report not yet written |
| Reports Submitted | Completed reports available for doctor review |
| Urgent / STAT | High-priority orders requiring immediate attention |
| Today's Volume | Total exams performed today |

---

## 3. Radiology Orders

Navigate to **Radiology** from the navigation.

### Viewing the Order Queue
- The queue lists all imaging orders submitted by doctors.
- Each row shows: Patient Name, Examination Type, Ordering Doctor, Order Date/Time, Urgency, and Status.

| Order Status | Meaning |
|--------------|---------|
| Ordered | Order placed; not yet scheduled |
| Scheduled | Examination appointment set |
| Performed | Examination completed; report pending |
| Report Submitted | Radiology report written and available |
| Critical | Significant finding requiring urgent communication |

### Scheduling an Examination
1. Click on a **Pending** order.
2. Click **Schedule Exam**.
3. Set the examination date and time.
4. Assign the radiology suite / machine if applicable.
5. Click **Save**. Status changes to **Scheduled**.
6. The patient (and referring doctor) are notified of the scheduled date/time.

### Marking as Performed
After the examination is done:
1. Open the **Scheduled** order.
2. Click **Mark as Performed**.
3. Enter: actual date/time performed, equipment used, and the technologist who performed it.
4. Click **Save**. Status changes to **Performed / Awaiting Report**.

---

## 4. Writing a Radiology Report

### Submitting a Report
1. Open a **Performed** order.
2. Click **Write Report**.
3. Complete the report form:

| Section | Description |
|---------|-------------|
| Clinical Information | Summary of the clinical indication (from the order) |
| Examination Type | Confirm X-Ray, CT, MRI, Ultrasound, etc. |
| Technique | Imaging technique used (contrast, plane, sequences, etc.) |
| Findings | Detailed description of all imaging findings |
| Impression / Conclusion | Your diagnostic conclusions |
| Recommendation | Follow-up actions, additional imaging, urgent referral if needed |
| Critical Finding | Check this box if an unexpected critical finding is present |

4. Click **Submit Report**.
5. Status changes to **Report Submitted**.
6. The ordering doctor is notified that the report is available.

### Critical Findings
When a critical (significant unexpected) finding is identified:
1. Check **Critical Finding** when submitting the report.
2. The system flags the order as **Critical** and sends an urgent notification to the ordering doctor.
3. Contact the ordering doctor directly (phone or in person) immediately.
4. Document the notification in the report or add a follow-up note.

### Amending a Report
If you need to correct a submitted report:
1. Open the completed order.
2. Click **Amend Report**.
3. Modify the report content and add an amendment note explaining the change.
4. Resubmit. The amended version is saved with an amendment timestamp.

### Printing / Downloading the Report
1. Open the completed order.
2. Click **Print / Download Report** to generate a PDF.
3. The PDF includes: patient details, examination type, date performed, findings, impression, and radiologist name/signature.

---

## 5. Radiology Examination Catalogue

Navigate to **Radiology → Examination Catalogue** (or **Settings → Radiology Setup**).

### Adding a New Examination Type
1. Click **Add Examination**.
2. Enter:
   - **Examination Name** (e.g., Chest X-Ray, CT Head with Contrast)
   - **Short Code** (e.g., CXR, CTHEAD)
   - **Modality** (X-Ray, CT, MRI, Ultrasound, PET, etc.)
   - **Body Part / Region**
   - **Contrast Required** (Yes / No)
   - **Standard Preparation Instructions** (e.g., fasting requirements)
   - **Price**
   - **Typical Turnaround Time** (hours)
3. Click **Save**.

### Editing an Examination
1. Find the examination in the catalogue list.
2. Click **Edit**, modify required fields, and save.

### Deactivating
- If an exam type is no longer offered, click **Deactivate** to prevent new orders being placed without deleting historical records.

---

## 6. Radiology Billing

### Automatic Billing
- When a radiology order is placed, a billing line item is automatically added to the patient's bill at the examination price.
- No manual billing action is required from the Radiologist in most cases.

### Viewing Radiology Bills
- Navigate to **Billing** (Radiology section) to view radiology-related bill entries.

---

## 7. Reports

Navigate to **Reports** from the navigation. Radiology-relevant reports:

| Report | Description |
|--------|-------------|
| Radiology Patient Report | All exams performed for a patient within a date range |
| Radiology Order Volume | Count of orders by examination type and period |
| Pending Radiology Report | Orders not yet reported |

### Running a Report
1. Select the report type.
2. Set date range and filters.
3. Click **Generate** and export as CSV or PDF.

---

## 8. Patient Portal Integration

When a radiology report is submitted:
- The patient can view their report from the **Patient Portal → Medical Records → Radiology Reports** section.
- They can also download a PDF of the report.
- No additional action is needed from you for portal visibility.

---

## 9. AI Assistant

The floating AI button (bottom-right):
- Ask for help navigating the radiology module or workflow questions.
- Chatbot answers are grounded in approved help content.

---

## 10. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**.

---

## 11. Messaging

Navigate to **Messaging** from the navigation.

- Send messages to referring doctors about imaging findings or scheduling queries.
- View inbox and conversation threads.

---

## 12. Download Center

Navigate to **Download Center** from the navigation.

- Access radiology protocol guides, imaging reference materials, and department policies.

---

## Quick Reference

| Task | Where |
|------|-------|
| View pending imaging orders | Radiology (queue) |
| Schedule an examination | Radiology → Open Order → Schedule Exam |
| Mark examination performed | Radiology → Open Scheduled → Mark Performed |
| Write a radiology report | Radiology → Open Performed → Write Report |
| Flag critical finding | Write Report → Check Critical Finding |
| Print/download report | Radiology → Open Completed → Print Report |
| Add exam to catalogue | Radiology → Examination Catalogue → Add |
| Send a message | Messaging → New Message |
| Download radiology protocols | Download Center |
| Run radiology report | Reports → Radiology Patient Report |
