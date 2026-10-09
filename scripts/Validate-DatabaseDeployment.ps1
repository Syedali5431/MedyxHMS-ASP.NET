param(
    [switch]$Full = $false,
    [switch]$Empty = $false,
    # Also run StoredProcedures_Reports.sql, CreateIndexes.sql, MFA-Migration.sql and (with -Full) SeedDemoData.sql.
    [switch]$AllScripts = $false,
    [string]$SqlServer = ".\SQLEXPRESS",
    # Keep the temporary database for inspection instead of dropping it.
    [switch]$Keep = $false
)

# Deploys New-Database.sql (-Full) or New-Database-Empty.sql (-Empty) to a temporary database, runs it a second
# time to prove it is safe to re-run, checks the result and drops the temporary database again.
# Exit code 0 = passed, 1 = failed.

if (-not $Full -and -not $Empty) {
    Write-Error "Specify either -Full or -Empty"
    exit 1
}

$ScriptRoot = $PSScriptRoot
$Script = if ($Full) { Join-Path $ScriptRoot "New-Database.sql" } else { Join-Path $ScriptRoot "New-Database-Empty.sql" }
$DbName = "MedyxHMS_Validate_$(Get-Random)"
$TempScript = Join-Path $env:TEMP "temp_validate_$DbName.sql"
$Failures = New-Object System.Collections.Generic.List[string]

# Expected objects (117 tables / 132 foreign keys) and baseline rows of the -Full script.
$Expected = @{ Tables = 117; ForeignKeys = 132 }
$ExpectedRows = [ordered]@{
    'AspNetRoles' = 12; 'AspNetUsers' = 1; 'Roles' = 12; 'Features' = 28; 'RoleFeatures' = 122;
    'SystemModules' = 30; 'Hospitals' = 1; 'CmsPages' = 3; 'CmsMenuItems' = 7; 'CmsNotices' = 2
}

