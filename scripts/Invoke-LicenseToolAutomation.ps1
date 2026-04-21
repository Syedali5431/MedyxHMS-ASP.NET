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
    [string[]]$Modules = @('Dashboard','Patient','Appointment','OPD','IPD','Billing','Prescription','Lab','Radiology','BloodBank','OperationTheatre','FrontOffice','Attendance','Leave','Payroll','Certificate','Referral','Report','PatientPortal','Ambulance','Chatbot','CMS','License')
)

$ErrorActionPreference = 'Stop'

$allModules = @('Dashboard','Patient','Appointment','OPD','IPD','Billing','Prescription','Lab','Radiology','BloodBank','OperationTheatre','FrontOffice','Attendance','Leave','Payroll','Certificate','Referral','Report','PatientPortal','Ambulance','Chatbot','CMS','License')
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = 'dotnet'
$processInfo.Arguments = ('run --project "{0}"' -f $ProjectPath)
$processInfo.RedirectStandardInput = $true
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo
$null = $process.Start()

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

foreach ($line in $inputLines) {
    $process.StandardInput.WriteLine($line)
}
$process.StandardInput.Close()

$stdout = $process.StandardOutput.ReadToEnd()
$stderr = $process.StandardError.ReadToEnd()
$process.WaitForExit()

if ($process.ExitCode -ne 0) {
    throw "License tool failed with exit code $($process.ExitCode).`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
}

[pscustomobject]@{
    ExitCode = $process.ExitCode
    OutputDirectory = $OutputDirectory
    StdOut = $stdout
    StdErr = $stderr
}
