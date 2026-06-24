# Database Bootstrap Scripts

This folder contains SQL and PowerShell scripts for database bootstrap and validation.

## New Database Scripts

- `New-Database.sql`
  - Creates database `MedyxHMS` if missing.
  - Creates schema (tables, constraints, indexes) — 67 core tables.
  - Includes MFA columns on AspNetUsers (MFAEnabled, MFASecretKey, MFATempSecret, MFARecoveryCodes).
  - Applies baseline seed data for core startup usage (roles, features, settings, SuperAdmin mapping).

- `New-Database-Empty.sql`
  - Creates database `MedyxHMS` if missing.
  - Creates schema (tables, constraints, indexes) — 67 core tables.
  - Includes MFA columns on AspNetUsers.
  - Does not include baseline data inserts.

- `New-Database.Validation.sql`
  - Schema + seed data with validation checks.
  - Includes MFA columns.

> **Note:** These scripts contain 67 core tables. The full database has 96 tables.
> Additional tables (28) are created dynamically by `DatabaseInitializer.Ensure*Async()` methods at app startup.
> **Recommended deployment:** Run the app once — `EnsureCreatedAsync()` creates any missing tables.
> For full manual deployment, use SQL Server Management Studio → Tasks → Generate Scripts from the live database.

## MFA Migration

- `MFA-Migration.sql`
  - Standalone script to add MFA columns (MFAEnabled, MFASecretKey, MFATempSecret, MFARecoveryCodes) to an existing AspNetUsers table.
  - Safe to run on any existing database.

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
