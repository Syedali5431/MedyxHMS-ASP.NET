# Database Bootstrap Scripts

This folder contains SQL and PowerShell scripts for database bootstrap and validation.

## New Database Scripts

- `New-Database.sql`
  - Creates database `MedyxHMS` if missing.
  - Creates full schema (tables, constraints, indexes).
  - Applies baseline seed data for core startup usage (roles, features, settings, SuperAdmin mapping).
  - Creates utility views and stored procedures.

- `New-Database-Empty.sql`
  - Creates database `MedyxHMS` if missing.
  - Creates full schema (tables, constraints, indexes).
  - Does not include baseline data inserts.
  - Keeps utility views and stored procedures.

## Run From SSMS (SQL Server Management Studio)

1. Open SSMS and connect to your server.
2. Open either:
   - `scripts/New-Database.sql`, or
   - `scripts/New-Database-Empty.sql`
3. Execute the script.

## Run From sqlcmd

### LocalDB example

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
