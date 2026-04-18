param(
    [Parameter(Mandatory = $true)]
    [string]$ServerInstance,

    [Parameter(Mandatory = $false)]
    [string]$Database = "MedyxHMS",

    [Parameter(Mandatory = $true)]
    [string]$SourceSnapshotCsv,

    [Parameter(Mandatory = $false)]
    [string]$OutputFolder = ".\\docs",

    [switch]$UseIntegratedSecurity = $true,

    [string]$Username,
    [string]$Password
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourceSnapshotCsv)) {
    throw "Source snapshot file not found: $SourceSnapshotCsv"
}

if (-not (Test-Path $OutputFolder)) {
    New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$targetCsv = Join-Path $OutputFolder "target-counts-$timestamp.csv"
$comparisonCsv = Join-Path $OutputFolder "count-comparison-$timestamp.csv"

$query = @"
SET NOCOUNT ON;
SELECT 'Patients' AS [CheckName], COUNT(*) AS [TargetCount] FROM Patients
UNION ALL SELECT 'Doctors', COUNT(*) FROM Doctors
UNION ALL SELECT 'Appointments', COUNT(*) FROM Appointments
UNION ALL SELECT 'Bills', COUNT(*) FROM Bills
UNION ALL SELECT 'BillItems', COUNT(*) FROM BillItems
UNION ALL SELECT 'Payments', COUNT(*) FROM Payments
UNION ALL SELECT 'PublicAppointmentRequests', COUNT(*) FROM PublicAppointmentRequests
UNION ALL SELECT 'Staff', COUNT(*) FROM Staff;
"@

$queryFile = [System.IO.Path]::GetTempFileName()
Set-Content -Path $queryFile -Value $query -Encoding UTF8

try {
    if ($UseIntegratedSecurity.IsPresent) {
        & sqlcmd -S $ServerInstance -d $Database -E -W -s "," -h -1 -i $queryFile -o $targetCsv
    }
    else {
        if ([string]::IsNullOrWhiteSpace($Username) -or [string]::IsNullOrWhiteSpace($Password)) {
            throw "Username and Password are required when not using integrated security."
        }

        & sqlcmd -S $ServerInstance -d $Database -U $Username -P $Password -W -s "," -h -1 -i $queryFile -o $targetCsv
    }
}
finally {
    Remove-Item -Path $queryFile -Force -ErrorAction SilentlyContinue
}

$sourceRows = Import-Csv -Path $SourceSnapshotCsv
$targetRows = Import-Csv -Path $targetCsv -Header "CheckName", "TargetCount"

$sourceMap = @{}
foreach ($row in $sourceRows) {
    $sourceMap[$row.CheckName.Trim()] = [int64]$row.SourceCount
}

$targetMap = @{}
foreach ($row in $targetRows) {
    if ([string]::IsNullOrWhiteSpace($row.CheckName)) {
        continue
    }

    $targetMap[$row.CheckName.Trim()] = [int64]$row.TargetCount
}

$allKeys = @($sourceMap.Keys + $targetMap.Keys | Sort-Object -Unique)
$comparison = foreach ($key in $allKeys) {
    $sourceCount = if ($sourceMap.ContainsKey($key)) { $sourceMap[$key] } else { $null }
    $targetCountValue = if ($targetMap.ContainsKey($key)) { $targetMap[$key] } else { $null }
    $delta = if ($null -ne $sourceCount -and $null -ne $targetCountValue) { $targetCountValue - $sourceCount } else { $null }

    [PSCustomObject]@{
        CheckName = $key
        SourceCount = $sourceCount
        TargetCount = $targetCountValue
        Delta = $delta
        Match = if ($null -eq $delta) { "N/A" } elseif ($delta -eq 0) { "Yes" } else { "No" }
    }
}

$comparison | Export-Csv -Path $comparisonCsv -NoTypeInformation

Write-Host "Target counts saved to: $targetCsv"
Write-Host "Comparison saved to: $comparisonCsv"
Write-Host ""
Write-Host "Comparison summary:"
$comparison | Format-Table -AutoSize

$mismatches = $comparison | Where-Object { $_.Match -eq "No" }
if ($mismatches.Count -gt 0) {
    Write-Warning "Count mismatches detected. Review the comparison CSV."
    exit 2
}

Write-Host "All comparable counts match source snapshot." -ForegroundColor Green
exit 0
