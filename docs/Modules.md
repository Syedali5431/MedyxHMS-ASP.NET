# MedyxHMS — Modules Reference

> **Date**: 2026-06-24 | **Total Modules**: 30

---

## Overview

The MedyxHMS system is organized into 30 feature modules. Each module controls sidebar visibility via `SystemModule` entities and per-user access via `UserModuleAccess`. SuperAdmin bypasses all module restrictions.

**Module gating:** `Services/Interfaces/IServices.cs` → `IModuleService` → `Services/Implementations/ModuleService.cs`

---

## Module Registry

### 🏥 Clinical Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 1 | `Appointment` | Appointments | Appointment scheduling, calendar view, conflict detection | ✅ |
| 2 | `OPD` | Outpatient Department | OPD visits, consultations, prescriptions | ✅ |
| 3 | `IPD` | Inpatient Department | IPD admissions, wards, discharge, bed assignment | ✅ |
| 4 | `Prescription` | Pharmacy & Prescription | Pharmacy inventory, prescriptions, dispensing | ✅ |
| 5 | `Lab` | Laboratory | Lab test catalog, test ordering, results entry | ✅ |
| 6 | `Radiology` | Radiology | Radiology test catalog, imaging orders, reports | ✅ |
| 7 | `BloodBank` | Blood Bank | Blood inventory management, blood issuing to patients | ✅ |
| 8 | `OperationTheatre` | Operation Theatre | OT scheduling, case management | ✅ |
| 9 | `Ambulance` | Ambulance Management | Ambulance dispatch, vehicle tracking | ✅ |
| 10 | `LiveConsultation` | Live Consultation | Video consultation session scheduling | ✅ |
| 11 | `Referral` | Referrals | Patient referral creation and status tracking | ✅ |

### 👥 Patient & Records Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 12 | `Patient` | Patient Management | Patient registration, records, medical history | ✅ |
| 13 | `BirthDeath` | Birth / Death Records | Hospital birth and death certificate records | ✅ |
| 14 | `Certificate` | Certificates & ID Cards | Staff certificates, patient ID cards | ✅ |
| 15 | `BedManagement` | Bed Management | Bed tracking, assignment, transfer, status | ✅ |

### 💰 Financial Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 16 | `Billing` | Billing | Invoices, payments, multi-gateway payment processing | ✅ |
| 17 | `TPA` | TPA Management | Third-party administrator claims and providers | ✅ |

### 👷 HR & Administrative Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 18 | `Attendance` | Attendance | Staff attendance tracking, check-in/out | ✅ |
| 19 | `Leave` | Leave Management | Leave requests, approvals, balances, leave types | ✅ |
| 20 | `Payroll` | Payroll | Staff payroll generation and payment marking | ✅ |

### 🏢 Operations Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 21 | `FrontOffice` | Front Office | Visitor logs, complaints, dispatch/receive | ✅ |
| 22 | `Inventory` | Inventory Management | Hospital supply inventory, stock transactions | ✅ |
| 23 | `Messaging` | Internal Messaging | Staff-to-staff internal messaging, broadcasts | ✅ |
| 24 | `DownloadCenter` | Download Center | Staff document uploads and downloads | ✅ |

### 📊 Intelligence Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 25 | `Dashboard` | Dashboard | Main dashboard with KPIs, charts, module navigator | ✅ |
| 26 | `Report` | Reports | System reports, analytics, custom templates | ✅ |

### 🌐 External-Facing Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 27 | `PatientPortal` | Patient Portal | Patient self-service: profile, appointments, records | ✅ |
| 28 | `CMS` | CMS / Public Website | Public website pages, notices, menus, booking | ✅ |
| 29 | `Chatbot` | Chatbot | AI-powered chatbot assistant with escalation | ✅ |

### 🔐 System Modules

| # | Key | Display Name | Description | Default |
|---|-----|-------------|-------------|---------|
| 30 | `License` | License Management | System license activation, renewal, reminders | ✅ |

---

## Feature Toggles (appsettings.json)

These toggles exist in `appsettings.json` → `FeatureToggles` and represent a legacy/additional gating layer:

| Toggle Key | Default | Related Module |
|-----------|---------|----------------|
| `PatientPortal` | `true` | PatientPortal |
| `AppointmentSystem` | `true` | Appointment |
| `BillingModule` | `true` | Billing |
| `OPDModule` | `true` | OPD |
| `IPDModule` | `true` | IPD |
| `PharmacyModule` | `true` | Prescription |
| `LabModule` | `true` | Lab |
| `RadiologyModule` | `true` | Radiology |
| `HRModule` | `false` | Attendance, Leave |
| `PayrollModule` | `false` | Payroll |
| `InventoryModule` | `false` | Inventory |
| `ReportsModule` | `true` | Report |
| `PublicWebsite` | `false` | CMS |
| `MobileAPI` | `false` | App (Mobile API) |
| `CertificateModule` | `true` | Certificate |

---

## Module Access Model

```
SystemModule (global)
    ├── IsGloballyEnabled (bool) ← SuperAdmin toggles
    └── UserModuleAccess (per-user overrides)
            ├── UserId
            ├── IsEnabled (bool)
            └── Effective = IsGloballyEnabled AND (default true OR UserModuleAccess.IsEnabled)
```

**SuperAdmin** sees all modules regardless of settings.

**File locations:**
- Model: `Models/RBAC.cs` → `SystemModule`, `UserModuleAccess`
- Service: `Services/Interfaces/IServices.cs` → `IModuleService`
- Implementation: `Services/Implementations/ModuleService.cs`
- Controller: `Controllers/ModuleManagementController.cs`
