# SQL Script Validation Report (2026-04-22)

## Objective
Execute a controlled dry-run validation of the ASP.NET bootstrap script after the NormalizedUserName seed fix.

## Script Under Test
- scripts/New-Database.sql

## Validation Method
1. Created an isolated temporary script copy with database name remapped to `MedyxHMS_Validation_20260422`.
2. Executed full script with sqlcmd in fail-fast mode (`-b`) from master context.
3. Verified seed row values for `superadmin-user-id` in temporary database.
4. Dropped temporary validation database after evidence capture.

## Result
- Dry-run execution status: PASS
- sqlcmd exit code: 0
- Blocking SQL errors: none

## Seed Data Verification (Targeted Fix)
Validated the inserted superadmin identity row:
- UserName: superadmin
- NormalizedUserName: SUPERADMIN
- NormalizedEmail: SUPERADMIN@HOSPITAL.COM

This confirms the previous failure condition is resolved:
- `Cannot insert the value NULL into column 'NormalizedUserName'` no longer reproduces.

## Evidence Files
- temp_build_output/sql-validation-2026-04-22.log
- temp_build_output/sql-validation-seed-check-2026-04-22.log

## Notes
- Validation was executed against a disposable database context to avoid impact on operational data.
- Temporary validation database was removed after completion.
