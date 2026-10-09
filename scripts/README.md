# Scripts — Run Sequence

This folder contains the database scripts and the test / automation scripts for MedyxHMS.

The database scripts match the current application: 117 tables, 132 foreign keys, the report
procedures and the reporting indexes (October 2026: discharge reports `DischargeSummaries` and the My Profile
columns of `AspNetUsers` – gender, date of birth, address, city, emergency contact, about). All of them are **safe to run more than once** and print a
`Note:` line (instead of stopping) for anything an existing database prevents.

The `.sql` files are saved as UTF-8 with a byte-order mark, so `sqlcmd` and SSMS read names with
dashes or accents correctly – no `-f 65001` switch is needed. Keep that encoding when you edit them.

---

## 🚀 New installation

Run these **in order** (SQL Server 2017 or later; `-b` stops at the first error):

| # | Script | Command | What it does |
|---|--------|---------|--------------|
| 1 | `New-Database.sql` | `sqlcmd -S <server> -E -b -i New-Database.sql` | Creates the database `MedyxHMS` and the full schema, then the **baseline data** the application needs: 12 roles with their permissions, 30 modules, 120 settings, the default hospital *Main Hospital*, the public website pages/menu and the SuperAdmin account. |
| 1 (alt.) | `New-Database-Empty.sql` | `sqlcmd -S <server> -E -b -i New-Database-Empty.sql` | Same schema, **no data**. The application inserts the baseline data itself the first time it starts. |
| 2 | `StoredProcedures_Reports.sql` | `sqlcmd -S <server> -E -b -d MedyxHMS -i StoredProcedures_Reports.sql` | Report stored procedures. Optional: the application runs this file at every start-up. |
| 3 | `CreateIndexes.sql` | `sqlcmd -S <server> -E -b -d MedyxHMS -i CreateIndexes.sql` | Extra indexes for dashboards and reports (dates, business keys). Optional, recommended. |
| 4 | `SeedDemoData.sql` | `sqlcmd -S <server> -E -b -d MedyxHMS -i SeedDemoData.sql` | Demo data (departments, doctors, patients, appointments, OPD/IPD, bills, pharmacy, lab, radiology, blood bank, front office, HR, OT, referrals), **three hospitals** (Main Hospital – Lahore, City General Hospital – Islamabad, Karachi Care Hospital – Karachi) with their own accounts, wards, beds and recent activity, **inventory** (vendors, stock items per hospital, stock movements, purchase bills), the currency **PKR** and the test account `tester` (every role except SuperAdmin, all three hospitals, no two-step login). Optional — demo / test databases only. |

**SuperAdmin sign-in** (database from `New-Database.sql`): `superadmin` / `SuperAdmin@123!`.
Change the password after the first sign-in (My Profile); administrators must set up two-step login
(authenticator app) at the first sign-in. With `New-Database-Empty.sql` the application creates the
SuperAdmin with the password in `Seeding:SuperAdminInitialPassword` (appsettings), or generates one and
writes it to the log once.

**Test accounts (development only):** when the application runs with `Seeding:UatAccounts = true`
(appsettings.Development.json) it creates one account per role, all with the password `UatRole@123!`.
Staff sign in at `/Account/Login`, the patient at `/PatientPortal/Account/Login`.

| User name | Role | Start page |
|-----------|------|------------|
| `superadmin` (password `SuperAdmin@123!`) | SuperAdmin | Dashboard |
| `admin.uat` | Admin | Dashboard |
| `doctor.uat` | Doctor | OPD |
| `nurse.uat` | Nurse | IPD |
| `accountant.uat` | Accountant | Billing |
| `receptionist.uat` | Receptionist | Front Office |
| `multirole.uat` | Doctor + Nurse | OPD |
| `patient.uat` | Patient (portal) | Patient Portal |
| `tester` (password `Tester@123!`) | **every role except SuperAdmin** (Admin, Doctor, Nurse, Staff, Receptionist, Pharmacist, LabTechnician, Pathologist, Radiologist, Accountant) – created by `SeedDemoData.sql`; works in all three hospitals and chooses one on the sign-in page | Dashboard |

**Hospital accounts (demo data, `SeedDemoData.sql`):** password `Hospital@123!` for all of them.
Each account works only in its own hospital; the hospital is chosen on the sign-in page when an account
has more than one.

