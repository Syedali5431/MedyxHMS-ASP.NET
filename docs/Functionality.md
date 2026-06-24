# MedyxHMS — Functionality Reference

> **Date**: 2026-06-24 | **Architecture**: Controllers → Services → Data (EF Core)

---

## 1. Authentication & Authorization

### Identity System
**Files:** `Models/ApplicationUser.cs`, `Program.cs` (lines 35–46)

- ASP.NET Core Identity with `ApplicationUser : IdentityUser`
- Password policy: 8+ chars, requires digit, lowercase, uppercase
- Email-based login with optional Employee ID lookup
- Role-based authorization with 11 roles: SuperAdmin, Admin, Doctor, Nurse, Staff, Accountant, Receptionist, Pharmacist, LabTechnician, Radiologist, Patient

### Login Flow
**File:** `Controllers/AccountController.cs`

1. User enters email/employee ID + password
2. System finds user by email → falls back to EmployeeId lookup
3. Checks `IsActive` — inactive users see approval/rejection status
4. `PasswordSignInAsync` with lockout on failure
5. Optional legacy bcrypt password migration
6. Concurrent session enforcement via `IConcurrentSessionService`
7. Role-based redirect after successful login
8. Audit logging for login success, failure, lockout

### Account Registration
**File:** `Controllers/AccountController.cs`

- Self-registration creates account + approval request
- Admin/SuperAdmin must approve via `AccountsApprovalController`
- Approval status: Pending → Approved/Rejected

### Permission-Based Authorization
**File:** `Extensions/` (custom permission handler)

- Fine-grained permissions: ViewPatients, AddPatients, EditPatients, DeletePatients, etc.
- Permission claims added to user identity
- `[Authorize(Roles = "...")]` on controllers
- Custom `IAuthorizationService` for programmatic permission checks

### Concurrent Session Control
**File:** `Services/Implementations/ConcurrentSessionService.cs`

- Enforces per-user session limits at login
- Tracks active sessions by userId, role, sessionId, IP, user agent
- Blocks login when limit exceeded
- Integrated with license enforcement

---

## 2. Dashboard
**Files:** `Controllers/DashboardController.cs`, `Views/Dashboard/`

### Main Dashboard
- Role-aware KPI cards (patients, appointments, revenue, occupancy)
- Module navigator with quick links
- Charts: appointment trends, revenue by department, bed occupancy
- Role-based dashboard views: Admin, Patient, Appointment, Billing

---

## 3. Patient Management
**Files:** `Controllers/PatientController.cs`, `Services/Implementations/PatientService.cs`

### Patient Registration
- Full demographic capture: name, DOB, gender, blood group, contact
- Emergency contact with relationship
- Medical history & allergies
- Insurance information
- Guardian details

### Patient Records
- Searchable patient list with pagination
- Edit/update patient information
- Patient detail view with:
  - Appointment history
  - OPD visit history
  - IPD admission history
  - Billing history
  - Lab/radiology results

---

## 4. Appointment System
**Files:** `Controllers/AppointmentController.cs`, `Services/Implementations/AppointmentService.cs`

### Scheduling
- Create appointments with patient + doctor selection
- Appointment types and status tracking
- Conflict detection & validation

### Views
- List view with search, status filter, date filter, doctor filter
- Weekly calendar view
- KPI dashboard: today's appointments, upcoming, by type, by status

### Export
- CSV and PDF export of appointment lists

---

## 5. OPD (Outpatient Department)
**Files:** `Controllers/OPDController.cs`, `Services/Implementations/OPDService.cs`

### OPD Visits
- Create OPD visit with patient, doctor, department
- Record symptoms, diagnosis, treatment, prescription
- Payment status tracking
- Edit and detail views

### Revenue Tracking
- OPD revenue calculation by date range

---

## 6. IPD (Inpatient Department)
**Files:** `Controllers/IPDController.cs`, `Services/Implementations/IPDService.cs`

### Admissions
- Admit patient with ward + bed assignment
- Diagnosis, treatment plan, attending doctor
- Daily charges tracking

### Discharge
- Generate final IPD bill on discharge
- Update bed status to cleaning

### AJAX Endpoints
- Get available beds for a ward
- Get patient medical history

---

