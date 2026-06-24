# MedyxHMS — Function List

> **Date**: 2026-06-24 | **No Duplicates** — each function appears once with its canonical location.

---

## Controllers (Action Methods)

### AccountController
**File:** `Controllers/AccountController.cs`

| Function | Purpose |
|----------|---------|
| `Register()` [GET] | Display user registration form |
| `Register(RegisterViewModel)` [POST] | Process user registration, create account + approval request |
| `Login(string?)` [GET] | Display login page |
| `ValidateCredentials(string,string)` [POST] | AJAX: validate email/password & return user roles |
| `Login(LoginViewModel,string?)` [POST] | Authenticate user, enforce session/license limits, role redirect |
| `Logout()` [POST] | Sign out user, end session |
| `LicenseExpired()` [GET] | Display license-expired page |
| `RequestLicenseFile()` [POST] | Email SuperAdmins license renewal request |
| `AccessDenied()` [GET] | Display access-denied page |

### AccountsApprovalController
**File:** `Controllers/AccountsApprovalController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?)` | List pending/approved/rejected account requests |
| `Approve(int)` [POST] | Approve pending account & activate user |
| `Reject(int,string?)` [POST] | Reject pending account |
| `Passwords(string?)` | Admin password management list |
| `ResetPassword(string)` | Show password reset form |
| `ResetPassword(AdminPasswordResetViewModel)` [POST] | Execute password reset |

### AmbulanceController
**File:** `Controllers/AmbulanceController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | List all ambulance vehicles |
| `Create()` | Show vehicle create form |
| `Create(AmbulanceVehicle)` [POST] | Create new ambulance vehicle |
| `Edit(int)` | Show vehicle edit form |
| `Edit(int,AmbulanceVehicle)` [POST] | Update ambulance vehicle |
| `Dispatches()` | List ambulance dispatches |
| `Dispatch()` | Show dispatch form |
| `Dispatch(AmbulanceDispatch)` [POST] | Create ambulance dispatch |
| `MarkReturned(int)` [POST] | Mark dispatch as returned |

### AppController
**File:** `Controllers/AppController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` [POST] | Mobile API v1: return app config |
| `Config()` | Mobile API v2: return extended app config |

### AppointmentController
**File:** `Controllers/AppointmentController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?,string?,DateTime?,int?)` | List appointments with filters |
| `Export(string,string?,string?,DateTime?,int?)` | Export appointments to CSV/PDF |
| `Calendar(DateTime?,int?)` | Weekly calendar view |
| `Dashboard()` | Appointment KPI dashboard |
| `Details(int)` | Appointment detail view |
| `Create(int?,string?)` | Show create form |
| `Create(AppointmentCreateViewModel)` [POST] | Create appointment |
| `Edit(int)` | Show edit form |
| `Edit(int,AppointmentEditViewModel)` [POST] | Update appointment |

### AttendanceController
**File:** `Controllers/AttendanceController.cs`

| Function | Purpose |
|----------|---------|
| `Index(DateTime?,string?)` | View attendance records |
| `MarkAttendance(AttendanceIndexViewModel)` [POST] | Manually mark attendance |
| `CheckIn(DateTime?)` [POST] | Self-service check-in |
| `CheckOut(DateTime?)` [POST] | Self-service check-out |

### AuditController
**File:** `Controllers/AuditController.cs`

| Function | Purpose |
|----------|---------|
| `Index(DateTime?,DateTime?,string,string)` | View audit log entries |
| `Details(int)` | View single audit log detail |
| `UserActions(string,DateTime?,DateTime?)` | View user action logs |

### BedManagementController
**File:** `Controllers/BedManagementController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | Bed overview grid with summary stats |
| `Assign(int,int)` [POST] | Assign bed to patient |
| `Release(int)` [POST] | Release bed from patient |
| `Transfer(int,int)` [POST] | Transfer patient between beds |
| `SetStatus(int,string)` [POST] | Manually set bed status |
| `GetBedsApi()` | REST API: return all beds |
| `AssignBedApi(AssignBedRequest)` [POST] | REST API: assign bed |
| `ReleaseBedApi(ReleaseBedRequest)` [POST] | REST API: release bed |
| `TransferBedApi(TransferBedRequest)` [POST] | REST API: transfer bed |
| `UpdateBedStatusApi(UpdateBedStatusRequest)` [POST] | REST API: update bed status |
| `Create()` | Show bed creation form |
| `Create(Bed,int)` [POST] | Create 1–50 beds |
| `Edit(int)` | Show bed edit form |
| `Edit(int,Bed)` [POST] | Update bed properties |

