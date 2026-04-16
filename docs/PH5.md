# Phase 5 - EF Warning Hardening and Startup Log Cleanup

## Objective
Eliminate high-noise, non-fatal runtime warnings to improve maintainability and reduce hidden data-risk scenarios.

## What Was Done
- Eliminated EF shadow FK warnings for OPD/IPD patient relationships:
  - Explicitly mapped inverse collections:
    - OPDVisit.Patient <-> Patient.OPDVisits
    - IPDAdmission.Patient <-> Patient.IPDAdmissions
- Eliminated SQL decimal precision warnings:
  - Added explicit HasPrecision(18,2) mappings for all affected decimal properties in ApplicationDbContext.
- Removed development HTTPS redirection warning:
  - Restricted UseHttpsRedirection middleware to non-development environment configuration.

## How It Was Done
1. Parsed startup warning stream and isolated warning classes by source.
2. Centralized precision configuration in DbContext for consistency and future extensibility.
3. Updated middleware ordering/environment guard in Program startup pipeline.
4. Rebuilt and reran smoke checks to verify no functional regressions.

## Verification Snapshot
- Build status: Success.
- Public endpoints: 200 responses.
- Protected endpoints: 302 redirects to login when unauthenticated.
- Startup warnings addressed in this phase: resolved.

## Notes for Future Work
- Consider enforcing decimal precision conventions through model configuration tests.
- Keep environment-specific middleware decisions documented with run profiles.