| Hospital | Accounts |
|----------|----------|
| Main Hospital (Lahore, `MAIN`, default) | `admin.main`, `doctor.main`, `nurse.main`, `reception.main`, `accounts.main`, `pharmacy.main`, `lab.main` |
| City General Hospital (Islamabad, `ISB`) | `admin.isb`, `doctor.isb`, `nurse.isb`, `reception.isb`, `accounts.isb`, `pharmacy.isb`, `lab.isb` |
| Karachi Care Hospital (Karachi, `KHI`) | `admin.khi`, `doctor.khi`, `nurse.khi`, `reception.khi`, `accounts.khi`, `pharmacy.khi`, `lab.khi` |
| Islamabad **and** Karachi | `admin.region` (Admin of two hospitals – switches between them in the top bar) |

`superadmin` sees every hospital and "All hospitals", adds hospitals and assigns hospitals to any account
(User Management → hospital button, or Hospitals / Branches → Staff Access). An Admin manages only the
hospitals assigned to them and cannot change the hospitals of other administrators.

Administrators (`superadmin`, `admin.uat`) must set up two-step login with an authenticator app at the
first sign-in. `tester` and the demo hospital admins (`admin.main`, `admin.isb`, `admin.khi`,
`admin.region`) are not asked: they are listed under Security & Backups → **Test accounts without
two-step login** (SuperAdmin accounts can never be listed there). Switch `Seeding:UatAccounts` off, do not run `SeedDemoData.sql`, and remove these accounts (and clear the
test-account list) on a production database.

**First start – licence:** a new database has no licence, so until one is installed only the SuperAdmin
can sign in (other users see *"Valid signed license not found."*). Copy the signed `MedyxHMS.lic` into the
application folder: at start-up the application imports it automatically whenever the active licence is
missing, expired or not signed with a trusted vendor key. A SuperAdmin can also use **License** → **Load from
file** or upload the `.lic` file there. The matching public key is read from `MedyxHMS-Lic\current`.

**Licence keys:** the application accepts only licences signed with the vendor keys listed in
`Services/Implementations/LicenseTrust.cs` (public keys in Settings or in `MedyxHMS-Lic\current` that are not
listed there are ignored). The vendor **private** key (`medyxhms-private-key-*.json`) must stay on the vendor's
computer – it is never committed (`.gitignore`) and never copied to a server; `.lic` files are not committed
either. To replace the vendor key: generate a new pair with `MedyxHMS-Lic` (option 1), add its verification
key to `LicenseTrust.cs` (and remove the old one), put the new public key in `MedyxHMS-Lic\current`, sign new
licences (option 2) and place `MedyxHMS.lic` in each installation's folder before starting the new version.

**Another database name:** in `New-Database.sql` / `New-Database-Empty.sql` replace `[MedyxHMS]` and
`N'MedyxHMS'` at the top of the file. Scripts 2–4 and `MFA-Migration.sql` run in the database given with
`-d` (or selected in SSMS), so they work with any name; they refuse to run in `master`.

---

## ⬆️ Updating an existing database

Run `New-Database.sql` (or `New-Database-Empty.sql`) against the existing database, then scripts 2 and 3.
On an existing database the scripts only **add** what is missing — tables, columns, indexes, foreign keys
(existing rows are not re-checked) — and never drop or change anything. Baseline data is only inserted
into an empty database; the application adds missing baseline rows (new roles, permissions, settings,
modules) itself at start-up.

| Script | What it does |
|--------|--------------|
| `MFA-Migration.sql` | Adds the MFA columns to `AspNetUsers` of a database from before June 2026. Not needed when you run `New-Database.sql` (which adds them too); kept for manual updates. |

The application itself also adds the discharge table and the profile columns to an existing database at
start-up, and removes the old `Printing:ReceiptPaperWidth` setting (the paper width is chosen on the receipt).

Tested on: a new database, a second run on the same database, the live database (nothing to add), a
backup from 4 October 2026 (44 tables/columns added) and a database created by the application itself
(column types `nvarchar(450)`/`nvarchar(max)`: three optional objects are reported as notes).

---

## 🔧 Validation & Testing Scripts

