# User Guides — Index

**Medyx HMS User Guides**  
**Last Updated:** 2026-06-24

---

## Overview

This folder contains role-specific user guides for all Medyx HMS user roles. Each guide covers login, module access, step-by-step workflows, and quick-reference tables specific to that role.

## New Features (2026-06-24)

| Feature | Description | Affected Roles |
| --------- | ------------- | ---------------- |
| Sidebar Toggle | Collapse sidebar to icon-only (56px) — persists across reloads | All staff |
| Visual Polish | Dashboard icon cards, table/form styling, button icons | All |
| Profile Pictures | Upload JPG/PNG avatar via Profile page, shown in navbar | All |
| MFA (TOTP) | QR code setup with Google/Microsoft Authenticator, recovery codes | All (optional) |
| Forced Password Change | Default password "Medyx147" triggers mandatory password change | All |
| Audit Log Viewer | Color-coded audit logs with CSV export | Admin, SuperAdmin |

---

## Role Guides

| Guide | Role | Portal | Primary Area |
| ------- | ------ | -------- | -------------- |
| [SuperAdmin.md](SuperAdmin.md) | SuperAdmin | Staff Portal | Full system governance, license, security |
| [Admin.md](Admin.md) | Admin | Staff Portal | Hospital operations, patients, billing, reports |
| [Doctor.md](Doctor.md) | Doctor | Staff Portal | OPD, IPD, prescriptions, test ordering |
| [Nurse.md](Nurse.md) | Nurse | Staff Portal | IPD nursing, observations, orders tracking |
| [Pharmacist.md](Pharmacist.md) | Pharmacist | Staff Portal | Prescriptions, stock, pharmacy billing |
| [Accountant.md](Accountant.md) | Accountant | Staff Portal | Billing, payments, income/expense, reports |
| [Receptionist.md](Receptionist.md) | Receptionist | Staff Portal | Registration, appointments, front office |
| [LabTechnician.md](LabTechnician.md) | LabTechnician | Staff Portal | Lab orders, results, test catalogue |
| [Radiologist.md](Radiologist.md) | Radiologist | Staff Portal | Imaging orders, radiology reports, catalogue |
| [Staff.md](Staff.md) | Staff | Staff Portal | Attendance, leave, payroll, notifications |
| [Patient.md](Patient.md) | Patient | Patient Portal | Appointments, records, bills, prescriptions |

---

## Additional Documents

| Document | Description |
|----------|-------------|
| [MedyxHMS-LIC.md](MedyxHMS-LIC.md) | Complete explanation of how the MedyxHMS-Lic licensing tool works |

---

## Portal URLs

| Portal | URL | Who Uses It |
| -------- | ----- | ------------- |
| Staff / Admin Portal | `/` | All staff roles |
| Patient Portal | `/PatientPortal/` | Patients only |
| Public Website | `/Site/` | General public |

---

## Role-to-Module Access Summary

| Module | SA | AD | DR | NU | PH | AC | RC | LT | RA | ST | PA |
| -------- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ----- |
| Dashboard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Patient Mgmt | ✅ | ✅ | 👁 | 👁 | — | — | ✅ | — | — | — | 👁 |
| Appointments | ✅ | ✅ | ✅ | 👁 | — | — | ✅ | — | — | — | ✅ |
| OPD | ✅ | ✅ | ✅ | 👁 | — | — | — | — | — | — | — |
| IPD | ✅ | ✅ | ✅ | ✅ | — | — | 👁 | — | — | — | — |
| Billing | ✅ | ✅ | — | — | ✅ | ✅ | — | — | — | — | 👁 |
| Pharmacy | ✅ | ✅ | ✅ | 👁 | ✅ | — | — | — | — | — | 👁 |
| Lab | ✅ | ✅ | ✅ | — | — | — | — | ✅ | — | — | 👁 |
| Radiology | ✅ | ✅ | ✅ | — | — | — | — | — | ✅ | — | 👁 |
| Blood Bank | ✅ | ✅ | ✅ | — | — | — | — | — | — | — | — |
| Operation Theatre | ✅ | ✅ | ✅ | — | — | — | — | — | — | — | — |
| Front Office | ✅ | ✅ | — | — | — | — | ✅ | — | — | — | — |
| Bed Management | ✅ | ✅ | 👁 | ✅ | — | — | 👁 | — | — | — | — |
| Birth & Death Records | ✅ | ✅ | — | — | — | — | ✅ | — | — | — | — |
| Ambulance | ✅ | ✅ | — | — | — | — | ✅ | — | — | — | — |
| TPA | ✅ | ✅ | 👁 | — | — | ✅ | 👁 | — | — | — | — |
| Messaging | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| Inventory | ✅ | ✅ | — | — | ✅ | 👁 | — | — | — | — | — |
| Download Center | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Live Consultation | ✅ | ✅ | ✅ | 👁 | — | — | 👁 | — | — | — | ✅ |
| Attendance | ✅ | ✅ | — | — | — | — | — | — | — | ✅ | — |
| Leave | ✅ | ✅ | — | — | — | — | — | — | — | ✅ | — |
| Payroll | ✅ | ✅ | — | — | — | ✅ | — | — | — | 👁 | — |
| Certificates | ✅ | ✅ | — | — | — | — | — | — | — | — | — |
| Referrals | ✅ | ✅ | ✅ | — | — | — | ✅ | — | — | — | — |
| Reports | ✅ | ✅ | 👁 | — | 👁 | ✅ | 👁 | 👁 | 👁 | — | — |
| License | ✅ | — | — | — | — | — | — | — | — | — | — |
| Module Mgmt | ✅ | — | — | — | — | — | — | — | — | — | — |
| CMS | ✅ | ✅ | — | — | — | — | — | — | — | — | — |
| Chatbot Admin | ✅ | — | — | — | — | — | — | — | — | — | — |
| Audit | ✅ | 👁 | — | — | — | — | — | — | — | — | — |

**Legend:**

- ✅ Full access
- 👁 Read-only / view access
- — No access (or access controlled by Module Management)
- SA = SuperAdmin, AD = Admin, DR = Doctor, NU = Nurse, PH = Pharmacist, AC = Accountant, RC = Receptionist, LT = LabTechnician, RA = Radiologist, ST = Staff, PA = Patient (via Patient Portal)

> Actual module access is governed by license entitlement (modules must be licensed) and per-user Module Management settings configured by Admin/SuperAdmin. The table above reflects typical defaults.
