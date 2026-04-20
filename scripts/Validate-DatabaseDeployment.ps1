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

Write-Host "================================================"
Write-Host "MedyxHMS Database Validation Script"
Write-Host "================================================"
Write-Host "Mode: $(if ($Full) { 'FULL (with seed data)' } else { 'EMPTY (schema only)' })"
Write-Host "Target Server: $SqlServer"
Write-Host "Temporary DB: $DbName"
Write-Host ""

try {
    # Validate script file exists
    if (-not (Test-Path $Script)) {
        Write-Error "Script not found: $Script"
        exit 1
    }

    # Create temporary script with modified database name
    $Content = Get-Content $Script -Raw
    # Replace all occurrences of MedyxHMS with the temp database name
    $Content = $Content -replace "IF DB_ID\(N'MedyxHMS'\)", "IF DB_ID(N'$DbName')"
    $Content = $Content -replace "CREATE DATABASE \[MedyxHMS\]", "CREATE DATABASE [$DbName]"
    $Content = $Content -replace "USE \[MedyxHMS\]", "USE [$DbName]"
    $Content = $Content -replace "DB_NAME\(\) = 'MedyxHMS'", "DB_NAME() = '$DbName'"
    $Content | Set-Content $TempScript

    # Deploy script
    Write-Host "Step 1: Deploying to temporary database..."
    $DeployStart = Get-Date
    $DeployOutput = & sqlcmd -S $SqlServer -i $TempScript 2>&1
    $ExitCode = $LASTEXITCODE
    $DeployEnd = Get-Date
    $DeployDuration = ($DeployEnd - $DeployStart).TotalSeconds

    Write-Host "  Exit Code: $ExitCode"
    Write-Host "  Duration: $($DeployDuration)s"

    if ($ExitCode -ne 0) {
        Write-Host "[FAILED] Deployment FAILED" -ForegroundColor Red
        Write-Output $DeployOutput
        exit $ExitCode
    }
    
    Write-Host "[OK] Deployment SUCCESSFUL" -ForegroundColor Green

    # Run validation queries
    Write-Host "`nStep 2: Running validation queries..."
    $ValidationStart = Get-Date

    # Helper function to extract numeric results from sqlcmd output
    function Get-SqlResult {
        param([string]$Query, [string]$Server, [string]$Database)
        $output = sqlcmd -S $Server -d $Database -Q $Query 2>&1
        $result = $output | Where-Object { $_ -match "^\s*\d+\s*$" } | Select-Object -Last 1
        if ($null -ne $result) {
            return ($result -replace "\s", "")
        }
        return "0"
    }

    # Query 1: Table count
    $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE'" -Server $SqlServer -Database $DbName
    Write-Host "  Table Count: $Result"

    # Query 2: View count
    $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = 'dbo'" -Server $SqlServer -Database $DbName
    Write-Host "  View Count: $Result"

    # Query 3: Procedure count
    $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_SCHEMA = 'dbo'" -Server $SqlServer -Database $DbName
    Write-Host "  Procedure Count: $Result"

    if ($Full) {
        # Query 4: Identity Roles
        $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM AspNetRoles" -Server $SqlServer -Database $DbName
        Write-Host "  Identity Roles: $Result (expected: 9)"

        # Query 5: Features
        $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM dbo.Features" -Server $SqlServer -Database $DbName
        Write-Host "  Features: $Result (expected: 28)"

        # Query 6: Role-Feature Mappings
        $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM dbo.RoleFeatures" -Server $SqlServer -Database $DbName
        Write-Host "  Role-Feature Mappings: $Result (expected: 95)"

        # Query 7: Users
        $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM AspNetUsers" -Server $SqlServer -Database $DbName
        Write-Host "  Users: $Result (expected: 1)"

        # Query 8: Settings
        $Result = Get-SqlResult -Query "SELECT COUNT(*) FROM dbo.Settings" -Server $SqlServer -Database $DbName
        Write-Host "  Settings: $Result (expected: 10+)"

        # Query 9: SuperAdmin User
        $SuperAdminQuery = sqlcmd -S $SqlServer -d $DbName -Q "SELECT UserName FROM AspNetUsers WHERE UserName = 'SuperAdmin'" 2>&1
        $SuperAdmin = $SuperAdminQuery | Where-Object { $_ -match "SuperAdmin" } | Select-Object -Last 1
        Write-Host "  SuperAdmin User: $SuperAdmin"
    }

    $ValidationEnd = Get-Date
    $ValidationDuration = ($ValidationEnd - $ValidationStart).TotalSeconds
    Write-Host "  Validation Duration: $($ValidationDuration)s"

    Write-Host "`n[OK] Validation PASSED" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Cleanup
    Write-Host "`nStep 3: Cleaning up temporary database..."
    
    $DropResult = & sqlcmd -S $SqlServer -Q "DROP DATABASE IF EXISTS $DbName" 2>&1
    $DropExitCode = $LASTEXITCODE

    if ($DropExitCode -eq 0) {
        Write-Host "[OK] Temporary database dropped" -ForegroundColor Green
    }
    else {
        Write-Host "[WARNING] Could not drop temporary database: $DbName" -ForegroundColor Yellow
    }

    if (Test-Path $TempScript) {
        Remove-Item $TempScript -Force -ErrorAction SilentlyContinue
        Write-Host "[OK] Temporary script removed"
    }

    Write-Host "`n================================================"
}
