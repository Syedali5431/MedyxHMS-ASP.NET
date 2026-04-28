# User Guide — Lab Technician

**Role:** LabTechnician  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-04-28  

---

## Overview

Lab Technicians manage the pathology / laboratory workflow in Medyx HMS. They receive test orders placed by doctors, process samples, enter results, and make the results available to requesting doctors and patients. Lab Technicians also manage the pathology test catalogue and maintain the lab's billing records.

**Key responsibilities:**
- Processing pathology test orders
- Entering and validating lab test results
- Managing the pathology test catalogue
- Flagging critical results for doctor attention
- Supporting lab billing

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **LabTechnician** when prompted.
4. Click **Sign In**. You are directed to the Laboratory module.

---

## 2. Lab Dashboard

Your dashboard shows:

| Widget | Description |
|--------|-------------|
| Pending Orders | Test orders awaiting sample processing |
| In Progress | Orders where sample has been received but result not yet entered |
| Results Ready | Orders completed and available for doctor review |
| Critical Results | Results flagged as abnormal or critical |
| Today's Tests | Total test count for today |

---

## 3. Pathology Test Orders

Navigate to **Lab** from the navigation.

### Viewing the Order Queue
- The queue lists all pending test orders from doctors.
- Each row shows: Patient Name, Test Name, Ordering Doctor, Order Date/Time, Urgency (Routine / Urgent / STAT), and Status.

| Order Status | Meaning |
|--------------|---------|
| Ordered | Doctor has placed the order; sample not yet received |
| Sample Received | Sample collected and logged |
| In Progress | Processing underway in the lab |
| Result Available | Result entered and available |
| Critical | Result is outside normal range — requires urgent notification |

### Receiving a Sample
1. Click on a **Pending** order.
2. Click **Receive Sample**.
3. Enter:
   - Sample collection date/time
   - Sample type (blood, urine, stool, tissue, etc.)
   - Sample ID / barcode (if applicable)
   - Collected by (your name / ID)
4. Click **Save**. Status changes to **Sample Received**.

### Entering a Test Result
1. Open the order (status: Sample Received or In Progress).
2. Click **Enter Result**.
3. For each test parameter:
   - Enter the measured value.
   - The system automatically compares the value against the reference range.
   - Values outside the reference range are highlighted in **red** (high) or **blue** (low).
4. Add any interpretive remarks or technical notes.
5. If any value is critically abnormal, check the **Mark as Critical** checkbox.
6. Click **Submit Result**.
7. Status changes to **Result Available** (or **Critical** if flagged).

### Critical Results
When a result is marked as critical:
1. The ordering doctor is notified automatically by the system (in-app notification + email if configured).
2. You should also contact the doctor directly by phone or in person for STAT communication.
3. Add a note to the order documenting when and how you notified the doctor.

### Printing a Lab Report
1. Open the completed order.
2. Click **Print / Download Report** to generate a PDF lab report.
3. The report includes: patient details, test name, result values, reference ranges, and your lab's letterhead (as configured in settings).

---

## 4. Pathology Test Catalogue

Navigate to **Lab → Test Catalogue** (or **Settings → Pathology Setup**).

### Adding a New Test
1. Click **Add Test**.
2. Enter:
   - **Test Name** (e.g., Complete Blood Count)
   - **Short Code** (e.g., CBC)
   - **Category** (Haematology, Biochemistry, Microbiology, etc.)
   - **Sample Type** (Blood, Urine, Stool, Swab, etc.)
   - **Report Template** (which parameters to capture)
   - **Reference Ranges** per parameter (normal low / normal high, with units)
   - **Price** (for billing)
   - **Turnaround Time** (TAT) in hours
3. Click **Save**.

### Adding Test Parameters
Within a test, you can add multiple parameters (e.g., for CBC: Haemoglobin, WBC, Platelets):
1. Click **Add Parameter**.
2. Enter: parameter name, unit (g/dL, 10³/µL, etc.), reference range (min/max), and critical range (min/max).
3. Save.

### Editing a Test
1. Find the test in the catalogue and click **Edit**.
2. Modify price, reference ranges, or TAT as needed.
3. Save.

---

## 5. Lab Billing

### Automatic Billing
- When a test order is placed, a lab bill line item is automatically added to the patient's bill.
- You do not need to manually create billing entries in most cases.

### Viewing Lab Bills
- Navigate to **Billing** (Pathology section) to see lab-related bill entries.
- Filter by patient or date to locate specific records.

---

## 6. Reports

Navigate to **Reports** from the navigation. Lab-relevant reports:

| Report | Description |
|--------|-------------|
| Pathology Patient Report | All tests performed for a patient within a date range |
| Lab Order Report | Volume of orders by test type and period |
| Pending Orders Report | Orders not yet resulted |

### Running a Report
1. Select the report type.
2. Set filters (date range, test type, patient, etc.).
3. Click **Generate** and export as CSV or PDF.

---

## 7. Patient Portal Integration

When a result is marked **Result Available**:
- The patient can view the result from their **Patient Portal → Medical Records → Lab Results** section.
- They can also download the PDF lab report from the portal.
- You do not need to take any additional action for portal visibility.

---

## 8. AI Assistant

The floating AI button (bottom-right):
- Ask for help with order processing, result entry, or catalogue management.
- Chatbot answers are grounded in approved help content.

---

## 9. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**.

---

## 10. Messaging

Navigate to **Messaging** from the navigation.

- Send messages to doctors regarding critical results, sample queries, or test order clarifications.
- View inbox and conversation threads.

---

## 11. Download Center

Navigate to **Download Center** from the navigation.

- Access lab reference ranges, test procedure guides, and accreditation documents.

---

## Quick Reference

| Task | Where |
|------|-------|
| View pending test orders | Lab (queue) |
| Receive a sample | Lab → Open Order → Receive Sample |
| Enter a result | Lab → Open Order → Enter Result |
| Flag a critical result | Enter Result → Mark as Critical |
| Print a lab report | Lab → Open Completed Order → Print Report |
| Add a test to catalogue | Lab → Test Catalogue → Add Test |
| Run pathology report | Reports → Pathology Patient Report |
| Send a message | Messaging → New Message |
| Download lab references | Download Center |
