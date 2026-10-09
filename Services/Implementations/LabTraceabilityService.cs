using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MedyxHMS.Data;
using MedyxHMS.Models;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Specimen traceability (ISO 15189): accession numbers, chain-of-custody events and the electronic
    /// sign-off signature of lab results.
    /// </summary>
    public class LabTraceabilityService
    {
        /// <summary>Only users with this role can electronically sign off (authorise) results.</summary>
        public const string SignOffRole = "Pathologist";

        public static readonly string[] SampleTypes =
            { "Whole blood (EDTA)", "Serum", "Plasma (citrate)", "Plasma (fluoride)", "Urine", "Stool", "Swab", "Sputum", "CSF", "Tissue", "Other" };

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;

        public LabTraceabilityService(ApplicationDbContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        public static string DefaultSampleType(string? category) => (category ?? string.Empty).ToLowerInvariant() switch
        {
            var c when c.Contains("hemat") || c.Contains("haemat") => "Whole blood (EDTA)",
            var c when c.Contains("coag") => "Plasma (citrate)",
            var c when c.Contains("urin") => "Urine",
            var c when c.Contains("micro") => "Swab",
            var c when c.Contains("stool") => "Stool",
            _ => "Serum"
        };

        /// <summary>L + yyMMdd + 4-digit daily sequence, e.g. L2610040007 (Code 128 friendly).</summary>
        public async Task<string> NextAccessionNumberAsync()
        {
            var prefix = "L" + DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture);
            var last = await _context.LabResults
                .Where(r => r.AccessionNumber != null && r.AccessionNumber.StartsWith(prefix))
                .OrderByDescending(r => r.AccessionNumber)
                .Select(r => r.AccessionNumber)
                .FirstOrDefaultAsync();
            var next = last != null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
            return $"{prefix}{next:D4}";
        }

        public string CurrentUserName => _http.HttpContext?.User?.Identity?.Name ?? "system";

        /// <summary>Records a chain-of-custody event for the current user (saved with the next SaveChanges).</summary>
        public void AddEvent(LabResult result, string eventType, string details = "")
        {
            var user = _http.HttpContext?.User;
            _context.LabSampleEvents.Add(new LabSampleEvent
            {
                LabResult = result,
                EventType = eventType,
                OccurredAt = DateTime.Now,
                UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                UserName = user?.Identity?.Name ?? "system",
                Details = details.Length > 1000 ? details[..1000] : details
            });
        }

        /// <summary>SHA-256 over the authorised content, the signer and the signing time.</summary>
        public static string ComputeSignature(LabResult r, string signerUserId, DateTime signedAtUtc)
        {
            var content = string.Join("|", r.Id, r.AccessionNumber, r.OrderNumber, r.PatientId, r.LabTestId,
                r.ResultValue, r.Unit, r.NormalRange, r.Interpretation, signerUserId,
                signedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        }

        /// <summary>True when the stored signature still matches the result (nothing changed since sign-off).</summary>
        public static bool IsSignatureValid(LabResult r) =>
            r.SignedOffAt.HasValue && !string.IsNullOrEmpty(r.SignatureHash)
            && string.Equals(r.SignatureHash, ComputeSignature(r, r.SignedOffByUserId, r.SignedOffAt.Value), StringComparison.Ordinal);
    }
}
