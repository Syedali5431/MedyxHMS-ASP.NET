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

## Validation Scripts

- `data-migration-validation.sql`: record and integrity checks.
- `compare-migration-counts.ps1`: source vs target count comparison.
- `source-count-snapshot.template.csv`: source count template.
