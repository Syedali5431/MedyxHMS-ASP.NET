param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'UAT-Smoke.config.template.json'),
    [string]$BaseUrl,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipLicenseGeneration,
    [string]$ResultsPath = (Join-Path $PSScriptRoot '..\temp_build_output\uat-smoke-results.json')
)

$ErrorActionPreference = 'Stop'

$config = @{}
if (Test-Path $ConfigPath) {
    $config = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json -AsHashtable
}

if (-not $BaseUrl -and $config.ContainsKey('BaseUrl')) {
    $BaseUrl = [string]$config['BaseUrl']
}

$root = Join-Path $PSScriptRoot '..'
$webProject = Join-Path $root 'MedyxHMS.csproj'
$licenseProject = Join-Path $root 'MedyxHMS-Lic\MedyxHMS-Lic.csproj'
$results = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Build = @{}
    Tests = @{}
    LicenseTool = @{}
    HttpChecks = @{}
}

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    try {
        $value = & $Action
        return @{ Success = $true; Value = $value }
    }
    catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

if (-not $SkipBuild) {
    $results.Build.Web = Invoke-Step -Name 'BuildWeb' -Action {
        dotnet build $webProject -v minimal | Out-String
    }
    $results.Build.LicenseTool = Invoke-Step -Name 'BuildLicenseTool' -Action {
        dotnet build $licenseProject -v minimal | Out-String
    }
}

if (-not $SkipTests) {
    $results.Tests.UnitAndIntegration = Invoke-Step -Name 'RunTests' -Action {
        dotnet test (Join-Path $root 'tests\MedyxHMS.Tests\MedyxHMS.Tests.csproj') -v minimal | Out-String
    }
}

if (-not $SkipLicenseGeneration) {
    $licenseOutput = Join-Path $root 'temp_build_output\license-smoke'
    $keyRun = Invoke-Step -Name 'GenerateKey' -Action {
        & (Join-Path $PSScriptRoot 'Invoke-LicenseToolAutomation.ps1') -Mode GenerateKey -OutputDirectory $licenseOutput
    }
    $results.LicenseTool.GenerateKey = $keyRun

    if ($keyRun.Success) {
        $privateKey = Get-ChildItem -Path $licenseOutput -Filter 'medyxhms-private-key-*.json' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($null -ne $privateKey) {
            $modules = @()
            if ($config.ContainsKey('Modules')) {
                $modules = [string[]]$config['Modules']
            }
            if ($modules.Count -eq 0) {
                $modules = @('Dashboard','Patient','Appointment','OPD','IPD','Billing','Prescription','Lab','Radiology','BloodBank','OperationTheatre','FrontOffice','Attendance','Leave','Payroll','Certificate','Referral','Report','PatientPortal','Ambulance','Chatbot','CMS','License')
            }

            $createRun = Invoke-Step -Name 'CreateLicense' -Action {
                & (Join-Path $PSScriptRoot 'Invoke-LicenseToolAutomation.ps1') -Mode CreateLicense -PrivateKeyPath $privateKey.FullName -OutputDirectory $licenseOutput -TenantId ([string]($config['TenantId'] ?? 'uat-tenant')) -MaxConcurrentUsers ([int]($config['MaxConcurrentUsers'] ?? 5)) -ExpiryChoice ([string]($config['ExpiryChoice'] ?? '2')) -Modules $modules
            }
            $results.LicenseTool.CreateLicense = $createRun

            $licensePath = Join-Path $licenseOutput 'MedyxHMS.lic'
            $results.LicenseTool.LicensePath = $licensePath
            $results.LicenseTool.LicenseExists = Test-Path $licensePath
            if (Test-Path $licensePath) {
                $raw = Get-Content -Raw -Path $licensePath
                $results.LicenseTool.HasEncodedPrefix = $raw.StartsWith('MEDYX-LIC-V1:')
            }
        }
    }
}

if ($BaseUrl) {
    foreach ($path in @('/', '/Account/Login', '/Chatbot', '/health')) {
        $key = $path.Trim('/').Replace('/', '_')
        if ([string]::IsNullOrWhiteSpace($key)) { $key = 'root' }
        $results.HttpChecks[$key] = Invoke-Step -Name $key -Action {
            $response = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd('/') + $path) -UseBasicParsing
            [ordered]@{
                StatusCode = [int]$response.StatusCode
                HasLoginText = $response.Content -match 'Login'
                HasAiLauncher = $response.Content -match 'chatbot-fab'
            }
        }
    }
}

New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($ResultsPath)) | Out-Null
$results | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultsPath
$results | ConvertTo-Json -Depth 8