## 7. Beds & Wards
**Files:** `Controllers/BedManagementController.cs`, `Services/Implementations/BedService.cs`, `Services/Implementations/WardService.cs`

### Ward Management
- Ward CRUD with bed capacity tracking
- Bed occupancy and availability statistics

### Bed Management
- Bed overview grid with status colors
- Assign bed to patient (ICU admin approval check)
- Release bed → Cleaning status
- Transfer patient between beds
- Manual status override (Available/Cleaning/Maintenance/Blocked)

### REST API
- Full CRUD via API endpoints for mobile/web integration
- Assign, release, transfer, status update

---

## 8. Billing & Payments
**Files:** `Controllers/BillingController.cs`, `Services/Implementations/BillingService.cs`

### Bill Management
- Create bills with line items (service, medicine, room charges)
- Bill status tracking: Pending, Paid, Partial, Cancelled
- Paginated list with status filter

### Payments
- Offline/internal payment processing
- Multi-gateway online payments (20+ gateways configurable)
- Gateway callback/webhook handling
- Payment refund support

### Receipts
- PDF receipt generation & download
- Export bills to CSV/PDF

### Payment Gateway Configuration
- Gateway name, public key, private key, webhook secret
- Per-gateway currency and active status

---

## 9. Pharmacy & Prescriptions
**Files:** `Controllers/PrescriptionController.cs`, `Services/Implementations/PrescriptionService.cs`

### Prescriptions
- Create prescriptions linked to OPD/IPD visits
- Medicine selection with dosage, frequency, duration
- Prescription list with filters

### Medicine Inventory
- Medicine CRUD with stock tracking
- Pharmacy bill generation
- Pharmacy revenue reporting

---

## 10. Laboratory
**Files:** `Controllers/LabController.cs`, `Services/Implementations/LabService.cs`

### Test Catalog
- Lab test CRUD with category, normal range, unit
- Test code generation

### Test Ordering
- Order lab test for patient (linked to OPD/IPD visit)
- Pending test tracking
- Tests by category filtering

### Results Management
- Result entry with value, normal range, interpretation
- Status workflow: Ordered → In Progress → Completed
- Performed by / Verified by tracking
- Result notes with patient notification
- AJAX endpoints: patient results, pending count

---

## 11. Radiology
**Files:** `Controllers/RadiologyController.cs`, `Services/Implementations/RadiologyService.cs`

### Test Catalog
- Radiology test CRUD with category
- Test code generation

### Test Ordering
- Order radiology test for patient
- Pending test tracking

### Results Management
- Result entry with findings and impression
- Status workflow: Ordered → In Progress → Completed
- Performed by / Verified by tracking
- AJAX endpoints: patient results, pending count

---

## 12. Blood Bank
**Files:** `Controllers/BloodBankController.cs`, `Services/Implementations/BloodBankService.cs`

### Inventory Management
- Blood group stock levels (A+, A-, B+, B-, AB+, AB-, O+, O-)
- Upsert inventory counts
- Issue history tracking

### Blood Issuing
- Issue blood units to patient
- Auto-creates billing entry for blood charges

---

## 13. Operation Theatre
**Files:** `Controllers/OperationTheatreController.cs`, `Services/Implementations/OperationTheatreService.cs`

### OT Scheduling
- Schedule surgeries with patient, doctor, OT room
- Date/time, procedure type, status tracking
- Edit and detail views
- Status updates (Scheduled → In Progress → Completed)

---

## 14. Ambulance
**Files:** `Controllers/AmbulanceController.cs`

### Vehicle Management
- Ambulance vehicle CRUD (Admin/SuperAdmin)
- Vehicle status: Available, Dispatched, Maintenance

### Dispatch Management
- Dispatch ambulance to patient
- Vehicle status auto-update to "Dispatched"
- Mark returned → status back to "Available"

---

## 15. Front Office
**Files:** `Controllers/FrontOfficeController.cs`, `Services/Implementations/FrontOfficeService.cs`

### Visitor Management
- Visitor log with date filter
- Check-in/check-out functionality

### Complaint Management
- Register complaints with status tracking
- Status updates with resolution notes
- Filter by status

### Dispatch/Receive
- Record incoming/outgoing dispatches
- Date filter

---

## 16. HR & Staff Management
**Files:** `Controllers/StaffController.cs`, `Services/Implementations/StaffService.cs`

