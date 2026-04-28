# User Guide — Pharmacist

**Role:** Pharmacist  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-04-28  

---

## Overview

Pharmacists manage the hospital pharmacy in Medyx HMS. Their core workflow revolves around dispensing prescribed medicines, managing the medicine catalogue, monitoring stock levels, and generating pharmacy-related bills. Pharmacists also receive and act on low-stock and expiry alerts.

**Key responsibilities:**
- Reviewing and dispensing prescriptions
- Managing medicine stock (receive, issue, adjust)
- Maintaining the medicine catalogue and pricing
- Monitoring expiry dates and low-stock thresholds
- Generating pharmacy bills and receipts

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **Pharmacist** when prompted to choose a role.
4. Click **Sign In**. You are directed to `/Prescription` (the pharmacy workflow screen).

---

## 2. Pharmacy Dashboard

Your dashboard shows:

| Widget | Description |
|--------|-------------|
| Pending Prescriptions | Prescriptions waiting to be dispensed |
| Low Stock Alerts | Medicines below the minimum threshold |
| Expiry Alerts | Medicines expiring within the next 30 days |
| Today's Dispensed | Count of prescriptions dispensed today |
| Revenue Today | Pharmacy billing total for today |

---

## 3. Prescription Management

Navigate to **Prescriptions** from the navigation.

### Viewing the Prescription Queue
- The queue lists all prescriptions issued by doctors that have not yet been dispensed.
- Each row shows: Patient Name, Prescribing Doctor, Date, Number of Items, and Status.

| Status | Meaning |
|--------|---------|
| Pending | Awaiting dispensing |
| Partially Dispensed | Some items dispensed, some pending |
| Dispensed | All items dispensed and billed |
| Cancelled | Prescription voided by doctor or admin |

### Dispensing a Prescription
1. Click on a **Pending** prescription to open it.
2. Review the list of prescribed medicines:
   - Medicine name
   - Dosage and form
   - Quantity to dispense
   - Instructions
3. For each medicine:
   - Confirm the quantity in stock is sufficient.
   - Click **Dispense** or tick the checkbox next to each item.
   - If a medicine is out of stock, mark it as **Not Available** and note a substitution if applicable.
4. Click **Complete Dispensing**.
5. The prescription status updates to **Dispensed**.
6. A pharmacy bill is automatically generated for the dispensed items.

### Partial Dispensing
- If only some items are available, dispense available items and save.
- The prescription moves to **Partially Dispensed** status.
- Complete the remaining items when stock is replenished.

### Printing a Prescription Label
- After dispensing, click **Print Label** to generate a medicine label with dosage instructions.

---

## 4. Medicine Catalogue

Navigate to **Prescriptions → Medicine Catalogue** (or **Settings → Pharmacy Setup**).

### Adding a New Medicine
1. Click **Add Medicine**.
2. Fill in:
   - Generic name
   - Brand name(s)
   - Category (antibiotic, analgesic, etc.)
   - Form (tablet, capsule, syrup, injection, etc.)
   - Unit (strip, bottle, vial, etc.)
   - Strength / dosage (e.g., 500mg)
   - Minimum stock threshold (triggers low-stock alert)
   - Selling price
3. Click **Save**.

### Editing a Medicine
1. Find the medicine in the catalogue list.
2. Click **Edit**, modify the required fields, and save.

### Deactivating a Medicine
- If a medicine is discontinued, click **Deactivate** to remove it from the prescribing list without deleting historical records.

---

## 5. Stock Management

Navigate to **Prescriptions → Stock** (or **Inventory → Pharmacy**).

### Receiving Stock (Purchase / Replenishment)
1. Click **Receive Stock**.
2. Select the medicine from the catalogue.
3. Enter:
   - Supplier name (optional)
   - Quantity received
   - Batch number
   - Manufacturing date
   - Expiry date
   - Purchase price per unit
4. Click **Save**. The received quantity is added to the current stock level.

### Issuing / Adjusting Stock
- Stock is automatically reduced when a prescription is dispensed.
- For manual adjustments (damaged, expired, or lost stock):
  1. Click **Adjust Stock**.
  2. Select the medicine and enter the quantity adjustment (positive = add, negative = reduce).
  3. Select the reason: Damaged, Expired, Lost, Correction.
  4. Save. The adjustment is logged to the audit trail.

### Viewing Stock Levels
- The stock list shows all medicines with current quantity, minimum threshold, and expiry information.
- Filter by category or search by name.
- **Red rows** indicate stock is below the minimum threshold.
- **Orange rows** indicate medicine will expire within 30 days.

---

## 6. Expiry Management

Navigate to **Prescriptions → Expiry Alerts** (or via the dashboard widget).

- View all medicines with an upcoming expiry date.
- Filter by expiry period: next 7 days, 30 days, 60 days, or 90 days.
- For each expiring batch:
  1. Pull the affected stock from dispensing.
  2. Record a stock adjustment with reason **Expired**.
  3. Coordinate with Admin/procurement for replacement stock.

---

## 7. Pharmacy Billing

Navigate to **Billing** (Pharmacy section) or from within a dispensed prescription.

### Viewing Pharmacy Bills
- All pharmacy bills are listed with patient name, prescription date, items, total, and payment status.

### Processing Pharmacy Payments
1. Open an unpaid pharmacy bill.
2. Click **Pay**.
3. Select the payment method (Cash, Card, Insurance, etc.).
4. Enter the amount.
5. Click **Process Payment**.

### Generating a Pharmacy Receipt
- After payment, click **Print Receipt** to generate a PDF receipt for the patient.

---

## 8. Reports

Navigate to **Reports** from the navigation. Pharmacy-relevant reports include:

| Report | Description |
|--------|-------------|
| Pharmacy Balance Report | Outstanding pharmacy bills and collection summary |
| Expiry Medicine Report | Medicines expiring by date range |
| Inventory Stock Report | Current stock levels across all medicines |
| Inventory Issue Report | All stock issue and dispensing transactions |

To run:
1. Select the report type.
2. Set the date range.
3. Click **Generate** and export as CSV or PDF.

---

## 9. AI Assistant

The floating AI button (bottom-right):
- Ask for help with workflow questions (e.g., "How do I receive new stock?").
- Get guidance on prescription dispensing and stock management.

---

## 10. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**.

---

## 11. Inventory Management

Navigate to **Inventory** from the navigation (if licensed and access granted).

- Manage general medical supply stock (consumables, equipment, non-medicine items).
- Create purchase orders, receive deliveries, and log usage.
- Monitor reorder levels and stock alerts.
- Differs from the Pharmacy Stock module — Inventory covers non-medicine supplies.

---

## 12. Messaging

Navigate to **Messaging** from the navigation.

- Send direct messages to doctors, nurses, or admins about prescription or stock queries.
- View your inbox and conversation threads.

---

## 13. Download Center

Navigate to **Download Center** from the navigation.

- Access and download pharmacy reference materials, dosage guidelines, and policy documents.

---

## Quick Reference

| Task | Where |
|------|-------|
| View pending prescriptions | Prescriptions (queue) |
| Dispense a prescription | Prescriptions → Open → Complete Dispensing |
| Add a new medicine | Pharmacy Setup → Add Medicine |
| Receive stock | Stock → Receive Stock |
| View expiry alerts | Dashboard widget or Expiry Alerts |
| Process pharmacy payment | Billing → Open Bill → Pay |
| Run pharmacy report | Reports → Pharmacy Balance / Expiry / Stock |
| Manage inventory supplies | Inventory |
| Send a message | Messaging → New Message |
| Download pharmacy forms | Download Center |
