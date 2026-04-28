param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('GenerateKey', 'CreateLicense')]
    [string]$Mode,

    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\MedyxHMS-Lic\MedyxHMS-Lic.csproj'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\temp_build_output\license-smoke'),
    [string]$PrivateKeyPath,
    [string]$TenantId = 'uat-tenant',
    [ValidateSet('1','2','3','4','5')]
    [string]$ExpiryChoice = '2',
    [string]$CustomExpiryDate,
    [int]$MaxConcurrentUsers = 5,
    [string[]]$Modules = @('Dashboard','Patient','Appointment','OPD','IPD','Billing','Prescription','Lab','Radiology','BloodBank','OperationTheatre','FrontOffice','Attendance','Leave','Payroll','Certificate','Referral','Report','PatientPortal','Ambulance','Chatbot','CMS','License','BirthDeath','TPA','Messaging','Inventory','DownloadCenter','LiveConsultation','BedManagement')
)

$ErrorActionPreference = 'Stop'

$allModules = @('Dashboard','Patient','Appointment','OPD','IPD','Billing','Prescription','Lab','Radiology','BloodBank','OperationTheatre','FrontOffice','Attendance','Leave','Payroll','Certificate','Referral','Report','PatientPortal','Ambulance','Chatbot','CMS','License','BirthDeath','TPA','Messaging','Inventory','DownloadCenter','LiveConsultation','BedManagement')
$basicModules = @('Dashboard','Patient','Appointment','Billing','FrontOffice','Referral','Report','PatientPortal')
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$inputLines = New-Object System.Collections.Generic.List[string]
if ($Mode -eq 'GenerateKey') {
    $inputLines.Add('1')
    $inputLines.Add($OutputDirectory)
    $inputLines.Add('3')
}
else {
    if (-not $PrivateKeyPath) {
        throw 'PrivateKeyPath is required for CreateLicense mode.'
    }

    $inputLines.Add('2')
    $inputLines.Add($PrivateKeyPath)
    $inputLines.Add($TenantId)
    $inputLines.Add($ExpiryChoice)
    if ($ExpiryChoice -eq '5') {
        if (-not $CustomExpiryDate) {
            throw 'CustomExpiryDate is required when ExpiryChoice is 5.'
        }
        $inputLines.Add($CustomExpiryDate)
    }
    $inputLines.Add($MaxConcurrentUsers.ToString())

    foreach ($module in $allModules) {
        if ($basicModules -contains $module) {
            continue
        }

        if ($Modules -contains $module) {
            $inputLines.Add('Y')
        }
        else {
            $inputLines.Add('N')
        }
    }

    $inputLines.Add($OutputDirectory)
    $inputLines.Add('3')
}

$sessionId = [Guid]::NewGuid().ToString('N')
$stdinPath = Join-Path $OutputDirectory ("license-tool-input-{0}.txt" -f $sessionId)
$stdoutPath = Join-Path $OutputDirectory ("license-tool-stdout-{0}.txt" -f $sessionId)
$stderrPath = Join-Path $OutputDirectory ("license-tool-stderr-{0}.txt" -f $sessionId)

[System.IO.File]::WriteAllLines($stdinPath, $inputLines)

$process = Start-Process -FilePath 'dotnet' `
    -ArgumentList @('run', '--project', $ProjectPath) `
    -NoNewWindow `
    -PassThru `
    -RedirectStandardInput $stdinPath `
    -RedirectStandardOutput $stdoutPath `
    -RedirectStandardError $stderrPath

$process.WaitForExit()

$stdout = if (Test-Path $stdoutPath) { Get-Content -Raw -Path $stdoutPath } else { '' }
$stderr = if (Test-Path $stderrPath) { Get-Content -Raw -Path $stderrPath } else { '' }

$exitCode = 0
if ($null -ne $process.ExitCode) {
    $exitCode = [int]$process.ExitCode
}

if ($exitCode -ne 0) {
    throw "License tool failed with exit code $exitCode.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
}

[pscustomobject]@{
    ExitCode = $exitCode
    OutputDirectory = $OutputDirectory
    StdOut = $stdout
    StdErr = $stderr
}
