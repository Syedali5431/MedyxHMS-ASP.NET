using System.Security.Cryptography;
using MedyxHMS.Models;

// Purpose: Stores patient documents outside wwwroot (App_Data/PatientDocuments) after checking type, size and
// file signature, and records a SHA-256 fingerprint of each file.
namespace MedyxHMS.Services.Implementations
{
    public class PatientDocumentService
    {
        public static readonly string[] StaffCategories =
            { "Lab report", "Imaging / radiology report", "Discharge summary", "Referral letter", "Prescription", "Consent form", "Insurance / claim", "ID / registration", "Clinical photo", "Other" };
        public static readonly string[] PatientCategories =
            { "Previous medical report", "Lab report", "Imaging / radiology report", "Prescription", "Insurance / claim", "ID / registration", "Other" };
        public const long MaxBytes = 10 * 1024 * 1024;
        public const string AllowedList = ".pdf, .jpg, .jpeg, .png, .docx";

        private static readonly Dictionary<string, (string ContentType, byte[] Signature)> Allowed = new()
        {
            [".pdf"] = ("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
        };

        private readonly string _root;

        public PatientDocumentService(IWebHostEnvironment environment)
        {
            _root = Path.Combine(environment.ContentRootPath, "App_Data", "PatientDocuments");
        }

        /// <summary>PDF and images open in the browser; other types are downloaded.</summary>
        public static bool ShowInline(string contentType) => contentType is "application/pdf" or "image/jpeg" or "image/png";

        public async Task<(PatientDocument? File, string? Error)> SaveAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return (null, "Choose the file to upload.");
            if (file.Length > MaxBytes) return (null, "The file is larger than 10 MB.");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Allowed.TryGetValue(extension, out var type)) return (null, $"Allowed file types: {AllowedList}.");

            // The content must really be of that type (e.g. no web page renamed to .pdf).
            var header = new byte[type.Signature.Length];
            await using (var probe = file.OpenReadStream())
            {
                var read = await probe.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
                if (read < header.Length || !header.AsSpan().SequenceEqual(type.Signature))
                    return (null, $"The file content does not match its type ({extension}).");
            }

            Directory.CreateDirectory(_root);
            var stored = $"{Guid.NewGuid():N}{extension}";
            using var sha = SHA256.Create();
            await using (var source = file.OpenReadStream())
            await using (var target = File.Create(Path.Combine(_root, stored)))
            {
                var buffer = new byte[81920];
                int count;
                while ((count = await source.ReadAsync(buffer)) > 0)
                {
                    sha.TransformBlock(buffer, 0, count, null, 0);
                    await target.WriteAsync(buffer.AsMemory(0, count));
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            }

            return (new PatientDocument
            {
                OriginalFileName = SafeFileName(file.FileName),
                StoredFileName = stored,
                ContentType = type.ContentType,
                SizeBytes = file.Length,
                Sha256 = Convert.ToHexString(sha.Hash!).ToLowerInvariant()
            }, null);
        }

        public string? GetPath(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName) || storedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return null;
            var path = Path.Combine(_root, storedName);
            return File.Exists(path) ? path : null;
        }

        public static string SafeFileName(string? name)
        {
            var fileName = Path.GetFileName(name ?? string.Empty);
            foreach (var c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
            fileName = fileName.Replace('"', '_').Trim();
            if (fileName.Length > 150) fileName = fileName[..100] + fileName[^Math.Min(50, fileName.Length - 100)..];
            return string.IsNullOrWhiteSpace(fileName) ? "document" : fileName;
        }

        public static string FormatSize(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / 1024.0 / 1024.0:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";
    }
}
