namespace MedyxHMS.Services.Interfaces
{
    public interface IProfileImageService
    {
        Task<string?> UploadAsync(string userId, IFormFile file);
        Task<bool> DeleteAsync(string userId);
        string GetDisplayPath(string? profileImage);
    }
}