| # | Script | What It Does |
|---|--------|--------------|
| 5 | `Validate-DatabaseDeployment.ps1` | Deploys `New-Database.sql` (`-Full`) or `New-Database-Empty.sql` (`-Empty`) to a temporary database, runs it a second time (must change nothing), optionally runs the other scripts twice (`-AllScripts`), checks tables, foreign keys (all trusted), procedures and the baseline rows, then drops the temporary database. Fails on any SQL error or note. `-SqlServer` (default `.\SQLEXPRESS`), `-Keep` keeps the database. |
| 6 | `Run-RoleModuleSmoke.ps1` | Signs in as each test account (UAT accounts, `tester` and the demo hospital accounts) and opens every staff page of the sidebar (including Discharge Reports), My Profile and the AI Assistant, or the patient-portal pages (including My Profile and Discharge Reports). Hospital accounts also check the hospital choice at sign-in (number of hospitals offered, the chosen hospital shown afterwards). Accounts that must use two-step login are reported as such. Default URL `http://localhost:5000` (`-BaseUrl`); writes a JSON report to `temp_build_output\uat-role-run-current.json`. |
| 7 | `Invoke-UatSmoke.ps1` | Orchestrates UAT smoke testing — builds the web app and licence tool, runs every test project under `tests\`, generates a licence, checks the main URLs. Driven by `UAT-Smoke.config.template.json` |
| 8 | `UAT-Smoke.config.template.json` | Configuration template for `Invoke-UatSmoke.ps1` — base URL, license settings, tenant, modules |

```powershell
.\Validate-DatabaseDeployment.ps1 -Full -AllScripts
.\Validate-DatabaseDeployment.ps1 -Empty -AllScripts -SqlServer "(localdb)\MSSQLLocalDB"
```

---

## 🔑 License Tool Automation

| # | Script | What It Does |
|---|--------|--------------|
| 9 | `Invoke-LicenseToolAutomation.ps1` | Automates the `MedyxHMS-Lic` CLI — generates private keys and `.lic` license files with modules, expiry, and tenant configuration |

---

## Full deployment walkthrough

```powershell
# 1. Database with baseline data
sqlcmd -S .\SQLEXPRESS -E -b -i New-Database.sql

# 2. Report procedures and reporting indexes
sqlcmd -S .\SQLEXPRESS -E -b -d MedyxHMS -i StoredProcedures_Reports.sql
sqlcmd -S .\SQLEXPRESS -E -b -d MedyxHMS -i CreateIndexes.sql

# 3. Demo data (optional)
sqlcmd -S .\SQLEXPRESS -E -b -d MedyxHMS -i SeedDemoData.sql

# 4. Validate the scripts (temporary database)
.\Validate-DatabaseDeployment.ps1 -Full -AllScripts

# 5. Smoke test
.\Invoke-UatSmoke.ps1 -ConfigPath UAT-Smoke.config.template.json
```

Set `ConnectionStrings:DefaultConnection` (appsettings / environment) to the database, then start the
application. For a production database switch off `Seeding:DemoData` and `Seeding:UatAccounts`.

## Notes

- `SeedDemoData.sql` inserts rows with fixed ids and skips ids that already exist; demo patients have no
  portal login. The hospital demo data (ids 101 and up) uses dates relative to the day the script runs, so
  the reports show figures for the current month. For LocalDB development the application can also seed its own smaller demo set at
  start-up (`Seeding:DemoData`, `Services/Implementations/DemoDataSeeder.cs`).
- **2026 sample data:** the application adds sample records for January–October 2026 to every module (all
  hospitals: appointments, OPD visits with bills and payments, admissions with discharge reports and bills,
  laboratory, radiology, pharmacy, operations with their bills, referrals, insurance claims, blood bank,
  ambulance, births and deaths, front office, leave, payroll, training, quality, equipment, live consultations
  and website requests) once at start-up when `Seeding:Year2026SampleData` or `Seeding:DemoData` is `true`
  (`Services/Implementations/Year2026SampleDataSeeder.cs`). It runs only once per database (settings
  `SampleData:Year2026…`) and continues the system's own numbering. Example for an existing test database:
  start the application once with the environment variable `Seeding__Year2026SampleData=true`. Do not use it on
  a production database.
- **AI Assistant:** answers with built-in guidance until `OpenAI:Enabled = true` and `OpenAI:ApiKey` are set
  (environment variables `OpenAI__Enabled` and `OpenAI__ApiKey`; never commit a key to appsettings).
- `CreateIndexes.sql` adds date-range indexes (AppointmentDate, BillDate, AdmissionDate, …) and two
  computed-column unique indexes for `Patients.PatientId` and `Bills.BillNumber`.
- Older databases may still have the columns `Appointments.AppointmentId` and `SavedReports.ExportFormats /
  Status / ExpiresAt / IsPublic / ScheduleId`; the application no longer uses them and new databases are
  created without them.
