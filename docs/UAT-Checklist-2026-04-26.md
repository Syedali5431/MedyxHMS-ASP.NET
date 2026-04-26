# Final User Acceptance Testing (UAT) Checklist — Medyx HMS

## Purpose
This checklist ensures all modules, workflows, and role-based access controls are validated before production deployment and project closure.

---

## 1. General System Validation
- [ ] All modules visible in sidebar for entitled roles
- [ ] Dashboard loads for all staff roles (SuperAdmin, Admin, Doctor, Nurse, etc.)
- [ ] Patient Portal accessible and routes correctly
- [ ] All main navigation links work (no 404s)

## 2. Module-Specific UAT
- [ ] Bed Management: Assign, release, transfer, status update, bulk add, and role restrictions
- [ ] Certificate Management: Birth/Death certificate generation, template editing, print preview
- [ ] Appointment, OPD, IPD: Create, edit, discharge, transfer, and reporting
- [ ] Billing: Create, edit, view, and export bills
- [ ] Pharmacy, Lab, Radiology: Order, result entry, and reporting
- [ ] Ambulance, Blood Bank, Inventory: CRUD and workflow actions
- [ ] Download Center: Upload/download files
- [ ] Messaging: Inbox, compose, broadcast
- [ ] Reports: All 49 reports accessible, filterable, and exportable
- [ ] CMS: Page, notice, menu, and site settings management

## 3. Role-Based Access
- [ ] SuperAdmin: Full access to all modules and admin features
- [ ] Admin: Full access except license management
- [ ] Doctor/Nurse: Clinical modules, read-only for admin modules
- [ ] Accountant: Billing, payroll, and financial reports
- [ ] Receptionist: Front office, billing, appointment
- [ ] Patient: Patient Portal only
- [ ] LabTechnician/Radiologist/Staff: Module-specific access

## 4. Governance & Admin Workflows
- [ ] Module management: Enable/disable modules, assign user/module access
- [ ] License management: Upload, validate, and renew license
- [ ] User management: Add, edit, approve, and assign roles
- [ ] CMS: Add/edit pages, notices, menus, and settings

## 5. Technical Validation
- [ ] All API endpoints return expected results (200/201/204)
- [ ] No unhandled exceptions in logs
- [ ] Automated tests pass (service, controller, route-contract)
- [ ] Database migrations applied and verified
- [ ] Responsive UI on desktop and mobile
- [ ] Notifications and confirmation dialogs work

## 6. Evidence & Documentation
- [ ] Update `Docs/Final-touches.md` with UAT results
- [ ] Save screenshots of key workflows (optional)
- [ ] Archive test output: `temp_build_output/uat-role-run-current.json`

---

**Sign-off:**
- [ ] All items checked and validated by project owner
- [ ] Ready for production deployment
