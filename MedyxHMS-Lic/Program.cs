using System.Globalization;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

Console.WriteLine("MedyxHMS-Lic Vendor Licensing Tool");
Console.WriteLine("Private key must remain only on vendor-controlled systems.");
Console.WriteLine();

var availableModules = new (string Key, string Label)[]
{
    ("Dashboard", "Dashboard"),
    ("Patient", "Patient Management"),
    ("Appointment", "Appointments"),
    ("OPD", "Outpatient Department"),
    ("IPD", "Inpatient Department"),
    ("Billing", "Billing"),
    ("Prescription", "Pharmacy and Prescription"),
    ("Lab", "Laboratory"),
    ("Radiology", "Radiology"),
    ("BloodBank", "Blood Bank"),
    ("OperationTheatre", "Operation Theatre"),
    ("FrontOffice", "Front Office"),
    ("Attendance", "Attendance"),
    ("Leave", "Leave Management"),
    ("Payroll", "Payroll"),
    ("Certificate", "Certificates and ID Cards"),
    ("Referral", "Referrals"),
    ("Report", "Reports"),
    ("PatientPortal", "Patient Portal"),
    ("Ambulance", "Ambulance Management"),
    ("Chatbot", "Chatbot"),
    ("CMS", "CMS and Public Website")
};

var basicModuleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Dashboard",
    "Patient",
    "Appointment",
    "Billing",
    "FrontOffice",
    "Referral",
    "Report",
    "PatientPortal"
};

while (true)
{
    Console.WriteLine("1) Generate RSA key pair (one-time)");
    Console.WriteLine("2) Create signed license (.lic)");
    Console.WriteLine("3) Exit");
    Console.Write("Select option: ");
    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1":
                GenerateKeyPair();
                break;
            case "2":
                CreateSignedLicense(availableModules, basicModuleKeys);
                break;
            case "3":
                return;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }

    Console.WriteLine();
}

static void GenerateKeyPair()
{
    Console.Write("Output directory (empty = current): ");
    var outputDir = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Directory.GetCurrentDirectory();

    Directory.CreateDirectory(outputDir);

    var keyId = GenerateStrongKeyId();
    using var rsa = RSA.Create(4096);
    var privateParams = rsa.ExportParameters(true);
    var publicParams = rsa.ExportParameters(false);

    var publicKey = new PublicKeyFile
    {
        KeyId = keyId,
        Algorithm = "RSA",
        KeySize = 4096,
        CreatedAtUtc = DateTime.UtcNow,
        ModulusHex = ToHex(publicParams.Modulus),
        ExponentHex = ToHex(publicParams.Exponent)
    };
    publicKey.VerificationKey = ComputeVerificationKey(publicKey.ModulusHex, publicKey.ExponentHex);

    var privateKey = new PrivateKeyFile
    {
        KeyId = keyId,
        Algorithm = "RSA",
        KeySize = 4096,
        CreatedAtUtc = DateTime.UtcNow,
        ModulusHex = ToHex(privateParams.Modulus),
        ExponentHex = ToHex(privateParams.Exponent),
        DHex = ToHex(privateParams.D),
        PHex = ToHex(privateParams.P),
        QHex = ToHex(privateParams.Q),
        DPHex = ToHex(privateParams.DP),
        DQHex = ToHex(privateParams.DQ),
        InverseQHex = ToHex(privateParams.InverseQ)
    };
    privateKey.VerificationKey = publicKey.VerificationKey;

    var publicPath = Path.Combine(outputDir, $"medyxhms-public-key-{keyId}.json");
    var privatePath = Path.Combine(outputDir, $"medyxhms-private-key-{keyId}.json");

    WriteJson(publicPath, publicKey);
    WriteJson(privatePath, privateKey);
    TryHardenPrivateKeyFile(privatePath);

    Console.WriteLine($"Public key file:  {publicPath}");
    Console.WriteLine($"Private key file: {privatePath}");
    Console.WriteLine($"Verification Key: {publicKey.VerificationKey}");
    Console.WriteLine("Each generation creates a fresh key identity. Re-run option 1 for unlimited new keys.");
    Console.WriteLine("Deploy ONLY the public key modulus/exponent to MedyxHMS settings.");
}

