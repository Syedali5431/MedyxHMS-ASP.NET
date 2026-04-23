param(
    [string]$BaseUrl = "http://localhost:5044",
    [string]$OutputPath = "e:\HMS\MedyxHMS-ASPNET\temp_build_output\uat-role-run-current.json"
)

$ErrorActionPreference = "Stop"
$root = "e:\HMS\MedyxHMS-ASPNET"

$staffRoutes = (Get-Content "$root\Views\Shared\Components\SidebarNav\Default.cshtml" -Raw |
    Select-String -Pattern 'href="/[^"#]+' -AllMatches).Matches.Value |
    ForEach-Object { $_.Substring(6) } |
    Sort-Object -Unique
$staffRoutes += @('/Dashboard', '/BedManagement', '/bed-management', '/api/beds')
$staffRoutes = $staffRoutes | Sort-Object -Unique

$patientRoutes = @(
    '/PatientPortal/Dashboard',
    '/PatientPortal/Appointments',
    '/PatientPortal/Bills',
    '/PatientPortal/MedicalRecords',
    '/PatientPortal/Settings'
)

$users = @(
    @{ Name = 'superadmin'; Email = 'superadmin@hospital.com'; Password = 'SuperAdmin@123!'; Role = 'SuperAdmin'; Routes = $staffRoutes },
    @{ Name = 'admin'; Email = 'admin.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Admin'; Routes = $staffRoutes },
    @{ Name = 'doctor'; Email = 'doctor.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Doctor'; Routes = $staffRoutes },
    @{ Name = 'nurse'; Email = 'nurse.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Nurse'; Routes = $staffRoutes },
    @{ Name = 'accountant'; Email = 'accountant.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Accountant'; Routes = $staffRoutes },
    @{ Name = 'receptionist'; Email = 'receptionist.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Receptionist'; Routes = $staffRoutes },
    @{ Name = 'multirole-doctor'; Email = 'multirole.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Doctor'; Routes = $staffRoutes },
    @{ Name = 'multirole-nurse'; Email = 'multirole.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Nurse'; Routes = $staffRoutes },
    @{ Name = 'patient'; Email = 'patient.uat@hospital.com'; Password = 'UatRole@123!'; Role = 'Patient'; Routes = $patientRoutes }
)

function Get-Token([string]$html) {
    $m = [regex]::Match($html, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    if (-not $m.Success) {
        $m = [regex]::Match($html, 'name="__RequestVerificationToken"\s+value="([^"]+)"')
    }
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

$report = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    BaseUrl      = $BaseUrl
    Results      = @()
}

foreach ($u in $users) {
    $entry = [ordered]@{
        User         = $u.Name
        Email        = $u.Email
        Role         = $u.Role
        Login        = ''
        ValidateRoles = ''
        RouteChecks  = @()
        Summary      = @{}
    }

    $sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession

    try {
        $loginGet = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -WebSession $sess -UseBasicParsing -TimeoutSec 20
        $token = Get-Token $loginGet.Content
        if ([string]::IsNullOrWhiteSpace($token)) { throw 'Missing antiforgery token on login page' }

        $validateBody = @{
            email = $u.Email
            password = $u.Password
            __RequestVerificationToken = $token
        }
        $validateResp = Invoke-WebRequest -Uri "$BaseUrl/Account/ValidateCredentials" -Method Post -Body $validateBody -WebSession $sess -UseBasicParsing -TimeoutSec 20 -ContentType 'application/x-www-form-urlencoded'
        $validateJson = $validateResp.Content | ConvertFrom-Json
        $entry.ValidateRoles = ($validateJson | ConvertTo-Json -Compress)

        if (-not $validateJson.success) { throw "ValidateCredentials failed: $($validateJson.message)" }
        if (-not ($validateJson.roles -contains $u.Role)) { throw "Role $($u.Role) not offered for user" }

        $loginGet2 = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -WebSession $sess -UseBasicParsing -TimeoutSec 20
        $token2 = Get-Token $loginGet2.Content
        $loginBody = @{
            Email = $u.Email
            Password = $u.Password
            RememberMe = 'false'
            SelectedRole = $u.Role
            __RequestVerificationToken = $token2
        }
        $null = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $sess -UseBasicParsing -TimeoutSec 25 -ContentType 'application/x-www-form-urlencoded'
        $entry.Login = 'OK'

        foreach ($r in $u.Routes) {
            $status = 0
            $ok = $false
            $note = ''
            try {
                $resp = Invoke-WebRequest -Uri ($BaseUrl + $r) -WebSession $sess -UseBasicParsing -TimeoutSec 20 -MaximumRedirection 0
                $status = [int]$resp.StatusCode
                $ok = ($status -eq 200)
            }
            catch {
                if ($_.Exception.Response) {
                    $status = [int]$_.Exception.Response.StatusCode.value__
                    $ok = ($status -in 301, 302, 303, 307, 308, 401, 403)
                    $note = $_.Exception.Message
                }
                else {
                    $status = -1
                    $note = $_.Exception.Message
                }
            }

            $entry.RouteChecks += [ordered]@{
                Route = $r
                Status = $status
                Acceptable = $ok
                Note = $note
            }
        }

        $total = $entry.RouteChecks.Count
        $bad = @($entry.RouteChecks | Where-Object { -not $_.Acceptable })
        $entry.Summary = [ordered]@{
            Total = $total
            FailCount = $bad.Count
            PassCount = ($total - $bad.Count)
        }
    }
    catch {
        $entry.Login = 'FAILED: ' + $_.Exception.Message
    }

    $report.Results += $entry
}

$report | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath

$flat = @()
foreach ($r in $report.Results) {
    $fails = if ($r.RouteChecks) { @($r.RouteChecks | Where-Object { -not $_.Acceptable }).Count } else { -1 }
    $flat += [pscustomobject]@{
        User  = $r.User
        Role  = $r.Role
        Login = $r.Login
        Total = if ($r.Summary.Total) { $r.Summary.Total } else { 0 }
        Fails = $fails
    }
}

$flat | Format-Table -AutoSize
Write-Output "RESULT_FILE=$OutputPath"
