# UAT Smoke Checklist (2026-04-22)

## Environment
- Application URL: http://localhost:5044
- Database: MedyxHMS (SQL Server)
- Validation mode: Authenticated browser smoke pass + HTTP status verification

## Run status
- App process: Running (`dotnet` active)
- Root URL check: `200 OK`

## Module checklist
| Module/Route | Result | Notes |
|---|---|---|
| `/` | PASS | Home page loads |
| `/Dashboard` | PASS | Dashboard loads |
| `/Patient` | PASS | Patient management loads |
| `/Appointment` | PASS | Appointment listing loads |
| `/OPD` | PASS | OPD page loads |
| `/IPD` | PASS | IPD page loads |
| `/Billing` | PASS | Billing page loads |
| `/Lab` | PASS | Lab page loads |
| `/Radiology` | PASS | Radiology page loads |
| `/BloodBank` | PASS | Blood bank page loads |
| `/OperationTheatre` | PASS | OT page loads |
| `/Referral` | PASS | Referral/TPA page loads |
| `/Prescription` | PASS | Prescription page loads |
| `/FrontOffice` | PASS | Front office page loads |
| `/Attendance` | PASS | Attendance page loads |
| `/Leave` | PASS | Leave management page loads |
| `/Payroll` | PASS | Payroll page loads |
| `/Report` | PASS | Reports page loads |
| `/Notifications` | PASS | Notifications page loads |
| `/License` | PASS | License page loads |
| `/Chatbot` | PASS | Redirects to consent screen (`/Chatbot/RequestConsent`) |

## Commands to run the app
From `c:\Databases\Medyx-HMS\MedyxHMS-ASPNET`:

```powershell
dotnet run --project .\MedyxHMS.csproj --urls "http://localhost:5044"
```

## Quick health check
```powershell
Invoke-WebRequest -Uri "http://localhost:5044/" -UseBasicParsing | Select-Object StatusCode
```
Expected: `200`

## Phase outcome
- Smoke validation complete.
- Application is currently available and running on `http://localhost:5044`.
