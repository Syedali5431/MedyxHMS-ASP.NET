param(
    [string]$SqlInstance = ".",
    [string]$DatabaseName = "MedyxHMS",
    [string]$SourceSqlPath = "C:\Databases\Medyx-HMS\hospitaldemo_db.sql",
    [string]$WorkingSqlPath = "",
    [switch]$SkipNormalization
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Tool {
    param([string]$ToolName)

    $tool = Get-Command $ToolName -ErrorAction SilentlyContinue
    if (-not $tool) {
        throw "Required tool '$ToolName' was not found in PATH. Install SQL Server command-line tools and retry."
    }
}

function Convert-SqlDump {
    param(
        [string]$InputPath,
        [string]$OutputPath
    )

    $raw = Get-Content -Path $InputPath -Raw

    # Remove common MySQL directives/comments that SQL Server cannot parse.
    $normalized = $raw -replace '/\*!.*?\*/', ''
    $normalized = $normalized -replace '(?im)^\s*SET\s+SQL_MODE.*?;\s*$', ''
    $normalized = $normalized -replace '(?im)^\s*SET\s+time_zone.*?;\s*$', ''
    $normalized = $normalized -replace '(?im)^\s*START\s+TRANSACTION\s*;\s*$', ''
    $normalized = $normalized -replace '(?im)^\s*COMMIT\s*;\s*$', ''
    $normalized = $normalized -replace '(?im)^\s*LOCK\s+TABLES.*?;\s*$', ''
    $normalized = $normalized -replace '(?im)^\s*UNLOCK\s+TABLES\s*;\s*$', ''

    # Basic identifier and type normalization.
    $normalized = $normalized -replace '`([^`]+)`', '[$1]'
    $normalized = $normalized -replace '(?i)\bint\(\d+\)', 'INT'
    $normalized = $normalized -replace '(?i)\bbigint\(\d+\)', 'BIGINT'
    $normalized = $normalized -replace '(?i)\bsmallint\(\d+\)', 'SMALLINT'
    $normalized = $normalized -replace '(?i)\btinyint\(\d+\)', 'TINYINT'
    $normalized = $normalized -replace '(?i)\blongtext\b', 'NVARCHAR(MAX)'
    $normalized = $normalized -replace '(?i)\bmediumtext\b', 'NVARCHAR(MAX)'
    $normalized = $normalized -replace '(?i)\btext\b', 'NVARCHAR(MAX)'
    $normalized = $normalized -replace '(?i)\bdatetime\b', 'DATETIME2'
    $normalized = $normalized -replace '(?i)\btimestamp\b', 'DATETIME2'
    $normalized = $normalized -replace '(?i)\bunsigned\b', ''
    $normalized = $normalized -replace '(?i)AUTO_INCREMENT', 'IDENTITY(1,1)'

    # Remove MySQL table options like ENGINE/CHARSET from CREATE TABLE tails.
    $normalized = $normalized -replace '(?im)\)\s*ENGINE\s*=.*?;', ');'

    if ($normalized -notmatch '(?im)^\s*USE\s+\[') {
        $normalized = "USE [$DatabaseName];`r`nGO`r`n`r`n" + $normalized
    }

    Set-Content -Path $OutputPath -Value $normalized -Encoding UTF8
}

if (-not (Test-Path -Path $SourceSqlPath -PathType Leaf)) {
    throw "Source SQL file not found: $SourceSqlPath"
}

Assert-Tool -ToolName "sqlcmd"

if ([string]::IsNullOrWhiteSpace($WorkingSqlPath)) {
    $workingDir = Join-Path -Path $PSScriptRoot -ChildPath "temp"
    if (-not (Test-Path -Path $workingDir)) {
        New-Item -Path $workingDir -ItemType Directory | Out-Null
    }

    $WorkingSqlPath = Join-Path -Path $workingDir -ChildPath "hospitaldemo_db.sqlserver.ready.sql"
}

if ($SkipNormalization) {
    Copy-Item -Path $SourceSqlPath -Destination $WorkingSqlPath -Force
}
else {
    Convert-SqlDump -InputPath $SourceSqlPath -OutputPath $WorkingSqlPath
}

Write-Host "Importing demo data into [$DatabaseName] on [$SqlInstance] ..."
sqlcmd -S $SqlInstance -d $DatabaseName -b -i $WorkingSqlPath

Write-Host "Demo data import completed."
Write-Host "Working SQL file: $WorkingSqlPath"
