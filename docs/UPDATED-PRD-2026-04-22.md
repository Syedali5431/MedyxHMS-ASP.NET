# Updated PRD (2026-04-22)

## Product
MedyxHMS ASP.NET Core platform.

## Objective
Define the post-stabilization product requirements baseline for deployment readiness, governance, and operational continuity.

## Core Requirements
- Reliable authentication and role-based access control.
- Complete report generation, listing, edit, and schedule workflows.
- Stable patient portal registration and access flow.
- Admin/SuperAdmin governance over licensing, approvals, and module controls.
- Runtime-safe startup bootstrap and script deployment behavior.

## Operational Requirements
- Build must complete without compile errors.
- Automated smoke checks must validate public routes and protected-route redirects.
- Critical test suites must pass consistently.
- UAT evidence must be documented and traceable.

## Security and Governance Requirements
- Unique identity username policies must remain enforced.
- Role boundaries must be enforced for privileged operations.
- License restrictions and entitlement checks must remain server-side.

## Current Baseline (2026-04-22)
- Compile/test gates passing.
- Runtime smoke checks passing.
- Stored procedure and report index initialization path stabilized.
- Full business UAT still pending role-by-role manual execution with seeded data.

## Linked Documents
- UAT execution evidence: UAT-EXECUTION-EVIDENCE-2026-04-22.md
- Role validation checklist: UAT-ROLE-VALIDATION-CHECKLIST-2026-04-22.md
- Warning triage plan: NULLABILITY-WARNING-TRIAGE-PLAN-2026-04-22.md
- Final run status: IMPLEMENTATION-STATUS-2026-04-22-FINAL-RUN.md