### BillingController
**File:** `Controllers/BillingController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string,int,int)` | List bills with pagination & filter |
| `Export(string,string)` | Export bills to CSV/PDF |
| `DownloadReceipt(int)` | Download bill PDF receipt |
| `Details(int)` | View bill details |
| `Create()` | Show bill creation form |
| `Create(CreateBillViewModel)` [POST] | Create new bill |
| `Pay(int)` | Show payment form |
| `Pay(PaymentViewModel)` [POST] | Process offline payment |
| `GatewaySettings()` | View payment gateway config |
| `GatewaySettings(PaymentGatewaySettingsViewModel)` [POST] | Save gateway settings |
| `Checkout(int,string)` | Initiate online payment |
| `GatewayReturn(int,string,IQueryCollection?)` | Handle gateway callback |
| `GatewayCallback(string)` [POST] | Handle gateway webhook |

### BirthDeathController
**File:** `Controllers/BirthDeathController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | List birth records |
| `CreateBirth()` | Show birth record form |
| `CreateBirth(BirthRecord)` [POST] | Create birth record |
| `BirthDetails(int)` | View birth record details |
| `Deaths()` | List death records |
| `CreateDeath()` | Show death record form |
| `CreateDeath(DeathRecord)` [POST] | Create death record |
| `DeathDetails(int)` | View death record details |

### BloodBankController
**File:** `Controllers/BloodBankController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | View blood inventory & issue history |
| `UpdateInventory(string,int,int)` [POST] | Upsert blood group stock |
| `Issue()` | Show blood issue form |
| `Issue(BloodIssue)` [POST] | Issue blood to patient |

### CertificateController
**File:** `Controllers/CertificateController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?)` | View certificates & ID cards |
| `Birth()` | Birth certificate page |
| `CreateBirth(BirthRecord)` [POST] | Generate birth certificate |
| `Death()` | Death certificate page |
| `CreateDeath(DeathRecord)` [POST] | Generate death certificate |
| `GenerateCertificate()` | Show staff certificate form |
| `GenerateCertificate(GenerateCertificateViewModel)` [POST] | Generate staff certificate |
| `GenerateIdCard()` | Show ID card form |
| `GenerateIdCard(GenerateIdCardViewModel)` [POST] | Generate staff ID card |

### ChatbotAdminController
**File:** `Controllers/ChatbotAdminController.cs`

| Function | Purpose |
|----------|---------|
| `Settings()` | View chatbot admin settings |
| `Settings(ChatbotAdminSettingsViewModel)` [POST] | Save chatbot settings |
| `Analytics(int)` | View chatbot analytics |
| `Escalations(string)` | View escalation queue |
| `ResolveEscalation(long,string)` [POST] | Resolve escalation |

### ChatbotController
**File:** `Controllers/ChatbotController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?)` | Chatbot interface page |
| `RequestConsent()` | Show AI consent form |
| `AcceptConsent(ChatbotConsentAcceptViewModel)` [POST] | Record consent |
| `RejectConsent()` [POST] | Record consent rejection |
| `GetConsentStatus()` | AJAX: consent status |
| `Ask(ChatbotPageViewModel)` [POST] | Send prompt, get AI response |
| `AskJson(ChatbotAskRequestViewModel)` [POST] | AJAX: send prompt, get JSON |
| `SubmitFeedback(ChatbotFeedbackRequestViewModel)` [POST] | Submit chatbot feedback |
| `Escalate(ChatbotEscalationRequestViewModel)` [POST] | Escalate to human |
| `MarkUnresolved(ChatbotUnresolvedRequestViewModel)` [POST] | Mark session unresolved |

