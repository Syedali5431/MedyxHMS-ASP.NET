using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedyxHMS.Models;

// Purpose: Contains application code for LicenseCryptoUtility and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    internal static class LicenseCryptoUtility
    {
        public static string BuildCanonicalPayloadJson(LicensePayload payload)
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

        public static string ComputeVerificationKey(string modulusHex, string exponentHex)
        {
            var normalizedModulus = NormalizeHex(modulusHex);
            var normalizedExponent = NormalizeHex(exponentHex);
            var material = $"MEDYXHMS-VERIFY|{normalizedModulus}|{normalizedExponent}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
            return BytesToHex(hash);
        }

        public static bool VerifyRsaSha256(string canonicalPayloadJson, string signatureHex, string modulusHex, string exponentHex)
        {
            var data = Encoding.UTF8.GetBytes(canonicalPayloadJson);
            var signature = HexToBytes(signatureHex);

            using var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = HexToBytes(modulusHex),
                Exponent = HexToBytes(exponentHex)
            });
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public static string Sha256Hex(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = SHA256.HashData(bytes);
            return BytesToHex(hash);
        }

        public static string BytesToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes);
        }

        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex) || hex.Length % 2 != 0)
                throw new InvalidDataException("Signature hex is not valid.");

            try
            {
                return Convert.FromHexString(hex);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Signature hex is malformed.", ex);
            }
        }

        private static string NormalizeHex(string hex)
        {
            var cleaned = new string((hex ?? string.Empty)
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
                .Trim();

            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[2..];

            return cleaned.ToUpperInvariant();
        }
    }
}
