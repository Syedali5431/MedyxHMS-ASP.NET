# Stage 5 Notification Soak Test Script
$baseUrl = "http://localhost:5105"
$superAdminEmail = "superadmin@hospital.com"
$superAdminPwd = "SuperAdmin@123"

# Login function
function Get-AuthToken {
    param([string]$email, [string]$pwd)
    $loginUrl = "$baseUrl/Account/Login"
    $response = Invoke-WebRequest -Uri $loginUrl -SessionVariable session -UseBasicParsing
    $token = ([regex]::Matches($response.Content, 'name="__RequestVerificationToken"\s+value="([^"]+)"') | Select-Object -First 1).Groups[1].Value
    $loginBody = @{
        Email = $email
        Password = $pwd
        "__RequestVerificationToken" = $token
    }
    Invoke-WebRequest -Uri $loginUrl -Method POST -Body $loginBody -WebSession $session -UseBasicParsing | Out-Null
    return $session
}

# Test SMTP Health Check
function Test-SmtpHealth {
    param([object]$session)
    $healthUrl = "$baseUrl/Cms/RunSmtpHealthCheck"
    try {
        $response = Invoke-WebRequest -Uri $healthUrl -WebSession $session -UseBasicParsing -TimeoutSec 30
        if($response.StatusCode -eq 200) {
            return @{ Status = "PASS"; StatusCode = 200; Message = "SMTP health check passed" }
        }
    } catch {
        return @{ Status = "FAIL"; StatusCode = $_.Exception.Response.StatusCode; Message = $_.Exception.Message }
    }
}

# Test SMS Send
function Test-SmsSend {
    param([object]$session, [string]$phone)
    $smsUrl = "$baseUrl/Cms/SendTestSms"
    try {
        $smsBody = @{ PhoneNumber = $phone; Message = "Test SMS from Stage 5 soak test"; __RequestVerificationToken = (Get-CsrfToken $session) }
        $response = Invoke-WebRequest -Uri $smsUrl -Method POST -Body $smsBody -WebSession $session -UseBasicParsing -TimeoutSec 30
        if($response.StatusCode -eq 200) {
            return @{ Status = "PASS"; StatusCode = 200; Message = "SMS test sent" }
        }
    } catch {
        return @{ Status = "FAIL"; StatusCode = $_.Exception.Response.StatusCode; Message = $_.Exception.Message }
    }
}

# Test Email Send
function Test-EmailSend {
    param([object]$session, [string]$email)
    $emailUrl = "$baseUrl/Cms/SendTestEmail"
    try {
        $emailBody = @{ Email = $email; Subject = "Stage 5 SMTP Test"; Message = "This is a test email from Stage 5 notification soak test"; __RequestVerificationToken = (Get-CsrfToken $session) }
        $response = Invoke-WebRequest -Uri $emailUrl -Method POST -Body $emailBody -WebSession $session -UseBasicParsing -TimeoutSec 30
        if($response.StatusCode -eq 200) {
            return @{ Status = "PASS"; StatusCode = 200; Message = "Email test sent" }
        }
    } catch {
        return @{ Status = "FAIL"; StatusCode = $_.Exception.Response.StatusCode; Message = $_.Exception.Message }
    }
}

# Get CSRF Token
function Get-CsrfToken {
    param([object]$session)
    $response = Invoke-WebRequest -Uri "$baseUrl/Cms/Settings" -WebSession $session -UseBasicParsing
    $token = ([regex]::Matches($response.Content, 'name="__RequestVerificationToken"\s+value="([^"]+)"') | Select-Object -First 1).Groups[1].Value
    return $token
}

# Main soak test execution
$session = Get-AuthToken $superAdminEmail $superAdminPwd
$results = @()

# Run SMTP health check
"Testing SMTP health..." | Write-Host
$smtpHealth = Test-SmtpHealth $session
$results += @{ Test = "SMTP_HEALTH_CHECK"; Result = $smtpHealth.Status; Details = $smtpHealth.Message; Timestamp = [DateTime]::UtcNow }

# Run SMS test (5 iterations)
"Testing SMS delivery..." | Write-Host
1..5 | ForEach-Object {
    $smsResult = Test-SmsSend $session "+1555000$($_.ToString('0000'))"
    $results += @{ Test = "SMS_TEST_$_"; Result = $smsResult.Status; Details = $smsResult.Message; Timestamp = [DateTime]::UtcNow }
}

# Run email test (5 iterations)
"Testing Email delivery..." | Write-Host
1..5 | ForEach-Object {
    $emailResult = Test-EmailSend $session "testuser$($_)@hospital.local"
    $results += @{ Test = "EMAIL_TEST_$_"; Result = $emailResult.Status; Details = $emailResult.Message; Timestamp = [DateTime]::UtcNow }
}

# Output results
$results | ConvertTo-Json -Depth 10 | Out-File -FilePath "temp_build_output/stage5-soak-test-results-2026-04-24.json"
"SOAK_TEST_COMPLETED=true"
$results | Select-Object Test, Result, Details | Format-Table -AutoSize