### CmsController
**File:** `Controllers/CmsController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?,string?)` | List CMS pages |
| `IndexExport(string,string?,string?)` | Export CMS pages |
| `CreatePage()` | Show page creation form |
| `CreatePage(CmsPageEditViewModel)` [POST] | Create CMS page |
| `EditPage(int)` | Show page edit form |
| `EditPage(int,CmsPageEditViewModel)` [POST] | Update CMS page |
| `DeletePage(int)` [POST] | Delete CMS page |
| `Notices(string?,string?,int,int)` | List CMS notices |
| `NoticesExport(string,string?,string?)` | Export notices |
| `CreateNotice()` | Show notice creation form |
| `CreateNotice(CmsNoticeEditViewModel)` [POST] | Create CMS notice |
| `EditNotice(int)` | Show notice edit form |
| `EditNotice(int,CmsNoticeEditViewModel)` [POST] | Update CMS notice |
| `DeleteNotice(int)` [POST] | Delete CMS notice |
| `Menu()` | List CMS menu items |
| `MenuExport(string)` | Export menu |
| `CreateMenuItem()` | Show menu item form |
| `CreateMenuItem(CmsMenuItemEditViewModel)` [POST] | Create menu item |
| `DeleteMenuItem(int)` [POST] | Delete menu item |
| `NotificationSettings()` | View CMS notification config |

### DashboardController
**File:** `Controllers/DashboardController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | Main dashboard with KPIs & charts |
| `Admin()` | Admin dashboard |
| `Patient()` | Patient-focused dashboard |
| `Appointment()` | Appointment-focused dashboard |
| `Billing()` | Billing-focused dashboard |

### DownloadCenterController
**File:** `Controllers/DownloadCenterController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?)` | List downloadable files |
| `Upload()` | Show upload form |
| `Upload(DownloadFile,IFormFile)` [POST] | Upload file |
| `Download(int)` | Download file & increment counter |
| `Delete(int)` [POST] | Delete file |

### FrontOfficeController
**File:** `Controllers/FrontOfficeController.cs`

| Function | Purpose |
|----------|---------|
| `Index(DateTime?)` | Front office dashboard |
| `Visitors(DateTime?)` | Visitor log |
| `AddVisitor(VisitorPageViewModel)` [POST] | Log visitor |
| `CheckOutVisitor(int,DateTime?)` [POST] | Check out visitor |
| `Complaints(string?)` | View complaints |
| `AddComplaint(ComplaintPageViewModel)` [POST] | Register complaint |
| `UpdateComplaintStatus(int,string,string)` [POST] | Update complaint status |
| `DispatchReceive(string?,DateTime?)` | View dispatch/receive records |
| `AddDispatchReceive(DispatchReceivePageViewModel)` [POST] | Add dispatch/receive record |

### HomeController
**File:** `Controllers/HomeController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | Home/landing page |
| `Privacy()` | Privacy policy page |
| `Error()` | Error page |

### InventoryController
**File:** `Controllers/InventoryController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?,string?)` | List inventory items |
| `Create()` | Show item creation form |
| `Create(InventoryItem)` [POST] | Create inventory item |
| `Edit(int)` | Show item edit form |
| `Edit(int,InventoryItem)` [POST] | Update inventory item |
| `Transactions(int?)` | List inventory transactions |
| `AddTransaction(int?)` | Show transaction form |
| `AddTransaction(InventoryTransaction)` [POST] | Record stock transaction |
| `LowStock()` | View low stock items |

### IPDController
**File:** `Controllers/IPDController.cs`

| Function | Purpose |
|----------|---------|
| `Index(...)` | List IPD admissions with filters |
| `Details(int)` | IPD admission detail |
| `Create(int?)` | Show admission form |
| `Create(CreateIPDAdmissionViewModel)` [POST] | Create IPD admission |
| `Edit(int)` | Show admission edit form |
| `Edit(EditIPDAdmissionViewModel)` [POST] | Update IPD admission |
| `Discharge(int,DateTime)` [POST] | Discharge patient, generate bill |
| `GetAvailableBedsForWard(int)` | AJAX: available beds |
| `GetPatientMedicalHistory(int)` | AJAX: patient history |

### LabController
**File:** `Controllers/LabController.cs`

| Function | Purpose |
|----------|---------|
| `Index(int,int)` | List lab tests |
| `CreateTest()` | Show test creation form |
| `CreateTest(LabTest)` [POST] | Create lab test |
| `EditTest(int)` | Show test edit form |
| `EditTest(int,LabTest)` [POST] | Update lab test |
| `DeleteTest(int)` [POST] | Delete lab test |
| `Results(int,int,string)` | List lab results |
| `OrderTest()` | Show test order form |
| `OrderTest(LabResult)` [POST] | Order lab test |
| `ResultDetails(int)` | View lab result detail |
| `EditResult(int)` | Show result edit form |
| `EditResult(int,LabResult)` [POST] | Update lab result |
| `UpdateResultStatus(int,string)` [POST] | Update result status (AJAX) |
| `DeleteResult(int)` [POST] | Delete lab result |
| `GetPatientLabResults(int)` | AJAX: patient lab results |
| `GetPendingCount()` | AJAX: pending test count |
| `GetTestsByCategory(string)` | AJAX: tests by category |