### Staff Records
- Staff CRUD with search & pagination
- Department, designation, salary, joining date
- Profile page with personal details

### Role Management
- Assign/remove roles per staff member
- Role-based dashboard and permissions

### Password Management
- Admin password reset for any user
- Staff self-service password change

---

## 17. Attendance
**Files:** `Controllers/AttendanceController.cs`, `Services/Implementations/AttendanceService.cs`

- Daily attendance records by date
- Staff self-check-in / check-out
- Manual attendance marking (Admin)
- Attendance summary statistics

---

## 18. Leave Management
**Files:** `Controllers/LeaveController.cs`, `Services/Implementations/LeaveService.cs`

### Leave Requests
- Submit leave request with date range, type, reason
- Approve / Reject by Admin/SuperAdmin
- Leave balances by year

### Leave Types
- CRUD for leave types (Annual, Sick, Casual, etc.)
- Default days per type

---

## 19. Payroll
**Files:** `Controllers/PayrollController.cs`, `Services/Implementations/PayrollService.cs`

- Payroll generation with month/year selection
- Automatic calculation based on salary + attendance
- Mark payroll as paid
- Payroll listing with year filter

---

## 20. Birth & Death Records
**Files:** `Controllers/BirthDeathController.cs`

### Birth Records
- Record hospital births with auto-generated certificate number
- Parent details, baby details, delivery information

### Death Records
- Record hospital deaths with auto-generated certificate number
- Deceased details, cause of death, certifying doctor

---

## 21. Certificates & ID Cards
**Files:** `Controllers/CertificateController.cs`, `Services/Implementations/CertificateService.cs`

### Certificates
- Birth certificate generation
- Death certificate generation
- Staff certificates (experience, employment)

### ID Cards
- Patient ID card generation
- Staff ID card generation

---

## 22. Referrals
**Files:** `Controllers/ReferralController.cs`, `Services/Implementations/ReferralService.cs`

- Create patient referrals to external facilities
- Referral status tracking
- Detail view with referral information

---

## 23. TPA Management
**Files:** `Controllers/TpaController.cs`

### TPA Providers
- Provider CRUD with contact details

### TPA Claims
- Claim creation linked to patient and provider
- Claim status tracking

---

## 24. Inventory Management
**Files:** `Controllers/InventoryController.cs`

### Item Management
- Inventory item CRUD with category, unit, stock levels
- Reorder level tracking

### Transactions
- Stock IN/OUT transaction recording
- Transaction history by item
- Low stock alerts

---

## 25. Internal Messaging
**Files:** `Controllers/MessagingController.cs`

- Inbox (received + broadcasts)
- Sent messages folder
- Compose message with optional reply
- Broadcast to all staff (Admin/SuperAdmin)
- Soft-delete (per sender/recipient)
- Read/unread tracking

---

## 26. Live Consultation
**Files:** `Controllers/LiveConsultationController.cs`

- Schedule video consultation sessions
- Meeting link validation
- Session status tracking
- Cancel functionality

---

## 27. Download Center
**Files:** `Controllers/DownloadCenterController.cs`

- File upload by Admin/SuperAdmin
- Category-organized file listing
- Download counter tracking
- Soft-delete

---

## 28. Reports & Analytics
**Files:** `Controllers/ReportController.cs`, `Services/Implementations/ReportService.cs`

### Report Types
- **Department reports**: OPD, IPD, Lab, Radiology statistics
- **Financial reports**: Revenue by department, payment summaries
- **Occupancy reports**: Bed utilization, ward occupancy
- **Staff reports**: Attendance summary, leave balances
- **Custom reports**: User-defined templates via `IReportTemplateService`

### Export
- CSV, PDF, Excel formats
- Custom column selection for exports

### Report Templates
- Create/edit custom report templates
- Dynamic field management
- Role-based visibility via `IReportCatalogVisibilityService`

---

## 29. Audit Logging
**Files:** `Controllers/AuditController.cs`, `Services/Implementations/AuditService.cs`

### Audit Trail
- All sensitive actions logged with:
  - Timestamp, userId, action type, entity type, entity ID
  - Old values and new values (JSON)
- Date range and entity type filters
- Single audit entry detail view

### User Action Logs
- Filtered view per specific user
- Date range filtering

---

## 30. System Management
**File:** `Controllers/SystemManagementController.cs`

