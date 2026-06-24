# MedyxHMS — Sidebar Menu Reference

> **Date**: 2026-06-24 | **Source**: `Views/Shared/Components/SidebarNav/Default.cshtml`

---

## Overview

The sidebar is rendered by `Components/SidebarNavViewComponent.cs` using `SidebarNavViewModel`. Menu items are gated by module enablement (`Model.ModuleOn("Key")`) and role checks (`Model.IsAdminOrSuper`, `Model.IsSuperAdmin`). SuperAdmin sees all items.

---

## Legend

| Tag | Meaning |
|-----|---------|
| 🔓 | Always visible (no module/role gate) |
| 🔒 | Requires specific role |
| 🏷️ | Gated by module enablement |

---

## 🔝 Top Section

| Icon | Label | URL | Gate |
|------|-------|-----|------|
| `fa-tachometer-alt` | **Dashboard** | `/Dashboard` | 🔓 Always visible |

---

## 🏥 Clinical Section

| Icon | Label | Sub-items | Gate |
|------|-------|-----------|------|
| `fa-calendar-check` | **Appointments** | | 🏷️ `Appointment` |
| | ↳ All Appointments | `/Appointment` | |
| | ↳ Calendar | `/Appointment/Calendar` | |
| | ↳ Schedule New | `/Appointment/Create` | |
| `fa-stethoscope` | **OPD** | | 🏷️ `OPD` |
| | ↳ OPD Visits | `/OPD` | |
| | ↳ New OPD Visit | `/OPD/Create` | |
| `fa-bed` | **IPD** | | 🏷️ `IPD` |
| | ↳ Admissions | `/IPD` | |
| | ↳ New Admission | `/IPD/Create` | |
| `fa-pills` | **Pharmacy** | | 🏷️ `Prescription` |
| | ↳ Prescriptions | `/Prescription` | |
| | ↳ Medicines | `/Prescription/Medicines` | |
| `fa-flask` | **Pathology** | | 🏷️ `Lab` |
| | ↳ Lab Tests | `/Lab` | |
| | ↳ Results | `/Lab/Results` | |
| `fa-x-ray` | **Radiology** | | 🏷️ `Radiology` |
| | ↳ Tests | `/Radiology` | |
| | ↳ Results | `/Radiology/Results` | |
| `fa-tint` | **Blood Bank** | | 🏷️ `BloodBank` |
| | ↳ Inventory | `/BloodBank` | |
| | ↳ Issue Blood | `/BloodBank/Issue` | |
| `fa-ambulance` | **Ambulance** | | 🏷️ `Ambulance` |
| | ↳ Vehicles | `/Ambulance` | |
| | ↳ Dispatch Log | `/Ambulance/Dispatches` | |
| | ↳ New Dispatch | `/Ambulance/Dispatch` | |
| `fa-procedures` | **Operation Theatre** | | 🏷️ `OperationTheatre` |
| | ↳ OT Schedule | `/OperationTheatre` | |
| | ↳ Add OT Case | `/OperationTheatre/Create` | |

---

## 📋 Administrative Section

| Icon | Label | Sub-items | Gate |
|------|-------|-----------|------|
| `fa-file-invoice-dollar` | **Billing** | | 🏷️ `Billing` |
| | ↳ Bills | `/Billing` | |
| | ↳ New Bill | `/Billing/Create` | |
| `fa-building` | **Front Office** | | 🏷️ `FrontOffice` |
| | ↳ Overview | `/FrontOffice` | |
| | ↳ Visitors | `/FrontOffice/Visitors` | |
| | ↳ Complaints | `/FrontOffice/Complaints` | |
| `fa-share-alt` | **Referral** | | 🏷️ `Referral` |
| | ↳ Referrals | `/Referral` | |
| | ↳ Create Referral | `/Referral/Create` | |

---

## 🧑‍⚕️ Patients Section