### LeaveController
**File:** `Controllers/LeaveController.cs`

| Function | Purpose |
|----------|---------|
| `Index(DateTime?,DateTime?,string?,string?)` | List leave requests |
| `Request()` | Show leave request form |
| `Request(LeaveRequestCreateViewModel)` [POST] | Submit leave request |
| `UpdateStatus(int,string,string)` [POST] | Approve/reject leave |
| `Types()` | Manage leave types |
| `CreateType(LeaveTypeViewModel)` [POST] | Create leave type |
| `Balances(int,string?)` | View leave balances |

### LicenseController
**File:** `Controllers/LicenseController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | View license status & history |
| `ExportEntitlementMatrix()` | Export module entitlement CSV |
| `SavePublicKey(LicenseManagementViewModel)` [POST] | Save RSA public key |
| `Upload(IFormFile,IFormFile?)` [POST] | Upload signed license file |
| `LoadFromFile()` [POST] | Load license from disk |
| `Renew(LicenseManagementViewModel)` [POST] | Renew license |
| `SendReminder()` [POST] | Force-send license reminder |
| `Expired(string?)` | Display license-expired page |
| `FeatureLocked(string?,string?,string?)` | Display feature-locked page |

### LiveConsultationController
**File:** `Controllers/LiveConsultationController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | List consultation sessions |
| `Schedule()` | Show scheduling form |
| `Schedule(LiveConsultationSession)` [POST] | Schedule consultation |
| `Details(int)` | View session details |
| `UpdateStatus(int,string)` [POST] | Update session status |
| `Cancel(int)` [POST] | Cancel session |

### MessagingController
**File:** `Controllers/MessagingController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | View inbox |
| `Sent()` | View sent messages |
| `Compose(int?)` | Show compose form |
| `Compose(InternalMessage)` [POST] | Send message |
| `Read(int)` | Read message |
| `Delete(int,string)` [POST] | Delete message |
| `Broadcast()` | Show broadcast form |
| `Broadcast(InternalMessage)` [POST] | Send broadcast |

### ModuleManagementController
**File:** `Controllers/ModuleManagementController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | View module management |
| `Users()` | View user module access |

### NotificationsController
**File:** `Controllers/NotificationsController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | View notifications |
| `MarkAsRead(int)` [POST] | Mark notification as read |
| `MarkAllAsRead()` [POST] | Mark all as read |
| `GetUnreadCount()` | AJAX: unread count |

### OPDController
**File:** `Controllers/OPDController.cs`

| Function | Purpose |
|----------|---------|
| `Index(...)` | List OPD visits with filters |
| `Create(int?)` | Show OPD visit form |
| `Create(CreateOPDVisitViewModel)` [POST] | Create OPD visit |
| `Edit(int)` | Show edit form |
| `Edit(EditOPDVisitViewModel)` [POST] | Update OPD visit |
| `Details(int)` | View OPD visit details |

### OperationTheatreController
**File:** `Controllers/OperationTheatreController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | List OT schedules |
| `Create()` | Show OT case form |
| `Create(OperationTheatreSchedule)` [POST] | Create OT case |
| `Edit(int)` | Show edit form |
| `Edit(int,OperationTheatreSchedule)` [POST] | Update OT case |
| `Details(int)` | View OT case details |

### PatientController
**File:** `Controllers/PatientController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?,int,int)` | List patients with search & pagination |
| `Create()` | Show patient creation form |
| `Create(PatientCreateViewModel)` [POST] | Create patient |
| `Edit(int)` | Show edit form |
| `Edit(int,PatientEditViewModel)` [POST] | Update patient |
| `Details(int)` | View patient details |
| `Delete(int)` | Delete patient |

### PayrollController
**File:** `Controllers/PayrollController.cs`

| Function | Purpose |
|----------|---------|
| `Index(int?,string?)` | List payroll records |
| `Generate()` | Show payroll generation form |
| `Generate(PayrollGenerationViewModel)` [POST] | Generate payroll |
| `Pay(int)` [POST] | Mark payroll as paid |

### PrescriptionController
**File:** `Controllers/PrescriptionController.cs`