### User Management
- View all users (Admin/SuperAdmin)
- User activation/deactivation

### Report Management
- Report list, create, edit, download

### Theme Management
- Custom theme colors
- Dynamic CSS stylesheet generation (`/SystemManagement/ThemeStylesheet`)

---

## 31. Module Management
**File:** `Controllers/ModuleManagementController.cs`, `Services/Implementations/ModuleService.cs`

- Global module enable/disable (SuperAdmin)
- Per-user module access overrides
- Module access matrix view

---

## 32. Licensing
**Files:** `Controllers/LicenseController.cs`, `Services/Implementations/LicenseService.cs`

### License Activation
- Upload signed license file + optional RSA public key
- Load from `MedyxHMS.lic` disk file
- Cryptographic verification via `LicenseCryptoUtility`

### License Management
- View license status, expiry, audit history
- Renew license for N years
- Module entitlement matrix
- Export entitlement CSV

### Enforcement
- License expiry page
- Feature-locked page for unlicensed modules
- Expiry reminder system (background job + manual)
- Concurrent user limits

---

## 33. CMS / Public Website
**Files:** `Controllers/CmsController.cs`, `Controllers/PublicSiteAdminController.cs`, `Controllers/SiteController.cs`

### Pages
- CRUD for public website pages (About Us, Services, Contact)
- Status: Draft / Published
- Menu visibility toggle
- Sort order control

### Notices
- CRUD for public notices/announcements
- Type categorization
- Pagination

### Menu
- Menu item CRUD linked to CMS pages
- Sort order

### Public Booking
- Appointment request management from public site
- Notification settings for booking alerts

### Site Settings
- Public site configuration
- Notification templates

---

## 34. Chatbot (AI Assistant)
**Files:** `Controllers/ChatbotController.cs`, `Services/Implementations/OpenAiChatbotService.cs`

### Chat Interface
- OpenAI-powered conversational interface
- Session history
- Context-aware responses via `IChatbotPromptBuilder`

### Content Safety
- Input/output moderation via `IChatbotModerationService`
- PII redaction via `IChatbotPiiRedactionService`
- GDPR consent management via `IChatbotConsentService`

### Escalation
- Escalate to human support
- Escalation queue management (Admin)
- Resolve with handoff notes

### Feedback
- User feedback on chatbot responses
- Helpful/unhelpful ratings

### Admin Controls
- Model selection, token limits, temperature
- PII detection settings
- Consent settings
- Analytics dashboard
- Data cleanup (background job)

---

## 35. Notifications
**Files:** `Controllers/NotificationsController.cs`, `Services/Implementations/SystemNotificationService.cs`

### In-App Notifications
- Notification list with read/unread status
- Mark as read / Mark all as read
- AJAX unread count badge

### Email Notifications
- SMTP email via `SmtpEmailNotificationProvider`
- Configurable retry, SSL, templates

### SMS Notifications
- Twilio SMS via `TwilioSmsNotificationProvider`
- Africa's Talking SMS (alternate gateway)
- Provider router with failover logic

### Delivery Audit
- Full audit trail for all notification deliveries
- Channel, provider, recipient, subject, body, status

---

## 36. Patient Portal
**Directory:** `Areas/PatientPortal/`

### Patient Self-Service
- Login/Register for patients
- Profile management (edit personal details, change password)
- View own appointments, book new ones, cancel
- View medical records (OPD visits, IPD admissions, lab results)
- View bills and payment history
- Doctor directory

---

## 37. Mobile API
**File:** `Controllers/AppController.cs`

- App configuration endpoint (logo, colors, API base URL)
- Feature capability listing
- Versioned API (v1/v2)
- AllowAnonymous access

---

## 38. Infrastructure

### Caching
**File:** `Services/Implementations/CacheService.cs`

- Memory-based caching with TTL
- Get-or-set pattern
- Prefix-based invalidation

### File Management
**File:** `Services/Implementations/FileService.cs`

- Upload to configured path
- Download with content type detection
- Delete with existence check

### Export Service
**File:** `Services/Implementations/ExportService.cs`

- CSV generation from data tables
- PDF table generation (QuestPDF)
- Excel generation (ClosedXML)

### Health Checks
- SMTP health check via `ISmtpHealthService`
- Standard ASP.NET Core health check endpoints

