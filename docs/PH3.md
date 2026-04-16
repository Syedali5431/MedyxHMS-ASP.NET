# Phase 3 - Runtime Startup Blocker Resolution (EF Core and Seeding)

## Objective
Resolve startup-time crashes discovered during smoke testing so the web host can initialize and serve requests.

## What Was Done
- Fixed entity foreign key type mismatch:
  - MedicalRecord.PatientId and TestResult.PatientId were corrected to match Patient.Id type.
- Fixed SQL Server cascade-path creation failure:
  - Configured delete behavior for MedicalRecord and TestResult patient relations to Restrict.
- Fixed required seed data violations:
  - Added required ApplicationUser seed properties (FirstName, LastName) in model seeding.
- Fixed StaffRole seeding persistence failure:
  - Removed conflicting navigation mapping that generated shadow StaffId1 and caused null insert failures.
  - Aligned StaffRole navigation to the Staff aggregate relationship.

## How It Was Done
1. Reproduced each startup exception using dotnet run.
2. Extracted exact exception signature and failing model/database action.
3. Applied minimal model/context mapping fix for each blocker.
4. Rebuilt and reran after every fix until startup advanced to the next blocker.

## Outcome
- Startup moved from immediate EF model/seeding failures to active route-level runtime testing.

## Notes for Future Work
- Add integration startup tests that execute database initialization in CI.
- Ensure required identity fields in seed objects are validated by tests before deploy.