| Function | Purpose |
|----------|---------|
| `Index(...)` | List prescriptions |
| `Create(int?)` | Show prescription form |
| `Create(PrescriptionCreateViewModel)` [POST] | Create prescription |
| `Medicines()` | Medicine inventory |
| `CreateMedicine()` | Show medicine form |
| `CreateMedicine(Medicine)` [POST] | Create medicine |
| `EditMedicine(int)` | Show medicine edit form |
| `EditMedicine(int,Medicine)` [POST] | Update medicine |

### PublicSiteAdminController
**File:** `Controllers/PublicSiteAdminController.cs`

| Function | Purpose |
|----------|---------|
| `Settings()` | Public site settings |

### RadiologyController
**File:** `Controllers/RadiologyController.cs`

| Function | Purpose |
|----------|---------|
| `Index(int,int)` | List radiology tests |
| `CreateTest()` | Show test form |
| `CreateTest(RadiologyTest)` [POST] | Create radiology test |
| `EditTest(int)` | Show edit form |
| `EditTest(int,RadiologyTest)` [POST] | Update radiology test |
| `DeleteTest(int)` [POST] | Delete radiology test |
| `Results(int,int,string)` | List radiology results |
| `OrderTest()` | Show order form |
| `OrderTest(RadiologyResult)` [POST] | Order radiology test |
| `ResultDetails(int)` | View result detail |
| `EditResult(int)` | Show result edit form |
| `EditResult(int,RadiologyResult)` [POST] | Update result |
| `UpdateResultStatus(int,string)` [POST] | Update result status (AJAX) |
| `DeleteResult(int)` [POST] | Delete result |
| `GetPatientRadiologyResults(int)` | AJAX: patient results |
| `GetPendingCount()` | AJAX: pending test count |

### ReferralController
**File:** `Controllers/ReferralController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | List referrals |
| `Create()` | Show referral form |
| `Create(Referral)` [POST] | Create referral |
| `Details(int)` | View referral details |
| `UpdateStatus(int,string)` [POST] | Update referral status |

### ReportController
**File:** `Controllers/ReportController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?)` | Reports workspace |
| `Generate()` | Show report generation form |
| `Generate(ReportGenerateViewModel)` [POST] | Generate report |
| `FinancialReport()` | Financial report |
| `Export(string,string)` | Export report |

### SiteController
**File:** `Controllers/SiteController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | Public site home |

### StaffController
**File:** `Controllers/StaffController.cs`

| Function | Purpose |
|----------|---------|
| `Index(string?,int,int)` | List staff with search |
| `Create()` | Show staff creation form |
| `Create(StaffCreateViewModel)` [POST] | Create staff |
| `Edit(string)` | Show edit form |
| `Edit(StaffEditViewModel)` [POST] | Update staff |
| `Delete(string)` | Delete staff |
| `Details(string)` | View staff details |
| `Profile()` | Staff profile page |
| `ChangePassword()` | Show password change form |
| `ChangePassword(StaffPasswordChangeViewModel)` [POST] | Change password |
| `RoleManagement(string)` | Manage staff roles |
| `AddRole(StaffRoleManagementViewModel)` [POST] | Add role to staff |
| `RemoveRole(string,string)` [POST] | Remove role from staff |

### SystemManagementController
**File:** `Controllers/SystemManagementController.cs`

| Function | Purpose |
|----------|---------|
| `ReportManagement()` | Report list management |
| `CreateReport()` | Show report creation form |
| `CreateReport(...)` [POST] | Create report |
| `EditReport(int)` | Show report edit form |
| `EditReport(...)` [POST] | Update report |
| `DownloadReport(int)` | Download report |
| `UserManagement()` | User management |
| `ThemeManagement()` | Theme customization |
| `ThemeStylesheet()` | Dynamic theme CSS |

### TpaController
**File:** `Controllers/TpaController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | List TPA providers |
| `Create()` | Show provider form |
| `Create(TPAProvider)` [POST] | Create TPA provider |
| `Claims()` | List TPA claims |
| `CreateClaim()` | Show claim form |
| `CreateClaim(TPAClaim)` [POST] | Create TPA claim |

---

## Patient Portal Controllers

**Directory:** `Controllers/PatientPortal/`

### AccountController
**File:** `Areas/PatientPortal/Controllers/AccountController.cs`

| Function | Purpose |
|----------|---------|
| `Login()` | Patient portal login |
| `Login(LoginViewModel)` [POST] | Authenticate patient |
| `Register()` | Patient registration |
| `Register(RegisterViewModel)` [POST] | Create patient account |
| `Logout()` [POST] | Sign out patient |
| `Profile()` | Patient profile |
| `EditProfile()` | Edit profile form |

### AppointmentsController
**File:** `Areas/PatientPortal/Controllers/AppointmentsController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | My appointments |
| `Create()` | Book appointment |
| `Create(AppointmentCreateViewModel)` [POST] | Submit booking |
| `Cancel(int)` [POST] | Cancel appointment |

