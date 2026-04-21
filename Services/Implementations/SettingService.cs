using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class SettingService : ISettingService
    {
        private readonly ApplicationDbContext _context;

        public SettingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HospitalSettings> GetHospitalSettingsAsync()
        {
            var settings = await _context.Settings
                .Where(s => s.Category == "Hospital")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new HospitalSettings
            {
                Name = settings.GetValueOrDefault("Name", "Medyx Hospital Management System"),
                Version = settings.GetValueOrDefault("Version", "1.0.0"),
                DefaultLanguage = settings.GetValueOrDefault("DefaultLanguage", "en"),
                SupportedLanguages = settings.GetValueOrDefault("SupportedLanguages", "en,es,fr,ar").Split(',').ToList(),
                TimeZone = settings.GetValueOrDefault("TimeZone", "UTC"),
                Currency = settings.GetValueOrDefault("Currency", "USD"),
                DateFormat = settings.GetValueOrDefault("DateFormat", "yyyy-MM-dd"),
                EnableAuditLogging = bool.Parse(settings.GetValueOrDefault("EnableAuditLogging", "true")),
                EnableEmailNotifications = bool.Parse(settings.GetValueOrDefault("EnableEmailNotifications", "true")),
                EnableSMSNotifications = bool.Parse(settings.GetValueOrDefault("EnableSMSNotifications", "false")),
                FileUploadPath = settings.GetValueOrDefault("FileUploadPath", "uploads/"),
                MaxFileSizeMB = int.Parse(settings.GetValueOrDefault("MaxFileSizeMB", "10")),
                AllowedFileTypes = settings.GetValueOrDefault("AllowedFileTypes", ".jpg,.jpeg,.png,.pdf,.doc,.docx").Split(',').ToList()
            };
        }

        public async Task<FeatureToggles> GetFeatureTogglesAsync()
        {
            var settings = await _context.Settings
                .Where(s => s.Category == "Features" || s.Key == "ChatbotEnabled")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new FeatureToggles
            {
                ChatbotEnabled = bool.Parse(settings.GetValueOrDefault("ChatbotEnabled", "true")),
                PatientPortal = bool.Parse(settings.GetValueOrDefault("PatientPortal", "true")),
                AppointmentSystem = bool.Parse(settings.GetValueOrDefault("AppointmentSystem", "true")),
                BillingModule = bool.Parse(settings.GetValueOrDefault("BillingModule", "true")),
                OPDModule = bool.Parse(settings.GetValueOrDefault("OPDModule", "true")),
                IPDModule = bool.Parse(settings.GetValueOrDefault("IPDModule", "true")),
                PharmacyModule = bool.Parse(settings.GetValueOrDefault("PharmacyModule", "true")),
                LabModule = bool.Parse(settings.GetValueOrDefault("LabModule", "true")),
                RadiologyModule = bool.Parse(settings.GetValueOrDefault("RadiologyModule", "true")),
                HRModule = bool.Parse(settings.GetValueOrDefault("HRModule", "false")),
                PayrollModule = bool.Parse(settings.GetValueOrDefault("PayrollModule", "false")),
                InventoryModule = bool.Parse(settings.GetValueOrDefault("InventoryModule", "false")),
                ReportsModule = bool.Parse(settings.GetValueOrDefault("ReportsModule", "true")),
                PublicWebsite = bool.Parse(settings.GetValueOrDefault("PublicWebsite", "false")),
                MobileAPI = bool.Parse(settings.GetValueOrDefault("MobileAPI", "false"))
            };
        }

        public async Task<string?> GetSettingValueAsync(string key)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            return setting?.Value;
        }

        public async Task<bool> UpdateSettingAsync(string key, string value)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value,
                    Type = "string",
                    Category = "General",
                    IsSystem = false,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Language>> GetSupportedLanguagesAsync()
        {
            return await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();
        }
    }
}