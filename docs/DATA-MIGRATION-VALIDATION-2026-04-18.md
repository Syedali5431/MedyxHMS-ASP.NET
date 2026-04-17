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

sqlcmd -S localhost -d MedyxHMS -E -i "scripts\\data-migration-validation.sql" -W -s ","

Observed result:
- Connection failure to localhost SQL Server instance
- Error summary:
  - Named Pipes Provider: Could not open a connection to SQL Server
  - Login timeout expired
  - Server not found or not accessible

## Secondary Instance Check
Command used:

sqlcmd -S "(localdb)\\MSSQLLocalDB" -d MedyxHMS -E -Q "SELECT DB_NAME() AS DatabaseName"

Observed result:
- Login succeeded to instance but target database unavailable for current login
- Error summary:
  - Cannot open database "MedyxHMS" requested by the login

## Status
- Validation script creation: Completed
- Validation execution against accessible target DB: Blocked (database/instance not reachable from current environment)

## Next Action
Run the same script against the actual reachable SQL Server host containing the MedyxHMS migrated database, then record the output and reconcile any non-zero integrity issues.
