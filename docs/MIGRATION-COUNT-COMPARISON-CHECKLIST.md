# Migration Count Comparison Checklist

## Purpose

This checklist gives operations a single-run command flow to validate migrated SQL Server row counts against the approved source snapshot and return evidence artifacts for sign-off.

## Inputs Required

- Target SQL Server instance name (for example `PROD-SQL01\\MSSQLSERVER`)
- Target database name (for example `MedyxHMS`)
- Approved source snapshot CSV copied from `scripts/source-count-snapshot.template.csv` and filled with real source counts
- Output folder path for evidence artifacts
- SQL authentication credentials (only if integrated security is not used)

## Pre-Run Checks

1. Confirm the source snapshot CSV has values for all required checks and no blank `SourceCount` cells.
2. Confirm the SQL account has read access to all referenced tables/views.
3. Confirm PowerShell can run scripts in the current session.

## Single-Run Command (Integrated Security)

Run from repository root:

```powershell
pwsh ./scripts/compare-migration-counts.ps1 \
  -ServerInstance "PROD-SQL01\\MSSQLSERVER" \
  -Database "MedyxHMS" \
  -SourceSnapshotCsv "./scripts/source-count-snapshot.csv" \
  -OutputFolder "./migration-evidence" \
  -UseIntegratedSecurity:$true
```

## Single-Run Command (SQL Authentication)

```powershell
pwsh ./scripts/compare-migration-counts.ps1 \
  -ServerInstance "PROD-SQL01\\MSSQLSERVER" \
  -Database "MedyxHMS" \
  -SourceSnapshotCsv "./scripts/source-count-snapshot.csv" \
  -OutputFolder "./migration-evidence" \
  -UseIntegratedSecurity:$false \
  -Username "migration_reader" \
  -Password "<secure-password>"
```

## Pass/Fail Criteria

- Exit code `0`: all counts matched.
- Exit code `2`: one or more mismatches found.
- Any other non-zero exit: execution failure (connectivity, permissions, or script runtime issue).

## Evidence Artifacts to Return

1. `target-counts-*.csv`
2. `comparison-report-*.csv`
3. Console output transcript showing command, timestamp, and exit code
4. Optional: screenshot or ticket note confirming server/database used

## Ops Handover Template

Use this block in the sign-off ticket:

```text
Migration count comparison executed.
ServerInstance: <value>
Database: <value>
Execution UTC: <timestamp>
ExitCode: <value>
Artifacts:
- <path to target-counts csv>
- <path to comparison-report csv>
Outcome:
- PASS (all matched) or FAIL (mismatches attached)
```

## If Mismatches Are Found

1. Do not proceed with production sign-off.
2. Attach the comparison CSV to the escalation ticket.
3. Assign to migration/data team for reconciliation and rerun after correction.