| Icon | Label | Sub-items | Gate |
|------|-------|-----------|------|
| `fa-user-injured` | **Patients** | | 🏷️ `Patient` |
| | ↳ Patient List | `/Patient` | |
| | ↳ Add Patient | `/Patient/Create` | |
| `fa-baby` | **Birth / Death** | | 🏷️ `BirthDeath` |
| | ↳ Birth Records | `/BirthDeath` | |
| | ↳ Death Records | `/BirthDeath/Deaths` | |
| | ↳ New Birth | `/BirthDeath/CreateBirth` | |
| | ↳ New Death | `/BirthDeath/CreateDeath` | |

---

## 👥 HR Section

> **Section visible if:** `ModuleOn("Attendance")` OR `ModuleOn("Leave")` OR `ModuleOn("Payroll")` OR `IsAdminOrSuper`

| Icon | Label | Sub-items | Gate |
|------|-------|-----------|------|
| `fa-users-cog` | **Human Resource** | | *(section gate)* |
| | ↳ Staff | `/Staff` | 🔒 `IsAdminOrSuper` |
| | ↳ Attendance | `/Attendance` | 🏷️ `Attendance` |
| | ↳ Leave | `/Leave` | 🏷️ `Leave` |
| | ↳ Leave Types | `/Leave/Types` | 🏷️ `Leave` + 🔒 `IsAdminOrSuper` |
| | ↳ Balances | `/Leave/Balances` | 🏷️ `Leave` + 🔒 `IsAdminOrSuper` |
| | ↳ Payroll | `/Payroll` | 🏷️ `Payroll` |
| | ↳ Generate Payroll | `/Payroll/Generate` | 🏷️ `Payroll` + 🔒 `IsAdminOrSuper` |

---

## 📦 Other Section

| Icon | Label | Sub-items | Gate |
|------|-------|-----------|------|
| `fa-chart-pie` | **Finance** | `/Report/FinancialReport` | 🏷️ `Report` OR 🔒 `IsAdminOrSuper` |
| `fa-certificate` | **Certificates** | | 🏷️ `Certificate` |
| | ↳ Overview | `/Certificate` | |
| | ↳ Birth Certificate | `/Certificate/Birth` | |
| | ↳ Death Certificate | `/Certificate/Death` | |
| | ↳ Generate Certificate | `/Certificate/GenerateCertificate` | 🔒 `IsAdminOrSuper` |
| | ↳ Patient / Staff ID Card | `/Certificate/GenerateIdCard` | 🔒 `IsAdminOrSuper` |
| `fa-chart-bar` | **Reports** | | 🏷️ `Report` |
| | ↳ Reports Workspace | `/Report` | |
| | ↳ Report Details | `/SystemManagement/ReportManagement` | |
| | ↳ *Dynamic Report Catalog* | `/Report?reportKey={key}` | *(grouped by Category)* |
| `fa-bell` | **Notifications** | `/Notifications` | 🔓 Always visible |
| `fa-file-medical-alt` | **TPA Management** | | 🏷️ `TPA` |
| | ↳ Providers | `/Tpa` | |
| | ↳ Claims | `/Tpa/Claims` | |
| | ↳ New Claim | `/Tpa/CreateClaim` | |
| | ↳ Add Provider | `/Tpa/Create` | 🔒 `IsAdminOrSuper` |
| `fa-boxes` | **Inventory** | | 🏷️ `Inventory` |
| | ↳ Items | `/Inventory` | |
| | ↳ Transactions | `/Inventory/Transactions` | |
| | ↳ Low Stock | `/Inventory/LowStock` | |
| | ↳ Add Item | `/Inventory/Create` | 🔒 `IsAdminOrSuper` |
| `fa-envelope` | **Messaging** | | 🏷️ `Messaging` |
| | ↳ Inbox | `/Messaging` | |
| | ↳ Sent | `/Messaging/Sent` | |
| | ↳ Compose | `/Messaging/Compose` | |
| | ↳ Broadcast | `/Messaging/Broadcast` | 🔒 `IsAdminOrSuper` |
| `fa-download` | **Download Center** | | 🏷️ `DownloadCenter` |
| | ↳ Files | `/DownloadCenter` | |
| | ↳ Upload | `/DownloadCenter/Upload` | 🔒 `IsAdminOrSuper` |
| `fa-video` | **Live Consultation** | | 🏷️ `LiveConsultation` |
| | ↳ Sessions | `/LiveConsultation` | |
| | ↳ Schedule | `/LiveConsultation/Schedule` | |
| `fa-procedures` | **Bed Management** | | 🏷️ `BedManagement` |
| | ↳ Bed Overview | `/BedManagement` | |
| | ↳ Add Bed | `/BedManagement/Create` | 🔒 `IsSuperAdmin` OR `IsAdmin` OR `Nurse` |

