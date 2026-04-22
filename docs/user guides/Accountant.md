# User Guide — Accountant

**Role:** Accountant  
**Portal:** Staff / Admin Portal (`/`)  
**Last Updated:** 2026-04-22  

---

## Overview

Accountants in Medyx HMS are responsible for financial operations: billing patients, processing payments, managing income and expense records, reconciling TPA claims, and generating financial reports. The Accountant role has full access to the Billing module but does not have access to clinical workflows (prescriptions, lab, OPD/IPD clinical notes).

**Key responsibilities:**
- Creating and managing patient bills
- Processing payments and generating receipts
- Managing income and expense records
- TPA (Third-Party Administration) reconciliation
- Financial reporting

---

## 1. Logging In

1. Open the Medyx HMS URL in your browser.
2. Enter your email and password.
3. Select **Accountant** when prompted.
4. Click **Sign In**. You are directed to `/Billing`.

---

## 2. Billing Dashboard

Your dashboard shows:

| Widget | Description |
|--------|-------------|
| Total Bills Today | Number of bills created today |
| Revenue Today | Total payments collected today |
| Unpaid Bills | Count of outstanding unpaid bills |
| Overdue Bills | Bills past their expected payment date |
| TPA Claims Pending | Insurance/TPA claims awaiting reconciliation |

---

## 3. Bills Management

Navigate to **Billing** from the navigation.

### Creating a New Bill
1. Click **New Bill**.
2. Search for and select the patient.
3. Optionally link to:
   - An OPD encounter
   - An IPD admission
   - An appointment
4. Add bill line items:
   - **Service Name** (consultation, procedure, test, medicine, bed charge, etc.)
   - **Quantity**
   - **Unit Price**
   - **Discount** (if applicable)
   - **Tax Category** (if applicable)
5. Review the calculated total.
6. Click **Save Bill**.

### Viewing Bills
- The bill list shows all bills with patient name, bill date, total amount, amount paid, and status.
- Filter by: date range, patient, status (Unpaid / Partially Paid / Paid), or bill type.

### Bill Statuses
| Status | Meaning |
|--------|---------|
| Draft | Bill created but not yet sent to patient |
| Unpaid | Bill finalised, payment pending |
| Partially Paid | One or more partial payments received |
| Paid | Bill fully settled |
| Cancelled | Bill voided (audit-logged) |

### Editing a Bill
- Bills in **Draft** or **Unpaid** status can be edited.
- Open the bill and click **Edit**.
- Modify line items, apply discounts, or add additional charges.
- Save changes. All edits are audit-logged.

### Cancelling a Bill
- Open the bill and click **Cancel**.
- Enter the reason for cancellation (required).
- Cancelled bills remain in the system for audit purposes but are excluded from revenue totals.

---

## 4. Processing Payments

### Recording a Payment
1. Open an unpaid or partially paid bill.
2. Click **Pay**.
3. Select the payment method:

| Method | Description |
|--------|-------------|
| Cash | Physical cash payment |
| Card | Debit or credit card |
| Cheque | Bank cheque (enter cheque number) |
| Online | Bank transfer or online payment (enter reference) |
| Insurance / TPA | Insurance payer (attach TPA reference) |

4. Enter the amount received (can be less than total for partial payment).
5. Click **Process Payment**.
6. The bill status updates automatically.

### Multiple Payments on One Bill
- For bills paid in instalments, click **Pay** again to record additional payments.
- Each payment is timestamped and logged separately.
- The bill reaches **Paid** status when the full amount is collected.

---

## 5. Receipts

### Generating a Receipt
- After processing a payment, click **Print Receipt** to generate a PDF receipt.
- The receipt includes: hospital name, patient name, bill number, payment date, items paid, amount, payment method, and operator name.

### Re-printing a Receipt
1. Open the bill and click the **Payments** tab.
2. Find the payment record and click **Print Receipt** next to it.

---

## 6. TPA Management

Navigate to **TPA Management** from the navigation (or via Billing).

### Creating a TPA Bill
1. When processing a bill paid by insurance, select **Insurance / TPA** as the payment method.
2. Attach the TPA name and claim reference number.
3. Save.

### TPA Claim Reconciliation
1. Navigate to **TPA Management → Claims**.
2. The list shows all pending TPA claims.
3. For each settled claim:
   - Click **Mark Settled**.
   - Enter the amount received from the insurer and the settlement date.
4. Discrepancies (where insurer pays less than billed) are flagged for review.

---

## 7. Income Records

Navigate to **Finance → Income** from the navigation.

### Recording Non-Billing Income
For income not originating from patient bills (e.g., grants, donations, rents):
1. Click **Add Income**.
2. Select the income head / category.
3. Enter: amount, date, source, and any notes.
4. Click **Save**.

### Viewing Income Records
- The income list shows all recorded income entries with category, amount, and date.
- Filter by date range or income head.

---

## 8. Expense Records

Navigate to **Finance → Expenses** from the navigation.

### Recording an Expense
1. Click **Add Expense**.
2. Select the expense head / category.
3. Enter: amount, date, payee, payment method, and notes.
4. Attach a receipt or document (if configured).
5. Click **Save**.

### Viewing Expense Records
- Filter by expense head, date range, or payment method.

---

## 9. Financial Reports

Navigate to **Reports** from the navigation.

| Report | Description |
|--------|-------------|
| Daily Transaction Report | All financial transactions for a single day |
| All Transaction Report | Full transaction history with filters |
| Financial Report | Revenue breakdown by department and period |
| Income Report | All income entries by head and period |
| Income Group Report | Income aggregated by category |
| Expense Report | All expense entries by head and period |
| Expense Group Report | Expenses aggregated by category |
| OPD Balance Report | Pending OPD balances |
| IPD Balance Report | Pending IPD balances |
| Pharmacy Balance Report | Outstanding pharmacy bills |
| TPA Report | TPA claims and settlement summary |
| Patient Bill Report | Individual patient billing history |

### Running a Report
1. Select the report from the list.
2. Set the date range and any applicable filters.
3. Click **Generate**.
4. Export as **CSV** or **PDF** from the toolbar.

---

## 10. AI Assistant

The floating AI button (bottom-right):
- Ask for help with billing workflows, report generation, or TPA reconciliation.
- All chatbot sessions are logged.

---

## 11. Logging Out

1. Click your name/avatar in the top-right.
2. Click **Logout**.

---

## Quick Reference

| Task | Where |
|------|-------|
| Create a new bill | Billing → New Bill |
| Process a payment | Billing → Open Bill → Pay |
| Print a receipt | Billing → Open Bill → Print Receipt |
| Record TPA claim | TPA Management |
| Record an expense | Finance → Expenses → Add Expense |
| Run financial report | Reports → Financial Report |
| Run daily transactions | Reports → Daily Transaction Report |
| View unpaid bills | Billing → filter by Unpaid |
