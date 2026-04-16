# Phase 4 - Route, Authorization, and Login Flow Runtime Fixes

## Objective
Fix functional runtime failures surfaced by HTTP smoke checks after startup became stable.

## What Was Done
- Resolved ambiguous endpoint routing:
  - Differentiated PatientPortal account routes to avoid Account/Login action collisions.
- Implemented dynamic permission policy resolution:
  - Added policy provider support for Permission:* patterns used by custom authorization attributes.
- Fixed PatientPortal login runtime model mismatch:
  - Corrected action/view-model flow so the rendered login view receives a compatible model.
- Re-ran smoke checks and verified behavior:
  - Public endpoints returned 200.
  - Protected module routes returned expected auth redirect responses.

## How It Was Done
1. Executed targeted HTTP route probes on key endpoints.
2. Correlated 500 responses with live server exception logs.
3. Patched routing/authorization/view-model mismatch root causes.
4. Repeated smoke checks to confirm status changes from failure to expected behavior.

## Outcome
- Core auth entry points stabilized.
- Unauthenticated access flow validated through correct 302 redirects.

## Notes for Future Work
- Add endpoint smoke tests for login and protected routes in automated test pipeline.
- Keep portal route templates explicit to avoid controller/action name collisions.