### MedicalRecordsController
**File:** `Areas/PatientPortal/Controllers/MedicalRecordsController.cs`

| Function | Purpose |
|----------|---------|
| `Index()` | My medical records |

---

## Services (Business Logic)

### Core Entity Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `ISettingService` | `SettingService` | `Services/Implementations/SettingService.cs` | Hospital settings, feature toggles, language config |
| `IPatientService` | `PatientService` | `Services/Implementations/PatientService.cs` | Patient CRUD, search |
| `IAppointmentService` | `AppointmentService` | `Services/Implementations/AppointmentService.cs` | Appointment CRUD, filtering |
| `IBillingService` | `BillingService` | `Services/Implementations/BillingService.cs` | Bill CRUD, payments, revenue |
| `IAuditService` | `AuditService` | `Services/Implementations/AuditService.cs` | Activity logging, audit queries |
| `IFileService` | `FileService` | `Services/Implementations/FileService.cs` | File upload/download/delete |
| `IStaffService` | `StaffService` | `Services/Implementations/StaffService.cs` | Staff CRUD, passwords, roles, statistics |

### Clinical Module Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IOPDService` | `OPDService` | `Services/Implementations/OPDService.cs` | OPD visit CRUD, revenue |
| `IIPDService` | `IPDService` | `Services/Implementations/IPDService.cs` | IPD admission CRUD, discharge |
| `IWardService` | `WardService` | `Services/Implementations/WardService.cs` | Ward CRUD, bed tracking |
| `IBedService` | `BedService` | `Services/Implementations/BedService.cs` | Bed CRUD, assign/release/transfer |
| `IPrescriptionService` | `PrescriptionService` | `Services/Implementations/PrescriptionService.cs` | Prescriptions, medicines, pharmacy bills |

### Diagnostic Module Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `ILabService` | `LabService` | `Services/Implementations/LabService.cs` | Lab test catalog, results, revenue |
| `IRadiologyService` | `RadiologyService` | `Services/Implementations/RadiologyService.cs` | Radiology tests, results, revenue |

### Specialized Module Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IBloodBankService` | `BloodBankService` | `Services/Implementations/BloodBankService.cs` | Blood inventory, issuing |
| `IOperationTheatreService` | `OperationTheatreService` | `Services/Implementations/OperationTheatreService.cs` | OT schedule CRUD |
| `IReferralService` | `ReferralService` | `Services/Implementations/ReferralService.cs` | Referral CRUD, status |

### Administrative Module Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IAttendanceService` | `AttendanceService` | `Services/Implementations/AttendanceService.cs` | Staff attendance, check-in/out |
| `ILeaveService` | `LeaveService` | `Services/Implementations/LeaveService.cs` | Leave types, requests, balances |
| `IPayrollService` | `PayrollService` | `Services/Implementations/PayrollService.cs` | Payroll generation, payments |
| `IFrontOfficeService` | `FrontOfficeService` | `Services/Implementations/FrontOfficeService.cs` | Visitors, complaints, dispatch |
| `ICertificateService` | `CertificateService` | `Services/Implementations/CertificateService.cs` | Certificates & ID cards |
| `IReportService` | `ReportService` | `Services/Implementations/ReportService.cs` | Department, financial, occupancy, custom reports |
| `IReportTemplateService` | `ReportTemplateService` | `Services/Implementations/ReportTemplateService.cs` | Custom report templates |

### Authorization & Modules

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IAuthorizationService` | `AuthorizationService` | `Services/Implementations/AuthorizationService.cs` | Permission checks, role management |
| `IModuleService` | `ModuleService` | `Services/Implementations/ModuleService.cs` | Module visibility, per-user access |

### Patient Portal Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IPatientPortalService` | `PatientPortalService` | `Services/Implementations/PatientPortalService.cs` | Patient self-service operations |

