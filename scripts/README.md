# Scripts — Run Sequence

This folder contains database bootstrap, migration, testing, and automation scripts for MedyxHMS.

---

## 🚀 Fresh Deployment Sequence

Run these **in order** when setting up a new environment:

| # | Script | What It Does |
|---|--------|--------------|
| 1 | `New-Database.sql` | **Full database** — schema, roles, users, modules, settings, and baseline seed data |
| 2 | `New-Database-Empty.sql` | **Empty database** — schema only, no seed data (use for staging/validation) |
| 3 | `SeedDemoData.sql` | **Demo data** — departments, doctors, staff, patients, appointments, OPD, IPD, wards, beds, bills, pharmacy, lab, radiology, blood bank (idempotent — safe to re-run) |
| 4 | `StoredProcedures_Reports.sql` | **Report stored procedures** — optimized SPs for report generation (auto-deployed by app on startup) |
| 5 | `MFA-Migration.sql` | **MFA migration** — adds MFA columns to existing `AspNetUsers` (one-time, run once per DB) |

> **Note:** `StoredProcedures_Reports.sql` is automatically applied by the app via `DatabaseInitializer.EnsureReportStoredProceduresAsync()` on startup. Manual execution is optional but harmless.

---

## 🔧 Validation & Testing Scripts

| # | Script | What It Does |
|---|--------|--------------|
| 6 | `Validate-DatabaseDeployment.ps1` | Validates that `New-Database.sql` (`-Full`) or `New-Database-Empty.sql` (`-Empty`) deploys correctly to a temp database, then cleans up |
| 7 | `Run-RoleModuleSmoke.ps1` | Hits every staff-side and patient-portal route for each test user, verifying HTTP 200 responses. Outputs a JSON report |
| 8 | `Invoke-UatSmoke.ps1` | Orchestrates UAT smoke testing — builds, generates license, seeds DB, runs role/module route checks. Driven by `UAT-Smoke.config.template.json` |
| 9 | `UAT-Smoke.config.template.json` | Configuration template for `Invoke-UatSmoke.ps1` — base URL, license settings, tenant, modules |

---

## 🔑 License Tool Automation

| # | Script | What It Does |
|---|--------|--------------|
| 10 | `Invoke-LicenseToolAutomation.ps1` | Automates the `MedyxHMS-Lic` CLI — generates private keys and `.lic` license files with modules, expiry, and tenant configuration |

---

## Quick Start

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\scripts\New-Database.sql"
```

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\scripts\New-Database-Empty.sql"
```

### SQL Server instance example

```powershell
sqlcmd -S "YOUR_SERVER\\INSTANCE" -E -i ".\scripts\New-Database.sql"
```

```powershell
sqlcmd -S "YOUR_SERVER\\INSTANCE" -E -i ".\scripts\New-Database-Empty.sql"
```

If SQL authentication is required, replace `-E` with `-U <username> -P <password>`.

## Demo Data

- `SeedDemoData.sql`
  - Inserts realistic demo rows (patients, doctors, staff, appointments, OPD/IPD,
    billing, pharmacy, lab, radiology, blood bank, front office, attendance,
    payroll, OT schedules, referrals) into an already-deployed `MedyxHMS` database.
  - Run after `New-Database.sql` (or `New-Database-Empty.sql`) has created the schema.
  - Not invoked automatically by the app — for LocalDB development, the app seeds
    its own smaller demo dataset at startup via `DemoDataSeeder` instead
    (`Services/Implementations/DemoDataSeeder.cs`). Use this script for a real
    SQL Server deployment (staging/UAT) that needs fuller demo data.

- `Import-HospitalDemoData.ps1`
  - Wrapper that runs `SeedDemoData.sql` against a target SQL Server instance.

## Indexing

- `CreateIndexes.sql`
  - Adds supplemental indexes on top of the FK indexes `New-Database.sql`
    already creates — mainly date-range columns used by dashboard/report
    queries (AppointmentDate, BillDate, AdmissionDate, etc.) plus two
    computed-column indexes for the `Patients.PatientId` and
    `Bills.BillNumber` business keys. Idempotent — safe to re-run.
  - Run after the database has been created and (optionally) seeded:
    ```powershell
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d MedyxHMS -i "scripts/CreateIndexes.sql"
    ```

## Validation Scripts

- `data-migration-validation.sql`: record and integrity checks.
- `compare-migration-counts.ps1`: source vs target count comparison.
- `source-count-snapshot.template.csv`: source count template.

## UAT Smoke Automation

- `Invoke-UatSmoke.ps1`
  - Builds the ASP.NET project and MedyxHMS-Lic desktop tool.
  - Runs the automated test suite.
  - Optionally generates a smoke-test `.lic` file.
  - Optionally checks HTTP reachability for `/`, `/Account/Login`, `/Chatbot`, and `/health`.

- `Invoke-LicenseToolAutomation.ps1`
  - Automates the interactive MedyxHMS-Lic console workflow for key generation and license creation.

- `UAT-Smoke.config.template.json`
  - Template configuration for `Invoke-UatSmoke.ps1`.

### Example

```powershell
pwsh .\scripts\Invoke-UatSmoke.ps1 -BaseUrl "https://localhost:5001"
```

### Full deployment walkthrough

```powershell
# 1. Deploy full database
sqlcmd -S . -i New-Database.sql -E

# 2. Add demo data
sqlcmd -S . -d MedyxHMS -i SeedDemoData.sql -E

# 3. Validate deployment
.\Validate-DatabaseDeployment.ps1 -Full

# 4. Run role/module smoke test
.\Invoke-UatSmoke.ps1 -ConfigPath UAT-Smoke.config.template.json
```
