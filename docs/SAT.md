# Site Acceptance Test (SAT) — MedyxHMS

> **Date:** 2026-06-24 | **Version:** 1.0 | **Status:** Ready for Testing

---

## Purpose
Verify that all deployed features function correctly in the target environment.

---

## 1. Authentication & Login

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 1.01 | Login with valid credentials (superadmin@hospital.com) | Redirect to role selection, then Dashboard | |
| 1.02 | Login with invalid password | Error: "Invalid login attempt" | |
| 1.03 | Login with inactive account | Error: account inactive/pending approval | |
| 1.04 | Login with default password "Medyx147" | Redirect to ForceChangePassword page | |
| 1.05 | ForceChangePassword: enter current + new password (8+ chars) | Password changed, redirected to Dashboard | |
| 1.06 | ForceChangePassword: mismatched confirm password | Error: "Passwords do not match" | |
| 1.07 | ForceChangePassword: password < 8 chars | Error: "Password must be at least 8 characters" | |
| 1.08 | Logout | Session ended, redirect to Login | |
| 1.09 | Login with employee ID (SUPER001, UAT-ADM-001, etc.) | Successful login via EmployeeId lookup | |

---

## 2. Sidebar Toggle (Phase 1)

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 2.01 | Click ☰ toggle button on desktop | Sidebar collapses to 56px icon-only | |
| 2.02 | Click ☰ again | Sidebar expands to full width | |
| 2.03 | Refresh page after collapsing | Sidebar remains collapsed | |
| 2.04 | Clear localStorage, refresh | Sidebar shows expanded (default) | |
| 2.05 | Resize to mobile (< 992px) | Sidebar becomes overlay, toggle works | |
| 2.06 | All sidebar links functional when collapsed | Links navigate correctly | |
| 2.07 | Role-restricted items hidden when collapsed | e.g., Admin-only items not visible to Doctor | |

---

## 3. Visual Design (Phase 2)

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 3.01 | Dashboard stat cards show colored icon backgrounds | Primary/Success/Info/Warning circles visible | |
| 3.02 | Quick Action buttons have Font Awesome icons | Icons visible on Add Patient, Schedule, Create Bill | |
| 3.03 | Tables have striped rows and hover highlight | Hover changes row background | |
| 3.04 | Table headers are uppercase with letter-spacing | Consistent styling across all tables | |
| 3.05 | Sidebar links show hover transition | Smooth background color change on hover | |
| 3.06 | Active sidebar link has distinct background | Active link visually different from others | |
| 3.07 | Mobile responsive layout still works | Form fields stack correctly on small screens | |

---

## 4. Profile Pictures (Phase 3)

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 4.01 | Navigate to Profile page | Shows account details + upload form | |
| 4.02 | Upload valid JPG (≤ 2MB) | Image saved, shown on Profile page (140px circle) | |
| 4.03 | Upload valid PNG | Image saved and displayed | |
| 4.04 | Upload file > 2MB | Error: "File exceeds 2 MB limit" | |
| 4.05 | Upload .gif or .exe file | Error: "Only JPG and PNG files are allowed" | |
| 4.06 | Navbar shows profile picture after upload | 32px rounded image appears next to username | |
| 4.07 | No image uploaded — default avatar shown | SVG placeholder visible in navbar and profile | |
| 4.08 | Click "Remove" on Profile page | Image cleared, default avatar restored | |
| 4.09 | Upload new image replaces old one | Old file deleted, new file saved with GUID name | |

---

## 5. Multi-Factor Authentication (Phase 4)

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 5.01 | Navigate to Profile → Enable MFA | QR code page displayed | |
| 5.02 | QR code scans correctly (Google Authenticator) | App adds MedyxHMS account | |
| 5.03 | Enter valid 6-digit TOTP code | MFA enabled, redirected to Profile with success message | |
| 5.04 | Enter invalid OTP code | Error: "Invalid code. Try again." — MFA NOT enabled | |
| 5.05 | Login with MFA-enabled account | Password OK → redirected to VerifyMFA page | |
| 5.06 | Enter valid OTP on VerifyMFA | Login completes, redirected to Dashboard | |
| 5.07 | Enter invalid OTP on VerifyMFA | Error: "Invalid verification code" — login incomplete | |
| 5.08 | Login with MFA-disabled account | Normal login flow, no MFA prompt | |
| 5.09 | Disable MFA with correct password | MFA disabled, success message shown | |
| 5.10 | Disable MFA with wrong password | Error: "Incorrect password" — MFA stays enabled | |
| 5.11 | Recovery codes generated on MFA setup | 8 codes created, stored as SHA256 hashes | |
| 5.12 | Login with recovery code (instead of TOTP) | Login succeeds, recovery code consumed | |
| 5.13 | Reuse consumed recovery code | Login fails — code already used | |

---

## 6. Audit Log Viewer (Phase 5)

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 6.01 | Admin/SuperAdmin see "Audit Logs" in sidebar | Menu items visible in Admin section | |
| 6.02 | Doctor/Nurse/Staff do NOT see "Audit Logs" | Menu items are hidden | |
| 6.03 | Click "Audit Logs" → /Audit | Audit log list displayed with filters | |
| 6.04 | Filter by date range | Results narrow to selected range | |
| 6.05 | Filter by entity type | Only matching entities shown | |
| 6.06 | Color-coded action badges | FAILED=red, SUCCESS=green, LOGIN=blue, other=gray | |
| 6.07 | Failed actions highlighted in red | Row has `table-danger` background | |
| 6.08 | Click "CSV" export button | CSV file downloads with correct columns | |
| 6.09 | Click "View" on a log entry | Detail view shows old/new JSON, IP address | |
| 6.10 | "User Actions" sidebar link works | Shows user-specific action logs | |
| 6.11 | Meta-audit logged when viewing logs | AUDIT_LOG_VIEWED entry appears in logs | |

---

## 7. Role-Based Access Control

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 7.01 | SuperAdmin accesses all modules | All 30 modules visible and functional | |
| 7.02 | Admin cannot access License page | License link hidden/menu restricted | |
| 7.03 | Doctor accesses OPD, IPD, Prescriptions | Clinical modules available | |
| 7.04 | Doctor cannot access Billing admin | Billing management restricted | |
| 7.05 | Patient role cannot access Staff Portal | Redirected to Patient Portal | |
| 7.06 | Staff role cannot access Admin modules | Module Management, CMS hidden | |

---

## 8. Database & Schema

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 8.01 | AspNetUsers has MFA columns | MFAEnabled, MFASecretKey, MFATempSecret, MFARecoveryCodes exist | |
| 8.02 | AspNetUsers has ProfileImage column | Column exists, nullable | |
| 8.03 | All seeded users exist (8 users) | superadmin + 7 UAT users | |
| 8.04 | Database has 96 tables | All tables present | |
| 8.05 | App starts without errors | `dotnet run` succeeds, no unhandled exceptions | |

---

## 9. Documentation

| # | Test Case | Expected Result | Pass/Fail |
|---|-----------|----------------|-----------|
| 9.01 | PRD.md up to date (sections 9.1–9.6) | All features documented | |
| 9.02 | Function_List.md complete (49 interfaces, 51 implementations) | Counts match | |
| 9.03 | Functionality.md covers sections 1–43 | All features described | |
| 9.04 | Modules.md lists all 30 modules | Complete module registry | |
| 9.05 | Sidebar.md shows 32 top-level items | Includes Audit Logs + User Actions | |
| 9.06 | All 15 user guides updated (2026-06-24) | Dates and features current | |

---

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Tester | | | |
| Developer | | | |
| Admin | | | |
| SuperAdmin | | | |
