# Ops Migration Run Request

Use the message below to request final Stage 5.2 migration validation from operations.

```text
Subject: MedyxHMS Stage 5.2 final migration count validation request

Please execute the migration count comparison for the target SQL Server dataset using the checklist and script below:

- Checklist: docs/MIGRATION-COUNT-COMPARISON-CHECKLIST.md
- Script: scripts/compare-migration-counts.ps1

Inputs required from ops:
- target SQL Server instance
- target database name
- approved source snapshot CSV with real source counts
- output folder for evidence artifacts
- SQL credentials if integrated security is not being used

Required evidence to return:
- target-counts CSV
- comparison-report CSV
- execution timestamp (UTC)
- server instance and database name used
- script exit code

Pass criteria:
- exit code 0
- no mismatches in comparison report

If mismatches are found:
- do not mark migration sign-off complete
- attach the comparison report to the escalation ticket
- return the failure details for reconciliation

This is the final environment-dependent blocker for Stage 5.2 closure.
```