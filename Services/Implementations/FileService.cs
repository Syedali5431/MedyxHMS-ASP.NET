using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadPath;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadPath = Path.Combine(_environment.WebRootPath, "uploads");

            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subDirectory = "")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("File type not allowed");

            // Validate file size (10MB limit)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
                throw new ArgumentException("File size exceeds 10MB limit");

            // Create subdirectory if specified
            var targetDirectory = string.IsNullOrEmpty(subDirectory)
                ? _uploadPath
                : Path.Combine(_uploadPath, subDirectory);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(targetDirectory, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path
            var relativePath = string.IsNullOrEmpty(subDirectory)
                ? $"uploads/{fileName}"
                : $"uploads/{subDirectory}/{fileName}";

            return relativePath;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
                return false;

            try
            {
                File.Delete(fullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task<bool> FileExistsAsync(string filePath)
        {
            var fullPath = GetFullPath(filePath);
            return Task.FromResult(File.Exists(fullPath));
        }

        public string GetFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            // Ensure the path starts with /
            if (!filePath.StartsWith("/"))
                filePath = "/" + filePath;

            return filePath;
        }

        private string GetFullPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            // Remove leading slash if present
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);

            return Path.Combine(_environment.WebRootPath, filePath);
        }
    }
}