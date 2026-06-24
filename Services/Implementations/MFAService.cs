using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace MedyxHMS.Services.Implementations
{
    public class MFAService : IMFAService
    {
        private readonly UserManager<MedyxHMS.Models.ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private const string AppName = "MedyxHMS";

        public MFAService(UserManager<MedyxHMS.Models.ApplicationUser> userManager, IAuditService auditService)
        {
            _userManager = userManager;
            _auditService = auditService;
        }

        public (string secretKey, string qrCodeUri) GenerateSetupInfo(string email)
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            var encoded = Base32Encode(bytes);
            var qrUri = $"otpauth://totp/{AppName}:{email}?secret={encoded}&issuer={AppName}";
            return (encoded, qrUri);
        }

        public async Task<string> BeginSetupAsync(string userId, string email)
        {
            var (secret, qrUri) = GenerateSetupInfo(email);
            var user = await _userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");
            user.MFATempSecret = secret;
            await _userManager.UpdateAsync(user);
            await _auditService.LogActivityAsync(userId, "MFA_SETUP_BEGIN", "User", userId);
            return qrUri;
        }

        public async Task<bool> CompleteSetupAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.MFATempSecret)) return false;
            if (!ValidateCode(user.MFATempSecret, code))
            {
                await _auditService.LogActivityAsync(userId, "MFA_SETUP_FAILED", "User", userId);
                return false;
            }
            user.MFASecretKey = user.MFATempSecret;
            user.MFATempSecret = null;
            user.MFAEnabled = true;
            await _userManager.UpdateAsync(user);
            await _auditService.LogActivityAsync(userId, "MFA_ENABLED", "User", userId);
            return true;
        }

        public async Task<bool> DisableAsync(string userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            if (!await _userManager.CheckPasswordAsync(user, password)) return false;
            user.MFAEnabled = false;
            user.MFASecretKey = null;
            user.MFATempSecret = null;
            user.MFARecoveryCodes = null;
            await _userManager.UpdateAsync(user);
            await _auditService.LogActivityAsync(userId, "MFA_DISABLED", "User", userId);
            return true;
        }

        public async Task<bool> TestCodeAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var secret = user.MFATempSecret ?? user.MFASecretKey;
            return !string.IsNullOrEmpty(secret) && ValidateCode(secret, code);
        }

        public async Task<bool> ValidateLoginMfaAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.MFAEnabled || string.IsNullOrEmpty(user.MFASecretKey)) return true;
            var valid = ValidateCode(user.MFASecretKey, code);
            if (!valid) await _auditService.LogActivityAsync(userId, "MFA_LOGIN_FAILED", "User", userId);
            return valid;
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || code.Length != 6) return false;
            var secretBytes = Base32Decode(secretKey);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            for (long offset = -1; offset <= 1; offset++)
                if (GenerateTotpCode(secretBytes, timestamp + offset) == code) return true;
            return false;
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            int buffer = 0, bitsLeft = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b; bitsLeft += 8;
                while (bitsLeft >= 5) { result.Append(alphabet[(buffer >> (bitsLeft - 5)) & 0x1F]); bitsLeft -= 5; }
            }
            if (bitsLeft > 0) result.Append(alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
            return result.ToString();
        }

        private static byte[] Base32Decode(string base32)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            base32 = base32.TrimEnd('=').ToUpperInvariant();
            var result = new List<byte>();
            int buffer = 0, bitsLeft = 0;
            foreach (var c in base32)
            {
                var val = alphabet.IndexOf(c);
                if (val < 0) continue;
                buffer = (buffer << 5) | val; bitsLeft += 5;
                if (bitsLeft >= 8) { result.Add((byte)(buffer >> (bitsLeft - 8))); bitsLeft -= 8; }
            }
            return result.ToArray();
        }

        private static string GenerateTotpCode(byte[] secret, long counter)
        {
            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
            using var hmac = new HMACSHA1(secret);
            var hash = hmac.ComputeHash(counterBytes);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24) | ((hash[offset + 1] & 0xFF) << 16) | ((hash[offset + 2] & 0xFF) << 8) | (hash[offset + 3] & 0xFF);
            return (binary % 1_000_000).ToString("D6");
        }
    }
}