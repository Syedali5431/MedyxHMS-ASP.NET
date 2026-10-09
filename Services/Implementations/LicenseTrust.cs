// Purpose: The vendor signing keys this version of MedyxHMS accepts licences from.
namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Licences are accepted only when they are signed with a vendor key listed here. Each entry is the key's
    /// verification key (SHA-256 of the RSA public key, see LicenseCryptoUtility.ComputeVerificationKey).
    /// Public keys configured in Settings or found in MedyxHMS-Lic/current are used only when they match an entry,
    /// so a key pair made by anyone else cannot sign licences for this application.
    /// The private keys stay with the vendor (never in the repository). When the vendor key is replaced, add the
    /// new verification key here and remove keys that must no longer be accepted.
    /// </summary>
    internal static class LicenseTrust
    {
        private static readonly HashSet<string> TrustedVerificationKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            // Vendor key 30664C6F… created 2026-10-09 (replaces the earlier keys, whose private keys were exposed).
            "62483320DF14097ADD09029F065D73A234D1F59A66380C4902D85B151EB0C5CD",
        };

        public const string UntrustedKeyMessage =
            "This licence or public key is not from a MedyxHMS vendor key trusted by this version of the application. Ask the vendor for a new licence.";

        public static bool IsTrusted(string? verificationKey)
            => !string.IsNullOrWhiteSpace(verificationKey) && TrustedVerificationKeys.Contains(verificationKey.Trim());
    }
}
