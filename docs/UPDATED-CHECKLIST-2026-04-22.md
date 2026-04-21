# Updated Checklist (2026-04-22)

## Technical Gate Checklist
- [x] Build succeeds on current baseline.
- [x] Mobile API tests pass.
- [x] Chatbot security tests pass.
- [x] Public route smoke checks return expected 200 codes.
- [x] Protected route smoke checks return expected 302 redirects.
- [x] Startup report SQL deployment path completes without blocking errors.

## Governance Checklist
- [x] Report edit flow restricted to Admin and SuperAdmin.
- [x] Patient portal register route resolves without runtime model mismatch.
- [x] License handling remains server-side with role enforcement.

## Pending Business UAT Checklist
- [ ] Execute role-by-role authenticated UAT across all primary modules.
- [ ] Validate dashboard, chart, and table data against seeded reference data.
- [ ] Validate billing/front-office end-to-end transactional paths.
- [ ] Capture screenshots and role evidence for signoff.

## Follow-up Quality Checklist
- [ ] Reduce high-risk nullability warnings in controllers/services.
- [ ] Re-run full smoke and tests after each warning-reduction batch.
