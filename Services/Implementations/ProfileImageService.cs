using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class ProfileImageService : IProfileImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProfileImageService> _logger;

        private static readonly HashSet<string> AllowedExtensions = new(
            StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

        private const long MaxFileSize = 2 * 1024 * 1024;

        public ProfileImageService(IWebHostEnvironment env, ILogger<ProfileImageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<string?> UploadAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("File exceeds 2 MB limit.");

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Only JPG and PNG files are allowed.");

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profile");
            Directory.CreateDirectory(uploadsDir);

            await DeleteAsync(userId);

            var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("Profile image uploaded for user {UserId}", userId);
            return fileName;
        }

        public Task<bool> DeleteAsync(string userId)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profile");
            if (!Directory.Exists(uploadsDir)) return Task.FromResult(false);

            var existing = Directory.GetFiles(uploadsDir, $"{userId}_*");
            foreach (var f in existing) File.Delete(f);

            return Task.FromResult(existing.Length > 0);
        }

        public string GetDisplayPath(string? profileImage)
        {
            if (string.IsNullOrWhiteSpace(profileImage))
                return "/images/default-avatar.svg";
            return $"/uploads/profile/{profileImage}";
        }
    }
}