### Chatbot / AI Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IChatbotService` | `OpenAiChatbotService` | `Services/Implementations/OpenAiChatbotService.cs` | OpenAI chatbot |
| `IChatbotModerationService` | `ChatbotModerationService` | `Services/Implementations/ChatbotModerationService.cs` | Content moderation |
| `IChatbotPromptBuilder` | `ChatbotPromptBuilder` | `Services/Implementations/ChatbotPromptBuilder.cs` | System prompt construction |
| `IChatbotKnowledgeService` | `ChatbotKnowledgeService` | `Services/Implementations/ChatbotKnowledgeService.cs` | Context retrieval |
| `IChatbotConsentService` | `ChatbotConsentService` | `Services/Implementations/ChatbotConsentService.cs` | GDPR consent management |
| `IChatbotPiiRedactionService` | `ChatbotPiiRedactionService` | `Services/Implementations/ChatbotPiiRedactionService.cs` | PII redaction |
| `IChatbotDataCleanupService` | `ChatbotDataCleanupService` | `Services/Implementations/ChatbotDataCleanupService.cs` | Data cleanup |

### Licensing Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `ILicenseService` | `LicenseService` | `Services/Implementations/LicenseService.cs` | License status, renewal, reminders |
| `ILicenseFileService` | `LicenseFileService` | `Services/Implementations/LicenseFileService.cs` | License file validation & crypto |

### Notification Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IEmailNotificationProvider` | `SmtpEmailNotificationProvider` | `Services/Implementations/SmtpEmailNotificationProvider.cs` | SMTP email |
| `ISmsNotificationProvider` | `TwilioSmsNotificationProvider` | `Services/Implementations/TwilioSmsNotificationProvider.cs` | Twilio SMS |
| `ISmsNotificationProvider` | `SmsNotificationProviderRouter` | `Services/Implementations/SmsNotificationProviderRouter.cs` | SMS router |
| `INotificationDeliveryAuditService` | `NotificationDeliveryAuditService` | `Services/Implementations/NotificationDeliveryAuditService.cs` | Delivery audit |
| `ISystemNotificationService` | `SystemNotificationService` | `Services/Implementations/SystemNotificationService.cs` | In-app notifications |
| `IPublicBookingNotificationService` | `PublicBookingNotificationService` | `Services/Implementations/PublicBookingNotificationService.cs` | Booking confirmations |

### Infrastructure Services

| Interface | Implementation | File | Purpose |
|-----------|---------------|------|---------|
| `IExportService` | `ExportService` | `Services/Implementations/ExportService.cs` | CSV, PDF, Excel export |
| `ICacheService` | `CacheService` | `Services/Implementations/CacheService.cs` | Memory caching |
| `IConcurrentSessionService` | `ConcurrentSessionService` | `Services/Implementations/ConcurrentSessionService.cs` | Session enforcement |
| `ISmtpHealthService` | `SmtpHealthService` | `Services/Implementations/SmtpHealthService.cs` | SMTP health check |
| `IPaymentGatewayService` | `PaymentGatewayService` | `Services/Implementations/PaymentGatewayService.cs` | Multi-gateway payments |
| `IReportCatalogVisibilityService` | `ReportCatalogVisibilityService` | `Services/Implementations/ReportCatalogVisibilityService.cs` | Report visibility |

### Background & Utility Classes
**File:** `Services/Implementations/`

| Class | Purpose |
|-------|---------|
| `DatabaseInitializer` | Seeds identity roles, users, and initial DB schema on startup |
| `DemoDataSeeder` | Idempotent seeding of realistic demo/dummy data |
| `LicenseCryptoUtility` | Cryptographic payload canonicalization, signing, verification |
| `LicenseReminderHostedService` | Background job: license expiry reminders |
| `ChatbotDataCleanupHostedService` | Background job: cleanup expired chatbot data |
| `AfricaTalkingSmsNotificationProvider` | Africa's Talking SMS gateway integration |

---

## Summary

| Category | Count |
|----------|-------|
| Main Controllers | 31 |
| Patient Portal Controllers | 3 |
| Total Controller Actions | ~300 |
| Service Interfaces | 47 |
| Service Implementations | 49 |
| Background/Hosted Services | 2 |
| Utility/Seeder Classes | 4 |
| UI Feature (Phase 1) | 1 JS file (`sidebar-toggle.js`) — no backend functions |
| UI Feature (Phase 2) | 0 — CSS-only, no backend functions |