static void CreateSignedLicense((string Key, string Label)[] availableModules, HashSet<string> basicModuleKeys)
{
    Console.Write("Path to private key JSON: ");
    var privatePath = Console.ReadLine()?.Trim() ?? string.Empty;
    if (!File.Exists(privatePath))
        throw new FileNotFoundException("Private key file not found.", privatePath);

    var privateKey = JsonSerializer.Deserialize<PrivateKeyFile>(File.ReadAllText(privatePath))
                     ?? throw new InvalidDataException("Private key file is invalid.");

    ValidatePrivateKeyFile(privateKey, privatePath);

    Console.Write("TenantId: ");
    var tenantId = (Console.ReadLine() ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(tenantId))
        throw new InvalidDataException("TenantId is required.");

    var issuedAtUtc = DateTime.UtcNow;
    var minimumExpiryDateUtc = issuedAtUtc.Date.AddMonths(1);
    var expiresAtUtc = SelectExpiryDateUtc(issuedAtUtc, minimumExpiryDateUtc);

    Console.Write("MaxConcurrentUsers: ");
    var maxUsersText = (Console.ReadLine() ?? string.Empty).Trim();
    if (!int.TryParse(maxUsersText, out var maxUsers) || maxUsers <= 0)
        throw new InvalidDataException("MaxConcurrentUsers must be a positive integer.");

    var licensedModules = PromptModuleChecklist(availableModules, basicModuleKeys);

    var payload = new LicensePayload
    {
        ProductName = "MedyxHMS",
        TenantId = tenantId,
        LicenseId = Guid.NewGuid(),
        IssuedAt = issuedAtUtc,
        ExpiresAt = expiresAtUtc,
        MaxConcurrentUsers = maxUsers,
        VerificationKey = ResolveVerificationKey(privateKey),
        LicensedModules = licensedModules,
        Nonce = GenerateNonceHex(32)
    };

    var canonical = BuildCanonicalPayloadJson(payload);
    var signatureHex = SignWithPrivateKey(privateKey, canonical);

    var license = new SignedLicenseFile
    {
        Payload = payload,
        Algorithm = "RSA-SHA256",
        SignatureHex = signatureHex
    };

    Console.Write("Output directory (empty = current): ");
    var outputDir = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Directory.GetCurrentDirectory();
    Directory.CreateDirectory(outputDir);

    var outputPath = Path.Combine(outputDir, "MedyxHMS.lic");

    WriteEncodedLicenseFile(outputPath, license);
    Console.WriteLine($"License created: {outputPath}");
    Console.WriteLine($"LicenseId: {payload.LicenseId:D}");
}

static void WriteEncodedLicenseFile(string path, SignedLicenseFile license)
{
    var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = false });
    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    File.WriteAllText(path, $"MEDYX-LIC-V1:{encoded}");
}

