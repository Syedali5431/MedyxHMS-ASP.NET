using System.Text.Json;
using System.Text;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for LicenseFileService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class LicenseFileService : ILicenseFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingService _settingService;
        private readonly IDataProtector _storageProtector;
        private readonly IWebHostEnvironment _environment;

        public LicenseFileService(
            ApplicationDbContext context,
            ISettingService settingService,
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment)
        {
            _context = context;
            _settingService = settingService;
            _storageProtector = dataProtectionProvider.CreateProtector("MedyxHMS.License.Storage.v1");
            _environment = environment;
        }

        public async Task<LicenseRecord> ValidateAndActivateAsync(IFormFile licenseFile, string performedByUserId, string? ipAddress = null)
        {
            if (licenseFile == null || licenseFile.Length == 0)
                throw new InvalidDataException("License file is required.");

            if (!string.Equals(Path.GetExtension(licenseFile.FileName), ".lic", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid file extension. Only .lic files are allowed.");

            string rawJson;
            using (var stream = licenseFile.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                rawJson = await reader.ReadToEndAsync();
            }

            var decodedJson = DecodeLicenseContent(rawJson);

            var signed = JsonSerializer.Deserialize<SignedLicenseFile>(decodedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidDataException("Invalid license JSON format.");

            if (signed.Payload == null)
                throw new InvalidDataException("License payload is missing.");

            if (!string.Equals(signed.Algorithm, "RSA-SHA256", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported signature algorithm.");

            ValidatePayloadFields(signed.Payload);

            var normalizedModules = NormalizeModuleKeys(signed.Payload.LicensedModules);
            var knownModuleKeys = await _context.SystemModules
                .Select(x => x.Key)
                .ToListAsync();
            var unknownModules = normalizedModules
                .Where(x => !knownModuleKeys.Contains(x, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (unknownModules.Count > 0)
                throw new InvalidDataException($"License contains unknown module keys: {string.Join(", ", unknownModules)}");

            var expectedProduct = (await _settingService.GetSettingValueAsync("LicenseExpectedProductName"))?.Trim();
            if (string.IsNullOrWhiteSpace(expectedProduct))
                expectedProduct = "MedyxHMS";

            var expectedTenant = (await _settingService.GetSettingValueAsync("LicenseTenantId"))?.Trim();
            if (string.IsNullOrWhiteSpace(expectedTenant))
                throw new InvalidOperationException("LicenseTenantId setting is missing. Configure tenant before importing licenses.");

            var payloadTenant = signed.Payload.TenantId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(payloadTenant))
                throw new InvalidDataException("License TenantId is missing.");

            // Bootstrap tenant setting from the first real license import when the system is still in default state.
            if (string.Equals(expectedTenant, "UNCONFIGURED", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(payloadTenant, expectedTenant, StringComparison.OrdinalIgnoreCase))
            {
                await _settingService.UpdateSettingAsync("LicenseTenantId", payloadTenant);
                expectedTenant = payloadTenant;
            }

            if (!string.Equals(signed.Payload.ProductName, expectedProduct, StringComparison.Ordinal))
                throw new InvalidDataException("License product mismatch.");

            if (!string.Equals(payloadTenant, expectedTenant, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"License tenant mismatch. Expected '{expectedTenant}' but got '{payloadTenant}'.");

            if (signed.Payload.ExpiresAt.ToUniversalTime() < DateTime.UtcNow)
                throw new InvalidDataException("License is already expired.");

            var modulusHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyModulusHex"))?.Trim();
            var exponentHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyExponentHex"))?.Trim();
            if (string.IsNullOrWhiteSpace(modulusHex) || string.IsNullOrWhiteSpace(exponentHex))
            {
                var loaded = await TryLoadPublicKeySettingsFromDefaultPathAsync(signed.Payload.VerificationKey?.Trim());
                if (loaded)
                {
                    modulusHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyModulusHex"))?.Trim();
                    exponentHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyExponentHex"))?.Trim();
                }
            }
            if (string.IsNullOrWhiteSpace(modulusHex) || string.IsNullOrWhiteSpace(exponentHex))
                throw new InvalidOperationException("License public key settings are missing.");

            var configuredVerificationKey = (await _settingService.GetSettingValueAsync("LicenseVerificationKey"))?.Trim();
            var expectedVerificationKey = LicenseCryptoUtility.ComputeVerificationKey(modulusHex, exponentHex);
            if (string.IsNullOrWhiteSpace(configuredVerificationKey))
            {
                await _settingService.UpdateSettingAsync("LicenseVerificationKey", expectedVerificationKey);
                configuredVerificationKey = expectedVerificationKey;
            }

            if (!string.Equals(configuredVerificationKey, expectedVerificationKey, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Configured verification key does not match the configured public key.");

            var payloadVerificationKey = signed.Payload.VerificationKey?.Trim();
            if (!string.Equals(payloadVerificationKey, expectedVerificationKey, StringComparison.OrdinalIgnoreCase))
            {
                // If the uploaded license was generated with a newer keypair present in MedyxHMS-Lic/current,
                // switch to that matching public key automatically.
                var loaded = await TryLoadPublicKeySettingsFromDefaultPathAsync(payloadVerificationKey);
                if (!loaded)
                    throw new InvalidDataException("License verification key mismatch. License was not generated for this installation key.");

                modulusHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyModulusHex"))?.Trim();
                exponentHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyExponentHex"))?.Trim();
                configuredVerificationKey = (await _settingService.GetSettingValueAsync("LicenseVerificationKey"))?.Trim();

                if (string.IsNullOrWhiteSpace(modulusHex) || string.IsNullOrWhiteSpace(exponentHex))
                    throw new InvalidOperationException("License public key settings are missing after key update.");

                expectedVerificationKey = LicenseCryptoUtility.ComputeVerificationKey(modulusHex, exponentHex);
                if (!string.Equals(configuredVerificationKey, expectedVerificationKey, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(payloadVerificationKey, expectedVerificationKey, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("License verification key mismatch. License was not generated for this installation key.");
                }
            }

            var duplicateLicense = await _context.LicenseRecords
                .AnyAsync(x => x.LicenseGuid == signed.Payload.LicenseId);
            if (duplicateLicense)
                throw new InvalidDataException("This license has already been imported.");

            var canonical = LicenseCryptoUtility.BuildCanonicalPayloadJson(signed.Payload);
            var signatureValid = LicenseCryptoUtility.VerifyRsaSha256(canonical, signed.SignatureHex, modulusHex, exponentHex);
            if (!signatureValid)
                throw new InvalidDataException("License signature verification failed.");

            var licensedModulesCsv = string.Join(',', normalizedModules);

            var now = DateTime.UtcNow;
            var existingActive = await _context.LicenseRecords.Where(x => x.IsActive).ToListAsync();
            foreach (var row in existingActive)
            {
                row.IsActive = false;
                row.UpdatedAtUtc = now;
            }

            var newRecord = new LicenseRecord
            {
                LicenseReference = signed.Payload.LicenseId.ToString("D"),
                ProductName = signed.Payload.ProductName,
                TenantId = signed.Payload.TenantId,
                LicenseGuid = signed.Payload.LicenseId,
                IssuedAtUtc = signed.Payload.IssuedAt.ToUniversalTime(),
                ExpiresAtUtc = signed.Payload.ExpiresAt.ToUniversalTime(),
                MaxConcurrentUsers = signed.Payload.MaxConcurrentUsers,
                VerificationKey = expectedVerificationKey,
                LicensedModulesCsv = licensedModulesCsv,
                PublicKeyModulusHex = modulusHex,
                PublicKeyExponentHex = exponentHex,
                Nonce = signed.Payload.Nonce,
                SignatureAlgorithm = signed.Algorithm,
                SignatureHex = ProtectForStorage(signed.SignatureHex),
                CanonicalPayloadJson = ProtectForStorage(canonical),
                EncodedLicenseFile = ProtectForStorage(rawJson),
                PayloadSha256Hex = LicenseCryptoUtility.Sha256Hex(canonical),
                IsSignatureValid = true,
                LastValidatedAtUtc = now,
                Status = (signed.Payload.ExpiresAt.ToUniversalTime() < now ? LicenseState.Expired : LicenseState.Active).ToString(),
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Notes = "Imported from digitally signed .lic file."
            };

            _context.LicenseRecords.Add(newRecord);
            await _context.SaveChangesAsync();

            _context.LicenseAuditLogs.Add(new LicenseAuditLog
            {
                LicenseRecordId = newRecord.Id,
                ActionType = "SignedLicenseImported",
                PerformedByUserId = performedByUserId,
                PerformedAtUtc = now,
                NewExpiresAtUtc = newRecord.ExpiresAtUtc,
                Details = $"Signature verified. MaxConcurrentUsers={newRecord.MaxConcurrentUsers}. LicensedModules={newRecord.LicensedModulesCsv}.",
                IpAddress = ipAddress
            });
            await _context.SaveChangesAsync();

            return newRecord;
        }

        private async Task<bool> TryLoadPublicKeySettingsFromDefaultPathAsync(string? expectedVerificationKey)
        {
            try
            {
                var keyDirectory = Path.Combine(_environment.ContentRootPath, "MedyxHMS-Lic", "current");
                if (!Directory.Exists(keyDirectory))
                    return false;

                var publicKeyFiles = Directory
                    .GetFiles(keyDirectory, "medyxhms-public-key-*.json", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ToList();

                foreach (var path in publicKeyFiles)
                {
                    var text = await File.ReadAllTextAsync(path);
                    var key = JsonSerializer.Deserialize<PublicKeyFileDto>(text, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (key == null || string.IsNullOrWhiteSpace(key.ModulusHex) || string.IsNullOrWhiteSpace(key.ExponentHex))
                        continue;

                    var modulusHex = NormalizeHex(key.ModulusHex);
                    var exponentHex = NormalizeHex(key.ExponentHex);
                    if (!IsHex(modulusHex) || !IsHex(exponentHex) || modulusHex.Length % 2 != 0 || exponentHex.Length % 2 != 0)
                        continue;

                    var verificationKey = LicenseCryptoUtility.ComputeVerificationKey(modulusHex, exponentHex);
                    if (!string.IsNullOrWhiteSpace(expectedVerificationKey)
                        && !string.Equals(verificationKey, expectedVerificationKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    await _settingService.UpdateSettingAsync("LicensePublicKeyModulusHex", modulusHex);
                    await _settingService.UpdateSettingAsync("LicensePublicKeyExponentHex", exponentHex);
                    await _settingService.UpdateSettingAsync("LicenseVerificationKey", verificationKey);
                    return true;
                }
            }
            catch
            {
                // Ignore fallback read errors and let the normal validation message be shown.
            }

            return false;
        }

        private static string NormalizeHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = new string(value
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .Trim();

            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[2..];

            return cleaned.ToUpperInvariant();
        }

        private static bool IsHex(string value)
        {
            return value.All(c =>
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'F') ||
                (c >= 'a' && c <= 'f'));
        }

        public async Task<bool> IsCurrentLicenseCryptographicallyValidAsync()
        {
            var license = await _context.LicenseRecords
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (license == null)
                return false;

            if (license.ExpiresAtUtc < DateTime.UtcNow)
                return false;

            if (string.IsNullOrWhiteSpace(license.CanonicalPayloadJson)
                || string.IsNullOrWhiteSpace(license.SignatureHex)
                || !string.Equals(license.SignatureAlgorithm, "RSA-SHA256", StringComparison.Ordinal))
            {
                return false;
            }

            var canonicalPayload = TryUnprotect(license.CanonicalPayloadJson);
            var signatureHex = TryUnprotect(license.SignatureHex);

            if (string.IsNullOrWhiteSpace(canonicalPayload) || string.IsNullOrWhiteSpace(signatureHex))
                return false;

            var modulusHex = license.PublicKeyModulusHex?.Trim();
            var exponentHex = license.PublicKeyExponentHex?.Trim();

            // Backward compatibility for old records before public key persistence.
            if (string.IsNullOrWhiteSpace(modulusHex) || string.IsNullOrWhiteSpace(exponentHex))
            {
                modulusHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyModulusHex"))?.Trim();
                exponentHex = (await _settingService.GetSettingValueAsync("LicensePublicKeyExponentHex"))?.Trim();
            }

            if (string.IsNullOrWhiteSpace(modulusHex) || string.IsNullOrWhiteSpace(exponentHex))
                return false;

            var expectedVerificationKey = LicenseCryptoUtility.ComputeVerificationKey(modulusHex, exponentHex);
            if (!string.Equals(license.VerificationKey, expectedVerificationKey, StringComparison.OrdinalIgnoreCase))
                return false;

            var isValid = LicenseCryptoUtility.VerifyRsaSha256(canonicalPayload, signatureHex, modulusHex, exponentHex);
            if (!isValid && license.IsSignatureValid)
            {
                license.IsSignatureValid = false;
                license.Status = LicenseState.Expired.ToString();
                license.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return isValid;
        }

        private static void ValidatePayloadFields(LicensePayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.ProductName))
                throw new InvalidDataException("ProductName is required.");

            if (string.IsNullOrWhiteSpace(payload.TenantId))
                throw new InvalidDataException("TenantId is required.");

            if (payload.LicenseId == Guid.Empty)
                throw new InvalidDataException("LicenseId is required.");

            if (payload.MaxConcurrentUsers <= 0)
                throw new InvalidDataException("MaxConcurrentUsers must be greater than zero.");

            if (string.IsNullOrWhiteSpace(payload.VerificationKey) || payload.VerificationKey.Length != 64)
                throw new InvalidDataException("VerificationKey is required and must be 64 hex characters.");

            var modules = NormalizeModuleKeys(payload.LicensedModules);
            if (modules.Count == 0)
                throw new InvalidDataException("At least one licensed module must be selected.");

            if (string.IsNullOrWhiteSpace(payload.Nonce) || payload.Nonce.Length < 16)
                throw new InvalidDataException("Nonce must be a strong random value.");

            if (payload.IssuedAt == default || payload.ExpiresAt == default)
                throw new InvalidDataException("IssuedAt and ExpiresAt are required.");

            if (payload.ExpiresAt.ToUniversalTime() <= payload.IssuedAt.ToUniversalTime())
                throw new InvalidDataException("ExpiresAt must be after IssuedAt.");

            if (payload.ExpiresAt.ToUniversalTime() < payload.IssuedAt.ToUniversalTime().AddMonths(1))
                throw new InvalidDataException("Minimum license expiry window is 1 month from issue date.");
        }

        private static List<string> NormalizeModuleKeys(IEnumerable<string>? modules)
        {
            return (modules ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string ProtectForStorage(string value)
        {
            return _storageProtector.Protect(value ?? string.Empty);
        }

        private string TryUnprotect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            try
            {
                return _storageProtector.Unprotect(value);
            }
            catch
            {
                // Backward compatibility for records created before storage protection.
                return value;
            }
        }

        private static string DecodeLicenseContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException("License file content is empty.");

            var trimmed = text.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
                return trimmed;

            const string prefix = "MEDYX-LIC-V1:";
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                trimmed = trimmed[prefix.Length..].Trim();

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(trimmed)).Trim();
                if (decoded.StartsWith("{", StringComparison.Ordinal))
                    return decoded;
            }
            catch
            {
                // fall through and throw canonical invalid format below
            }

            throw new InvalidDataException("License file format is invalid or not recognized.");
        }
    }

    internal sealed class PublicKeyFileDto
    {
        public string KeyId { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public int KeySize { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string ModulusHex { get; set; } = string.Empty;
        public string ExponentHex { get; set; } = string.Empty;
        public string VerificationKey { get; set; } = string.Empty;
    }

}

