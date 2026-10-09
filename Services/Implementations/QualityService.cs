using MedyxHMS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public sealed record StaffOption(string Id, string Name, string Department);

    /// <summary>Shared helpers for the quality module: numbering, document file storage and staff look-ups.</summary>
    public class QualityService
    {
        public static readonly string[] IncidentCategories =
            { "Patient safety", "Medication error", "Patient fall", "Infection control", "Equipment / device", "Security", "Patient complaint", "Staff injury", "Documentation", "Other" };
        public static readonly string[] Severities = { "Low", "Medium", "High", "Critical" };
        public static readonly string[] IncidentStatuses = { "Open", "Under investigation", "CAPA in progress", "Closed" };
        public static readonly string[] DocumentCategories = { "Policy", "Procedure (SOP)", "Work instruction", "Form / template", "Manual", "Guideline" };
        public static readonly string[] AuditStandards = { "ISO 9001", "ISO 27001", "ISO 15189", "NABH", "JCI", "Internal checklist" };
        public static readonly string[] FindingTypes = { "Major non-conformity", "Minor non-conformity", "Observation", "Opportunity for improvement" };
        public static readonly string[] TrainingCategories = { "Induction", "Fire safety", "Infection control", "BLS / CPR", "Data protection", "Equipment", "Clinical skills", "Quality / ISO", "Other" };
        public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg" };
        public const long MaxDocumentBytes = 20 * 1024 * 1024;
        public const string StaffRoles = AppRoles.Staff;
        public const string ManagerRoles = AppRoles.Managers;

        private readonly ApplicationDbContext _context;
        private readonly string _documentRoot;

        public QualityService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            // Outside wwwroot: controlled documents are only served through QualityDocuments/Download (signed-in staff).
            _documentRoot = Path.Combine(environment.ContentRootPath, "App_Data", "QualityDocuments");
        }

        public async Task<string> NextIncidentNumberAsync()
        {
            var prefix = $"INC-{DateTime.Now:yyyy}-";
            var last = await _context.QualityIncidents.IgnoreQueryFilters()
                .Where(i => i.IncidentNumber.StartsWith(prefix))
                .OrderByDescending(i => i.IncidentNumber)
                .Select(i => i.IncidentNumber)
                .FirstOrDefaultAsync();
            var next = last != null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
            return $"{prefix}{next:D4}";
        }

        public async Task<string> NextAuditNumberAsync()
        {
            var prefix = $"IA-{DateTime.Now:yyyy}-";
            var last = await _context.InternalAudits.IgnoreQueryFilters()
                .Where(a => a.AuditNumber.StartsWith(prefix))
                .OrderByDescending(a => a.AuditNumber)
                .Select(a => a.AuditNumber)
                .FirstOrDefaultAsync();
            var next = last != null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
            return $"{prefix}{next:D3}";
        }

        /// <summary>Validates and stores an uploaded document file; returns the stored name or an error message.</summary>
        public async Task<(string? StoredName, string? Error)> SaveDocumentFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (null, "Please choose the document file.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedDocumentExtensions.Contains(extension))
            {
                return (null, "Allowed file types: " + string.Join(", ", AllowedDocumentExtensions) + ".");
            }

            if (file.Length > MaxDocumentBytes)
            {
                return (null, "The file is larger than 20 MB.");
            }

            Directory.CreateDirectory(_documentRoot);
            var stored = $"{Guid.NewGuid():N}{extension}";
            await using var stream = File.Create(Path.Combine(_documentRoot, stored));
            await file.CopyToAsync(stream);
            return (stored, null);
        }

        public string? GetDocumentFilePath(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName) || storedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return null;
            }

            var path = Path.Combine(_documentRoot, storedName);
            return File.Exists(path) ? path : null;
        }

        /// <summary>Active staff logins (any role except Patient) for owner / trainee pickers.</summary>
        public async Task<List<StaffOption>> GetStaffOptionsAsync()
        {
            var staffUserIds = await (from ur in _context.Set<IdentityUserRole<string>>()
                                      join r in _context.Set<IdentityRole>() on ur.RoleId equals r.Id
                                      where r.Name != "Patient"
                                      select ur.UserId).Distinct().ToListAsync();

            var users = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive && staffUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName })
                .ToListAsync();

            var departments = await _context.Staff.AsNoTracking()
                .Where(s => staffUserIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Department })
                .ToDictionaryAsync(s => s.Id, s => s.Department ?? string.Empty);

            return users
                .Select(u => new StaffOption(u.Id, string.IsNullOrWhiteSpace($"{u.FirstName} {u.LastName}".Trim()) ? u.UserName ?? u.Id : $"{u.FirstName} {u.LastName}".Trim(), departments.GetValueOrDefault(u.Id) ?? string.Empty))
                .OrderBy(s => s.Name)
                .ToList();
        }
    }
}
