namespace MedyxHMS.Services.Interfaces
{
    public interface IMFAService
    {
        (string secretKey, string qrCodeUri) GenerateSetupInfo(string email);
        bool ValidateCode(string secretKey, string code);
        Task<string> BeginSetupAsync(string userId, string email);
        Task<bool> CompleteSetupAsync(string userId, string code);
        Task<bool> DisableAsync(string userId, string password);
        Task<bool> TestCodeAsync(string userId, string code);
        Task<bool> ValidateLoginMfaAsync(string userId, string code);
        Task<List<string>> GenerateRecoveryCodesAsync(string userId);
        Task<bool> ValidateRecoveryCodeAsync(string userId, string code);
    }
}