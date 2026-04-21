# Nullability Warning Triage Plan (2026-04-22)

## Baseline
- Clean rebuild result: build succeeded with 1099 warnings.
- Dominant warning classes observed:
  - CS8618 (non-nullable property not initialized)
  - CS8601/CS8603 (possible null assignment/return)
  - CS8602 (possible null dereference)
  - CS8625 (null literal to non-nullable reference)
  - CS8634/CS8621 (generic nullability mismatch)

## Priority Strategy
1. P1 runtime-risk warnings first:
   - CS8602 in controllers/services on active request paths.
2. P2 data-contract safety:
   - CS8601/CS8603 in service return and mapping boundaries.
3. P3 model/viewmodel initialization hygiene:
   - CS8618 across DTOs/ViewModels.
4. P4 generic/nullability constraint cleanup:
   - CS8634/CS8621 in cache and helper abstractions.

## Recommended Batch Order
1. Controllers on high-traffic modules:
   - Report, OPD, IPD, CMS, Radiology, Site.
2. Core services:
   - Appointment, Billing, Staff, Leave, Notification providers.
3. ViewModels and DTOs:
   - Account and Appointment ViewModels first.
4. Infrastructure generics:
   - ReportTemplateService and cache interaction signatures.

## Fix Patterns
- Replace risky dereferences with guard clauses and early returns.
- Align method signatures to nullable reality (use nullable return types where appropriate).
- Initialize required non-nullable properties using:
  - constructor defaults,
  - required members,
  - nullable annotations where semantically correct.
- Avoid blanket null-forgiving operator usage unless invariants are proven.

## Safety Rules
- Do not change public behavior while reducing warnings.
- Add focused tests when altering control flow around null checks.
- Keep each PR warning-reduction scoped by module.

## Suggested Milestones
1. Milestone A: reduce runtime-risk warnings in controllers/services by at least 40 percent.
2. Milestone B: reduce CS8618 in ViewModels/DTOs by at least 60 percent.
3. Milestone C: resolve remaining generic nullability mismatches and stabilize under 200 warnings.

## Validation per Milestone
1. dotnet build MedyxHMS.csproj -v minimal
2. dotnet test tests/MedyxHMS.MobileApi.Tests/MedyxHMS.MobileApi.Tests.csproj -v minimal
3. dotnet test tests/MedyxHMS.Chatbot.Security.Tests/MedyxHMS.Chatbot.Security.Tests.csproj -v minimal
4. Re-run route smoke checks for public/protected endpoints.

## Owner Checklist
- Assign module owners and target warning buckets.
- Track warning count delta per PR.
- Reject mixed feature + warning cleanup PRs unless explicitly approved.