static DateTime SelectExpiryDateUtc(DateTime issuedAtUtc, DateTime minimumExpiryDateUtc)
{
    Console.WriteLine("Select expiry option:");
    Console.WriteLine($"1) 1 month trial (minimum allowed: {minimumExpiryDateUtc:yyyy-MM-dd})");
    Console.WriteLine("2) 1 year");
    Console.WriteLine("3) 2 years");
    Console.WriteLine("4) 3 years");
    Console.WriteLine("5) Custom date (yyyy-MM-dd)");
    Console.Write("Choice: ");
    var choice = (Console.ReadLine() ?? string.Empty).Trim();

    DateTime selectedDateUtc;
    switch (choice)
    {
        case "1":
            selectedDateUtc = minimumExpiryDateUtc;
            break;
        case "2":
            selectedDateUtc = issuedAtUtc.Date.AddYears(1);
            break;
        case "3":
            selectedDateUtc = issuedAtUtc.Date.AddYears(2);
            break;
        case "4":
            selectedDateUtc = issuedAtUtc.Date.AddYears(3);
            break;
        case "5":
            Console.Write($"Expiry date UTC (yyyy-MM-dd, min {minimumExpiryDateUtc:yyyy-MM-dd}): ");
            var expiryText = (Console.ReadLine() ?? string.Empty).Trim();
            if (!DateTime.TryParseExact(expiryText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var customDate))
                throw new InvalidDataException("Invalid expiry date format.");
            selectedDateUtc = customDate.Date;
            if (selectedDateUtc < minimumExpiryDateUtc)
                throw new InvalidDataException($"Minimum expiry date is {minimumExpiryDateUtc:yyyy-MM-dd}.");
            break;
        default:
            throw new InvalidDataException("Invalid expiry option.");
    }

    return DateTime.SpecifyKind(selectedDateUtc.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
}

static List<string> PromptModuleChecklist((string Key, string Label)[] availableModules, HashSet<string> basicModuleKeys)
{
    var selected = availableModules
        .Where(m => basicModuleKeys.Contains(m.Key))
        .Select(m => m.Key)
        .ToList();

    Console.WriteLine("Basic modules are always included:");
    foreach (var module in availableModules.Where(m => basicModuleKeys.Contains(m.Key)))
        Console.WriteLine($"- {module.Label} ({module.Key}) [included]");

    Console.WriteLine();
    Console.WriteLine("Select optional modules (Y = include, N = lock for non-SuperAdmin users):");

    foreach (var (key, label) in availableModules)
    {
        if (basicModuleKeys.Contains(key))
            continue;

        Console.Write($"- {label} ({key}) [Y/n]: ");
        var answer = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(answer) || answer.Equals("y", StringComparison.OrdinalIgnoreCase) || answer.Equals("yes", StringComparison.OrdinalIgnoreCase))
            selected.Add(key);
    }

    if (selected.Count == 0)
        throw new InvalidDataException("At least one module must be selected.");

    return selected
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static string SignWithPrivateKey(PrivateKeyFile key, string canonicalPayload)
{
    using var rsa = RSA.Create();
    rsa.ImportParameters(new RSAParameters
    {
        Modulus = FromHex(key.ModulusHex),
        Exponent = FromHex(key.ExponentHex),
        D = FromHex(key.DHex),
        P = FromHex(key.PHex),
        Q = FromHex(key.QHex),
        DP = FromHex(key.DPHex),
        DQ = FromHex(key.DQHex),
        InverseQ = FromHex(key.InverseQHex)
    });

    var data = Encoding.UTF8.GetBytes(canonicalPayload);
    var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    return Convert.ToHexString(signature);
}

static void ValidatePrivateKeyFile(PrivateKeyFile key, string path)
{
    static bool Missing(string? value) => string.IsNullOrWhiteSpace(value);

    if (Missing(key.ModulusHex) || Missing(key.ExponentHex))
        throw new InvalidDataException($"Invalid private key JSON: missing ModulusHex/ExponentHex in '{path}'.");

    if (Missing(key.DHex) || Missing(key.PHex) || Missing(key.QHex) || Missing(key.DPHex) || Missing(key.DQHex) || Missing(key.InverseQHex))
    {
        throw new InvalidDataException(
            $"Invalid private key JSON: RSA private parameters are missing in '{path}'. " +
            "You likely selected a public key JSON. Use medyxhms-private-key-*.json for license generation.");
    }
}

static string BuildCanonicalPayloadJson(LicensePayload payload)
{
    static string Q(string value) => JsonSerializer.Serialize(value ?? string.Empty);

    var normalizedModules = (payload.LicensedModules ?? new List<string>())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();
    var modulesJson = JsonSerializer.Serialize(normalizedModules);

    return "{" +
           "\"ProductName\":" + Q(payload.ProductName) + "," +
           "\"TenantId\":" + Q(payload.TenantId) + "," +
           "\"LicenseId\":" + Q(payload.LicenseId.ToString("D")) + "," +
           "\"IssuedAt\":" + Q(payload.IssuedAt.ToUniversalTime().ToString("O")) + "," +
           "\"ExpiresAt\":" + Q(payload.ExpiresAt.ToUniversalTime().ToString("O")) + "," +
           "\"MaxConcurrentUsers\":" + payload.MaxConcurrentUsers + "," +
           "\"VerificationKey\":" + Q(payload.VerificationKey) + "," +
           "\"LicensedModules\":" + modulesJson + "," +
           "\"Nonce\":" + Q(payload.Nonce) +
           "}";
}

static string ResolveVerificationKey(PrivateKeyFile privateKey)
{
    if (!string.IsNullOrWhiteSpace(privateKey.VerificationKey))
        return NormalizeHex(privateKey.VerificationKey);

    return ComputeVerificationKey(privateKey.ModulusHex, privateKey.ExponentHex);
}

static string ComputeVerificationKey(string modulusHex, string exponentHex)
{
    var normalizedModulus = NormalizeHex(modulusHex);
    var normalizedExponent = NormalizeHex(exponentHex);
    var material = $"MEDYXHMS-VERIFY|{normalizedModulus}|{normalizedExponent}";
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
    return Convert.ToHexString(hash);
}

static string GenerateNonceHex(int byteLength)
{
    var bytes = RandomNumberGenerator.GetBytes(byteLength);
    return Convert.ToHexString(bytes);
}

static string GenerateStrongKeyId()
{
    // 256-bit random ID to keep key identities effectively collision-free.
    return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}

static string ToHex(byte[]? bytes)
{
    return bytes == null || bytes.Length == 0 ? string.Empty : Convert.ToHexString(bytes);
}

static string NormalizeHex(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

    var cleaned = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).Trim();
    if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        cleaned = cleaned[2..];

    return cleaned.ToUpperInvariant();
}

