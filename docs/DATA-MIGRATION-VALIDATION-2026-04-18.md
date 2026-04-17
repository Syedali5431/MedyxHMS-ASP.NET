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

## Next Action
Run the same script against the SQL Server instance containing migrated production-like data, compare counts with source snapshot, and reconcile any non-zero integrity issues.