function Invoke-SqlFile {
    param([string]$File, [string]$Database)
    $output = & sqlcmd -S $SqlServer -E -b -d $Database -i $File 2>&1 | ForEach-Object { "$_" }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Get-SqlValue {
    param([string]$Query)
    $output = & sqlcmd -S $SqlServer -E -b -d $DbName -h -1 -W -Q "SET NOCOUNT ON; $Query" 2>&1
    $value = $output | Where-Object { "$_".Trim() -ne '' } | Select-Object -Last 1
    if ($null -eq $value) { return '' }
    return "$value".Trim()
}

function Test-Run {
    param([string]$Label, $Result, [switch]$ExpectNoChanges)
    $lines = @($Result.Output)
    $errors = @($lines | Where-Object { $_ -match '^Msg \d+, Level (1[1-9]|2\d)' -or $_ -match '^Sqlcmd: Error' })
    $notes = @($lines | Where-Object { $_ -match '^Note:' })
    $changes = @($lines | Where-Object { $_ -match '^(Created|Added) ' })
    Write-Host ("  {0}: exit {1}, {2} created/added, {3} notes, {4} errors" -f $Label, $Result.ExitCode, $changes.Count, $notes.Count, $errors.Count)
    if ($Result.ExitCode -ne 0 -or $errors.Count -gt 0) { $Failures.Add("$Label failed"); $lines | Select-Object -First 30 | ForEach-Object { Write-Host "    $_" } }
    foreach ($n in $notes) { $Failures.Add("$Label - $n") }
    if ($ExpectNoChanges -and $changes.Count -gt 0) { $Failures.Add("$Label changed the database again: $($changes -join '; ')") }
}

Write-Host "================================================"
Write-Host "MedyxHMS Database Validation"
Write-Host "================================================"
Write-Host "Mode: $(if ($Full) { 'FULL (schema + baseline data)' } else { 'EMPTY (schema only)' })$(if ($AllScripts) { ' + all scripts' })"
Write-Host "Server: $SqlServer"
Write-Host "Temporary database: $DbName"
Write-Host ""

try {
    if (-not (Test-Path $Script)) { Write-Error "Script not found: $Script"; exit 1 }

    # Same script, temporary database name.
    $Content = Get-Content $Script -Raw -Encoding UTF8
    $Content = $Content -replace "IF DB_ID\(N'MedyxHMS'\)", "IF DB_ID(N'$DbName')"
    $Content = $Content -replace "CREATE DATABASE \[MedyxHMS\]", "CREATE DATABASE [$DbName]"
    $Content = $Content -replace "USE \[MedyxHMS\]", "USE [$DbName]"
    [System.IO.File]::WriteAllText($TempScript, $Content, (New-Object System.Text.UTF8Encoding($true)))

    Write-Host "Step 1: Deploy"
    $start = Get-Date
    Test-Run -Label "first run" -Result (Invoke-SqlFile -File $TempScript -Database 'master')
    Write-Host ("  Duration: {0:N1}s" -f ((Get-Date) - $start).TotalSeconds)

    Write-Host "`nStep 2: Run again (must change nothing)"
    Test-Run -Label "second run" -Result (Invoke-SqlFile -File $TempScript -Database 'master') -ExpectNoChanges

    if ($AllScripts) {
        Write-Host "`nStep 3: Other scripts (each run twice)"
        $others = @('StoredProcedures_Reports.sql', 'CreateIndexes.sql', 'MFA-Migration.sql')
        if ($Full) { $others += 'SeedDemoData.sql'; $ExpectedRows['Hospitals'] = 3 }
        foreach ($name in $others) {
            foreach ($i in 1..2) { Test-Run -Label "$name run $i" -Result (Invoke-SqlFile -File (Join-Path $ScriptRoot $name) -Database $DbName) }
        }
    }

    Write-Host "`nStep 4: Checks"
    $tables = Get-SqlValue "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0"
    $fks = Get-SqlValue "SELECT COUNT(*) FROM sys.foreign_keys"
    $untrusted = Get-SqlValue "SELECT COUNT(*) FROM sys.foreign_keys WHERE is_not_trusted = 1 OR is_disabled = 1"
    $indexes = Get-SqlValue "SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE i.type > 0 AND t.is_ms_shipped = 0"
    $procs = Get-SqlValue "SELECT COUNT(*) FROM sys.procedures"
    $views = Get-SqlValue "SELECT COUNT(*) FROM sys.views"
    Write-Host "  Tables: $tables (expected $($Expected.Tables))"
    Write-Host "  Foreign keys: $fks (expected $($Expected.ForeignKeys)); not trusted or disabled: $untrusted (expected 0)"
    Write-Host "  Indexes: $indexes   Views: $views   Procedures: $procs"
    if ([int]$tables -ne $Expected.Tables) { $Failures.Add("table count $tables, expected $($Expected.Tables)") }
    if ([int]$fks -ne $Expected.ForeignKeys) { $Failures.Add("foreign key count $fks, expected $($Expected.ForeignKeys)") }
    if ([int]$untrusted -ne 0) { $Failures.Add("$untrusted foreign keys are not trusted or disabled") }
    # Discharge reports (DischargeSummaries) and the My Profile columns of AspNetUsers (October 2026).
    $newObjects = Get-SqlValue "SELECT CASE WHEN OBJECT_ID(N'dbo.DischargeSummaries', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.AspNetUsers', N'Gender') IS NOT NULL AND COL_LENGTH(N'dbo.AspNetUsers', N'DateOfBirth') IS NOT NULL AND COL_LENGTH(N'dbo.AspNetUsers', N'EmergencyContactPhone') IS NOT NULL AND COL_LENGTH(N'dbo.AspNetUsers', N'About') IS NOT NULL THEN 1 ELSE 0 END"
    Write-Host "  Discharge reports table and profile columns: $(if ($newObjects -eq '1') { 'present' } else { 'MISSING' })"
    if ($newObjects -ne '1') { $Failures.Add("DischargeSummaries table or the AspNetUsers profile columns are missing") }
    if ($AllScripts -and [int]$procs -lt 7) { $Failures.Add("report procedures missing ($procs procedures)") }

    if ($Full) {
        foreach ($t in $ExpectedRows.Keys) {
            $n = Get-SqlValue "SELECT COUNT(*) FROM [dbo].[$t]$(if ($t -eq 'AspNetUsers') { " WHERE [UserName] = N'superadmin'" })"
            Write-Host "  $t rows: $n (expected $($ExpectedRows[$t]))"
            if ([int]$n -ne $ExpectedRows[$t]) { $Failures.Add("$t has $n rows, expected $($ExpectedRows[$t])") }
        }
        $settings = Get-SqlValue "SELECT COUNT(*) FROM dbo.Settings"
        Write-Host "  Settings rows: $settings (expected 120 or more)"
        if ([int]$settings -lt 120) { $Failures.Add("Settings has $settings rows, expected 120 or more") }
        if ($AllScripts) {
            # Demo data: tester + 22 hospital accounts, their hospital access, inventory and PKR.
            $demo = [ordered]@{
                'Demo accounts (tester, hospital accounts)' = @("SELECT COUNT(*) FROM AspNetUsers WHERE UserName = N'tester' OR Email LIKE N'%@medyxdemo.pk'", 23)
                'Hospital access rows' = @("SELECT COUNT(*) FROM UserHospitalAccesses", 26)
                'Inventory items' = @("SELECT COUNT(*) FROM InventoryItems", 36)
                'Purchase bills' = @("SELECT COUNT(*) FROM PurchaseBills", 6)
                'Currency PKR' = @("SELECT COUNT(*) FROM Settings WHERE [Key] = N'Printing:CurrencySymbol' AND [Value] = N'PKR'", 1)
                'Unicode text intact (scripts read as UTF-8)' = @("SELECT COUNT(*) FROM Wards WHERE [Id] = 101 AND [Name] = N'General Ward ' + NCHAR(8211) + N' Islamabad'", 1)
            }
            foreach ($k in $demo.Keys) {
                $n = Get-SqlValue $demo[$k][0]
                Write-Host "  $($k): $n (expected $($demo[$k][1]))"
                if ([int]$n -ne $demo[$k][1]) { $Failures.Add("$k is $n, expected $($demo[$k][1])") }
            }
        }
        $pwd = Get-SqlValue "SELECT CASE WHEN LEN(PasswordHash) > 20 THEN 1 ELSE 0 END FROM AspNetUsers WHERE UserName = N'superadmin'"
        Write-Host "  SuperAdmin has a password: $(if ($pwd -eq '1') { 'yes' } else { 'NO' })"
        if ($pwd -ne '1') { $Failures.Add("SuperAdmin has no password") }
    }
    else {
        $rows = Get-SqlValue "SELECT COUNT(*) FROM dbo.AspNetRoles"
        Write-Host "  Data rows in AspNetRoles: $rows (expected 0 - the application seeds at start-up)"
        if ([int]$rows -ne 0) { $Failures.Add("schema-only script inserted data") }
    }

    if ($Failures.Count -eq 0) {
        Write-Host "`n[OK] Validation PASSED" -ForegroundColor Green
    }
    else {
        Write-Host "`n[FAILED] Validation FAILED:" -ForegroundColor Red
        $Failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    }
}
catch {
    Write-Host "[ERROR] $_" -ForegroundColor Red
    $Failures.Add("$_")
}
finally {
    if ($Keep) {
        Write-Host "`nTemporary database kept: $DbName"
    }
    else {
        Write-Host "`nCleaning up..."
        & sqlcmd -S $SqlServer -E -b -d master -Q "IF DB_ID(N'$DbName') IS NOT NULL BEGIN ALTER DATABASE [$DbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DbName]; END" 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) { Write-Host "[OK] Temporary database dropped" -ForegroundColor Green }
        else { Write-Host "[WARNING] Could not drop temporary database: $DbName" -ForegroundColor Yellow }
    }
    if (Test-Path $TempScript) { Remove-Item $TempScript -Force -ErrorAction SilentlyContinue }
    Write-Host "================================================"
}

if ($Failures.Count -gt 0) { exit 1 }
exit 0
