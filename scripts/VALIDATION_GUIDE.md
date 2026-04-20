# Validate MedyxHMS Database Deployment

## Usage
```powershell
# Test full script deployment with validation
.\scripts\Validate-DatabaseDeployment.ps1 -Full

# Test empty script deployment with validation
.\scripts\Validate-DatabaseDeployment.ps1 -Empty

# Custom SQL Server instance
.\scripts\Validate-DatabaseDeployment.ps1 -Full -SqlServer "CUSTOM-SQL\INSTANCE"
```

---

## Validation SQL Queries

The following queries are executed post-deployment to verify proper schema creation and data seeding:

### Schema Validation
```sql
-- Table count verification
SELECT COUNT(*) AS TableCount 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE';
-- Expected: 50+ tables

-- View verification
SELECT COUNT(*) AS ViewCount 
FROM INFORMATION_SCHEMA.VIEWS 
WHERE TABLE_SCHEMA = 'dbo';
-- Expected: 2+ views (vw_StaffWithRoles, vw_ActiveDoctorShifts)

-- Stored procedure verification
SELECT COUNT(*) AS ProcCount 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_SCHEMA = 'dbo';
-- Expected: 2+ procedures (usp_GetOperationalCounts, usp_GetUserRoleSummary)
```

### Seed Data Validation (Full Script Only)
```sql
-- Identity Roles
SELECT COUNT(*) AS RoleCount FROM AspNetRoles;
-- Expected: 9 roles

-- Features/Modules
SELECT COUNT(*) AS FeatureCount FROM dbo.Features;
-- Expected: 28 features

-- Role-Feature Mappings
SELECT COUNT(*) AS MappingCount FROM dbo.RoleFeatures;
-- Expected: 95 mappings

-- Users
SELECT COUNT(*) AS UserCount FROM AspNetUsers;
-- Expected: 1 (SuperAdmin)

-- SuperAdmin Verification
SELECT TOP 1 UserName, Email, PhoneNumber 
FROM AspNetUsers 
WHERE UserName = 'SuperAdmin';

-- Settings
SELECT COUNT(*) AS SettingCount FROM dbo.Settings;
-- Expected: 10+ settings

-- Utility Queries
EXEC usp_GetOperationalCounts;
EXEC usp_GetUserRoleSummary;
```

### Empty Script Validation
```sql
-- Schema should be identical
SELECT COUNT(*) AS TableCount 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE';
-- Expected: 50+ tables (same as Full script)

-- Seed data should be absent
SELECT COUNT(*) FROM AspNetRoles;
-- Expected: 0 (or only system defaults)

SELECT COUNT(*) FROM dbo.Features;
-- Expected: 0

SELECT COUNT(*) FROM AspNetUsers;
-- Expected: 0
```

---

## Quick Validation Commands

Run these after deployment to quickly verify success:

```powershell
# Test connection
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@SERVERNAME, GETDATE()"

# Count tables
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MedyxHMS -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE'"

# Verify roles (full script only)
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MedyxHMS -Q "SELECT COUNT(*) FROM AspNetRoles"

# Verify seed data seeded correctly (full script only)
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MedyxHMS -Q "SELECT * FROM AspNetUsers WHERE UserName = 'SuperAdmin'"

# Test utility view
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MedyxHMS -Q "SELECT TOP 5 * FROM vw_StaffWithRoles"
```

---

## Automated Validation Script

Save the following as `Validate-DatabaseDeployment.ps1`:

```powershell
param(
    [switch]$Full = $false,
    [switch]$Empty = $false,
    [string]$SqlServer = "(localdb)\MSSQLLocalDB"
)

if (-not $Full -and -not $Empty) {
    Write-Error "Specify either -Full or -Empty"
    exit 1
}

$Script = if ($Full) { "scripts\New-Database.sql" } else { "scripts\New-Database-Empty.sql" }
$DbName = "MedyxHMS_Validate_$(Get-Random)"
$TempScript = "temp_validate_$DbName.sql"

try {
    # Replace database name in temp script
    $Content = Get-Content $Script
    $Content -replace "CREATE DATABASE MedyxHMS", "CREATE DATABASE $DbName" | Set-Content $TempScript

    # Deploy script
    Write-Host "Deploying to temporary database: $DbName"
    $DeployResult = sqlcmd -S $SqlServer -i $TempScript 2>&1
    $ExitCode = $LASTEXITCODE

    if ($ExitCode -eq 0) {
        Write-Host "✓ Deployment successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Deployment failed with exit code $ExitCode" -ForegroundColor Red
        Write-Output $DeployResult
        exit $ExitCode
    }

    # Run validation queries
    Write-Host "`nRunning validation queries..."

    $Queries = @(
        ("Table Count", "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE'"),
        ("View Count", "SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = 'dbo'"),
        ("Procedure Count", "SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_SCHEMA = 'dbo'")
    )

    if ($Full) {
        $Queries += @(
            ("Identity Roles", "SELECT COUNT(*) FROM AspNetRoles"),
            ("Features", "SELECT COUNT(*) FROM dbo.Features"),
            ("Role Mappings", "SELECT COUNT(*) FROM dbo.RoleFeatures"),
            ("Users", "SELECT COUNT(*) FROM AspNetUsers"),
            ("Settings", "SELECT COUNT(*) FROM dbo.Settings")
        )
    }

    foreach ($Query in $Queries) {
        $Name, $Sql = $Query
        $Result = sqlcmd -S $SqlServer -d $DbName -Q $Sql 2>&1 | Select-Object -Last 1
        Write-Host "  $Name : $Result"
    }

    Write-Host "`n✓ Validation complete"
}
finally {
    # Cleanup
    Write-Host "`nCleaning up temporary database..."
    sqlcmd -S $SqlServer -Q "DROP DATABASE IF EXISTS $DbName" 2>&1 | Out-Null
    Remove-Item $TempScript -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Cleanup complete"
}
```

---

## Expected Validation Results

### Full Script Deployment
```
Table Count       : 50+
View Count        : 2+
Procedure Count   : 2+
Identity Roles    : 9
Features          : 28
Role Mappings     : 95
Users             : 1
Settings          : 10+
✓ Validation complete
```

### Empty Script Deployment
```
Table Count       : 50+
View Count        : 2+
Procedure Count   : 2+
✓ Validation complete
```

---

## References
- [Database Bootstrap Scripts README](README.md)
- [Deployment Checklist](DEPLOYMENT_CHECKLIST.md)