---

## 🔧 Admin Section

> **Entire section only visible if:** `IsAdminOrSuper`

| Icon | Label | Sub-items | Gate |
|------|-------|-----------|------|
| `fa-globe` | **Website CMS** | | 🏷️ `CMS` |
| | ↳ Pages | `/Cms` | |
| | ↳ Notices | `/Cms/Notices` | |
| | ↳ Menu | `/Cms/Menu` | |
| | ↳ Booking Requests | `/Cms/AppointmentRequests` | |
| | ↳ Notification Settings | `/Cms/NotificationSettings` | |
| | ↳ Site Settings | `/PublicSiteAdmin/Settings` | |
| | ↳ View Public Site | `/Site` | *(new tab)* |
| `fa-robot` | **Chatbot Admin** | | 🏷️ `Chatbot` |
| | ↳ Settings | `/ChatbotAdmin/Settings` | |
| | ↳ Analytics | `/ChatbotAdmin/Analytics` | |
| | ↳ Escalations | `/ChatbotAdmin/Escalations` | |
| `fa-cogs` | **Setup / Settings** | | 🔓 Always in Admin |
| | ↳ App Config | `/PublicSiteAdmin/Settings` | |
| | ↳ Accounts Approval | `/AccountsApproval` | |
| | ↳ Password Mgmt | `/AccountsApproval/Passwords` | |
| | ↳ Module Management | `/ModuleManagement` | |
| | ↳ User Module Access | `/ModuleManagement/Users` | |
| | ↳ License | `/License` | 🔒 `IsSuperAdmin` only |

---

## ⚙️ System Management (Bottom)

> **Visible if:** NOT `Patient` role

| Icon | Label | URL | Gate |
|------|-------|-----|------|
| `fa-tools` | **System Management** | *(expandable)* | — |
| | ↳ Report List | `/SystemManagement/ReportManagement` | 🔓 |
| | ↳ Create Report | `/SystemManagement/CreateReport` | 🔒 `IsSuperAdmin` OR `Admin` |
| | ↳ Edit Report | `/SystemManagement/EditReport` | 🔒 `IsSuperAdmin` OR `Admin` |
| | ↳ Download Report | `/SystemManagement/DownloadReport` | 🔓 |
| | ↳ User Management | `/SystemManagement/UserManagement` | 🔒 `IsSuperAdmin` OR `Admin` |
| | ↳ Theme Management | `/SystemManagement/ThemeManagement` | 🔓 |

---

## Summary

| Category | Top-Level Items | Sub-items |
|----------|----------------|-----------|
| Top | 1 | 0 |
| Clinical | 9 | 23 |
| Administrative | 3 | 7 |
| Patients | 2 | 6 |
| HR | 1 | 7 |
| Other | 10 | 28 |
| Admin | 3 | 14 |
| System Management | 1 | 6 |
| **Total** | **30** | **91** |

---

## Phase Implementation Notes

**Phase 1 — Sidebar Toggle (2026-06-24):** No new sidebar items. Menu structure unchanged. Toggle button moved from mobile-only (`d-lg-none`) to all screen sizes. Sidebar now supports desktop collapse to 56px icon-only view with localStorage persistence.

**Phase 2 — Visual Design Polish (2026-06-24):** No new sidebar items. Visual polish applied: hover transitions, active link backgrounds, border-radius on sidebar links. No structural changes to menu hierarchy.