static byte[] FromHex(string hex)
{
    if (string.IsNullOrWhiteSpace(hex))
        return Array.Empty<byte>();

    return Convert.FromHexString(hex);
}

static void WriteJson<T>(string path, T payload)
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(path, JsonSerializer.Serialize(payload, options));
}

static void TryHardenPrivateKeyFile(string privatePath)
{
    if (!OperatingSystem.IsWindows())
        return;

    try
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var currentUser = WindowsIdentity.GetCurrent();
        if (currentUser.User != null)
        {
            var accessRule = new FileSystemAccessRule(
                currentUser.User,
                FileSystemRights.FullControl,
                AccessControlType.Allow);
            security.AddAccessRule(accessRule);
            var fileInfo = new FileInfo(privatePath);
            fileInfo.SetAccessControl(security);
        }
    }
    catch
    {
        // Best-effort hardening; external controls (HSM/Cert Store/ACL policy) should still be applied.
    }
}

internal sealed class PublicKeyFile
{
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RSA";
    public int KeySize { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string ModulusHex { get; set; } = string.Empty;
    public string ExponentHex { get; set; } = string.Empty;
    public string VerificationKey { get; set; } = string.Empty;
}

internal sealed class PrivateKeyFile
{
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RSA";
    public int KeySize { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string ModulusHex { get; set; } = string.Empty;
    public string ExponentHex { get; set; } = string.Empty;
    public string DHex { get; set; } = string.Empty;
    public string PHex { get; set; } = string.Empty;
    public string QHex { get; set; } = string.Empty;
    public string DPHex { get; set; } = string.Empty;
    public string DQHex { get; set; } = string.Empty;
    public string InverseQHex { get; set; } = string.Empty;
    public string VerificationKey { get; set; } = string.Empty;
}

internal sealed class SignedLicenseFile
{
    public LicensePayload Payload { get; set; } = new();
    public string SignatureHex { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RSA-SHA256";
}

internal sealed class LicensePayload
{
    public string ProductName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public Guid LicenseId { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MaxConcurrentUsers { get; set; }
    public string VerificationKey { get; set; } = string.Empty;
    public List<string> LicensedModules { get; set; } = new();
    public string Nonce { get; set; } = string.Empty;
}