### Background Jobs
- `LicenseReminderHostedService`: periodic license expiry reminders
- `ChatbotDataCleanupHostedService`: periodic chatbot data cleanup

---

## 39. Sidebar Toggle (UI)

**Implemented:** 2026-06-24 | **Files:** `wwwroot/js/sidebar-toggle.js`, `wwwroot/css/site.css`, `Views/Shared/_Layout.cshtml`

### Desktop Collapse
- Toggle button (`#sidebar-toggle-btn`) in top navbar, visible on all screen sizes
- Collapses sidebar from 250px to 56px (icon-only)
- Hides all text spans, sub-links, chevrons, section headers, and collapsed submenus
- Centers sidebar icons when collapsed
- Smooth CSS transition (0.25s ease)

### State Persistence
- Sidebar state saved to `localStorage` under key `medyx-sidebar-collapsed`
- State restored on page load for desktop viewports
- Falls back gracefully if localStorage is unavailable

### Mobile Behavior
- Existing mobile overlay toggle (`.show` class) is completely unaffected
- Toggle button switches between mobile overlay and desktop collapse based on viewport width
- On resize from mobile → desktop, saved desktop state is restored
- On resize from desktop → mobile, `sidebar-collapsed` class is removed

### Compatibility
- All existing sidebar features preserved: role-based visibility, module gating, active link highlighting
- No changes to `SidebarNavViewComponent` or any backend code
- Works alongside existing inline mobile sidebar JS

---

## 40. Visual Design Polish (UI)

**Implemented:** 2026-06-24 | **Files:** `wwwroot/css/site.css`, `Views/Dashboard/Index.cshtml`

### Dashboard Stat Cards
- Icons upgraded from faint `text-gray-300 fa-2x` to colored `fa-lg` inside `rounded-circle bg-{color} bg-opacity-10 p-3` containers
- Colors match card border accents: primary (Patients), success (Appointments), info (Revenue), warning (Pending Bills)
- Quick action buttons now include Font Awesome icons

### Table Styling (Global)
- `.table` gets `border-radius: 0.5rem` with hidden overflow
- `thead th`: uppercase, 0.8rem, 0.05em letter-spacing, `#64748b` color, `#f8fafc` background
- `.table-hover tbody tr:hover`: `#f1f5f9` background

### Form Enhancements (Global)
- `.form-container` max-width: 900px
- Floating labels use `--hms-primary` on focus/filled
- Last `.form-floating` in `.card` removes bottom margin

### Sidebar Hover Polish
- `.staff-sidebar-link`: 0.15s ease transitions, `border-radius: 0.375rem`, horizontal margin
- Hover: `rgba(255,255,255,0.08)` background
- Active: `rgba(255,255,255,0.15)` background + `font-weight: 600`
- `.staff-sidebar-sublink`: smooth color transition

### Safety
- CSS-only changes — no form names, IDs, or bindings modified
- No backend, controller, or service changes

---

## 41. Profile Pictures

**Implemented:** 2026-06-24 | **Files:** `Services/Interfaces/IProfileImageService.cs`, `Services/Implementations/ProfileImageService.cs`, `Components/ProfileImageViewComponent.cs`, `Views/Account/Profile.cshtml`, `Views/Shared/_Layout.cshtml`

### Upload Service
- `IProfileImageService` with `UploadAsync`, `DeleteAsync`, `GetDisplayPath`
- Validates file type (JPG/PNG only), file size (≤ 2MB)
- GUID-based filename: `{userId}_{Guid:N}{ext}`
- Deletes old image before saving new one
- Creates `/wwwroot/uploads/profile/` directory if missing

### Profile Page
- `/Account/Profile` — displays user info (name, email, username, employee ID, phone, status, member since)
- Profile picture upload form with preview (140px rounded circle)
- Remove button for clearing profile picture

### Navbar Display
- `ProfileImageViewComponent` renders 32px rounded profile image in navbar
- Shows default SVG avatar when no image set
- Rendered via `@await Component.InvokeAsync("ProfileImage")` in `_Layout.cshtml`

### Security
- Extension whitelist prevents executable uploads
- 2MB size limit prevents disk exhaustion
- GUID filenames prevent path traversal
- Audit logging on upload and delete actions
- RBAC: any authenticated user can manage their own picture
