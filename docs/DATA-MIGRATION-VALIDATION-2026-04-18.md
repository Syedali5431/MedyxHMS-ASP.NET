# Data Migration Validation - April 18, 2026

## Scope
This document tracks the Step 5.2 data migration validation attempt and provides the executable validation script used for record count and integrity checks.

## Validation Script
- SQL script: scripts/data-migration-validation.sql
- Coverage:
  - Core record counts (Patients, Doctors, Appointments, Bills, BillItems, Payments, PublicAppointmentRequests, Staff)
  - Referential-integrity orphan checks
  - Billing consistency checks (paid > total, negative pending)
  - Duplicate PatientId check

## Execution Attempt
Command used:

sqlcmd -S "(localdb)\\MSSQLLocalDB" -d MedyxHMS -E -i "scripts\\data-migration-validation.sql" -W -s ","

Observed result:
- Validation script executed successfully on LocalDB-backed `MedyxHMS`
- Record counts returned (baseline dataset with seeded SuperAdmin staff profile)
- Integrity checks returned 0 issues for all checks

## Notes From Provisioning
- Initial startup provisioning surfaced seeding issues in `DatabaseInitializer` (staff-role linkage and required staff email).
- Seeding logic was updated to ensure SuperAdmin has a linked Staff profile and role assignment without violating constraints.

## Status
- Validation script creation: Completed
- Validation execution against accessible target DB: Completed (LocalDB baseline)
- Validation execution against real migrated dataset: Pending

## Environment Check for Real Dataset
- Checked common local SQL Server targets: `localhost`, `.\\SQLEXPRESS`, `(localdb)\\MSSQLLocalDB`
- Result:
  - `localhost`: not reachable
  - `.\\SQLEXPRESS`: instance not present/reachable
  - `(localdb)\\MSSQLLocalDB`: reachable, but only baseline `MedyxHMS` database is available in current environment
- Checked documented external import files referenced by conversion docs:
  - `c:\Users\alin\Downloads\Medyx-HMS\hospitaldemo_db.sql`
  - `c:\Databases\Medyx-HMS\Medyx-HMS-php\php-original\Scripts\NewDatabase.sql`
- Result: neither file exists on this machine at those paths
- Checked for installed SQL Server services (`MSSQL*`, `SQLAgent*`, `SQLBrowser`)
- Result: no full SQL Server service/instance is installed or running on this machine
- Searched likely local roots for migrated artifacts (`c:\Databases`, `c:\Users`, `d:\`, `e:\`) using Medyx/HMS/hospitaldemo filename filters
- Result: no `NewDatabase.sql`, `hospitaldemo_db.sql`, or matching `.bak` backup file was found
- Reconfirmed accessible LocalDB content:
  - `Patients`: 0
  - `Staff`: 1
  - `Appointments`: 0
  - `Bills`: 0
  - `Payments`: 0
  - `PublicAppointmentRequests`: 0

Conclusion:
- The real migrated SQL Server dataset is not currently available from this environment, so record-count comparison against the real migrated data cannot be completed yet.

## Next Action
Provide one of the following so validation can be completed:
- reachable SQL Server instance name hosting the migrated MedyxHMS dataset, or
- restored/imported migrated SQL script/dump on this machine

After that, run the same validation script, capture counts, compare with the source snapshot, and reconcile any non-zero integrity issues.
