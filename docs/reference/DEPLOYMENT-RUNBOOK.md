# Deployment Runbook

> **Last Updated:** 2026-04-28  
> **Status:** Production-ready — final UAT passed 2026-04-26; all 30 modules validated and system cleared for go-live.

## Purpose

This document covers the remaining Step 5.2 deployment-planning tasks:

- database migration execution plan
- cutover strategy
- rollback plan

## Pre-Deployment Checklist

- Confirm production appsettings and secrets are environment-specific.
- Confirm SQL Server target instance, credentials, backup path, and restore rights.
- Confirm the application build is green and automated tests are passing.
- Confirm required storage paths, logging paths, and write permissions exist.
- Confirm email/SMS providers are configured for production-safe use.
- Confirm support contacts and on-call ownership for cutover day.

## Database Migration Execution Plan

1. Take a verified backup/snapshot of the target SQL Server instance.
2. Restore or create the target `MedyxHMS` database on the production SQL Server instance.
3. Execute the migrated schema/data script or restore the approved `.bak` artifact.
4. Run `scripts/data-migration-validation.sql` against the imported database.
5. Compare output counts with the approved source snapshot.
	- Use `scripts/compare-migration-counts.ps1` with a filled source snapshot CSV.
6. Resolve any integrity or count mismatches before enabling user traffic.
7. Update the ASPNET `DefaultConnection` to the final production database target.
8. Run application startup validation and confirm seeding completes without constraint violations.

## Cutover Strategy

### Recommended Approach

Use a controlled short freeze cutover:

1. Announce a change window.
2. Freeze writes in the legacy PHP application.
3. Take the final source export/backup.
4. Import/restore into SQL Server.
5. Run validation queries.
6. Smoke-test the ASPNET application with admin and patient login paths.
7. Enable production traffic only after the smoke checklist passes.

### Smoke Checklist

- Staff login works.
- Patient login works.
- Patient list loads.
- Appointment list loads.
- Billing list loads.
- Public booking works.
- PDF/CSV exports work.
- Audit logging and notification logging write successfully.
- Bed Management module accessible to Nurse/Admin roles.
- License module accessible to SuperAdmin only.
- Messaging, Inventory, and Download Center accessible to appropriate roles.

## Rollback Plan

Rollback triggers:

- migration validation count mismatch not understood during the window
- application startup failure against production DB
- critical login, billing, or appointment workflow failure
- widespread runtime errors during smoke testing

Rollback steps:

1. Disable ASPNET production traffic.
2. Restore the pre-cutover SQL backup if the target DB was modified.
3. Re-enable the legacy PHP application if needed.
4. Preserve logs, validation output, and failure timestamps.
5. Record the rollback cause and corrective action list before scheduling a new cutover.

## Post-Go-Live Checks

- Review application logs for startup exceptions and recurring warnings.
- Verify notification delivery logs for production sends.
- Review billing and appointment creation during the first live hour.
- Confirm support desk has escalation contacts and issue templates.

## License Deployment Quick Checklist (MedyxHMS-Lic)

Use this checklist when applying or refreshing the production license file.

1. Sign in as SuperAdmin.
2. Run MedyxHMS-Lic and generate a new RSA key pair.
3. Copy public key modulus, exponent, and verification key from MedyxHMS-Lic.
4. In ASP.NET License page, paste the upload key values and save.
5. In MedyxHMS-Lic, select term and licensed modules, then generate MedyxHMS.lic.
6. Upload MedyxHMS.lic from ASP.NET License page.
7. Verify import success, updated expiry, and entitlement matrix accuracy.
8. Export entitlement matrix CSV for deployment evidence.
9. Confirm upload key fields are cleared after successful upload (one-time key usage behavior).

Reference:

- `docs/user guides/MedyxHMS-Lic-Operator-Guide.md`
- `docs/license Guide.md`

## Windows Clone and Checkout Recovery

If clone/check-out fails with `Filename too long` on Windows, apply the following recovery steps:

1. Enable long paths in Git:
	- `git config --global core.longpaths true`
	- `git config core.longpaths true`
2. Delete recursive build output folders that were created before long-path support was enabled:
	- `cmd /c "rmdir /s /q \\?\c:\path\to\repo\bin\Debug\net8.0\temp_build_output"`
3. Rehydrate files from HEAD:
	- `git restore --source=HEAD :/`
4. Prevent recurrence by ensuring build artifacts are ignored:
	- `.gitignore` must include `bin/`, `obj/`, `temp_build_output/`, and `TestResults/`.

## Automated Count Comparison Example

For an operator-focused run checklist and required evidence bundle, see `docs/MIGRATION-COUNT-COMPARISON-CHECKLIST.md`.

Integrated security:

`pwsh ./scripts/compare-migration-counts.ps1 -ServerInstance "(localdb)\\MSSQLLocalDB" -Database "MedyxHMS" -SourceSnapshotCsv "./scripts/source-count-snapshot.template.csv"`

SQL auth:

`pwsh ./scripts/compare-migration-counts.ps1 -ServerInstance "SERVER\\INSTANCE" -Database "MedyxHMS" -SourceSnapshotCsv "./scripts/source-count-snapshot.template.csv" -UseIntegratedSecurity:$false -Username "sa" -Password "***"`