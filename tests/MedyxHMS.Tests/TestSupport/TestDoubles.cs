using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MedyxHMS.Tests.TestSupport;

internal sealed class FakeSettingService : ISettingService
{
    public Dictionary<string, string?> Values { get; } = new();

    public Task<HospitalSettings> GetHospitalSettingsAsync() => Task.FromResult(new HospitalSettings
    {
        Name = "Test Hospital",
        Version = "1.0.0",
        DefaultLanguage = "en",
        SupportedLanguages = new List<string> { "en" },
        TimeZone = "UTC",
        Currency = "USD",
        DateFormat = "yyyy-MM-dd",
        EnableAuditLogging = true,
        EnableEmailNotifications = true,
        EnableSMSNotifications = false,
        FileUploadPath = "uploads/",
        MaxFileSizeMB = 10,
        AllowedFileTypes = new List<string> { ".pdf" }
    });

    public Task<FeatureToggles> GetFeatureTogglesAsync() => Task.FromResult(new FeatureToggles());

    public Task<string?> GetSettingValueAsync(string key)
    {
        Values.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task<bool> UpdateSettingAsync(string key, string value)
    {
        Values[key] = value;
        return Task.FromResult(true);
    }

    public Task<IEnumerable<Language>> GetSupportedLanguagesAsync() => Task.FromResult(Enumerable.Empty<Language>());
}

internal sealed class FakeEmailNotificationProvider : IEmailNotificationProvider
{
    public readonly List<(string To, string Subject)> Sent = new();

    public Task SendAsync(string toEmail, string subject, string body)
    {
        Sent.Add((toEmail, subject));
        return Task.CompletedTask;
    }
}

internal sealed class FakeSmsNotificationProvider : ISmsNotificationProvider
{
    public readonly List<string> SentTo = new();

    public Task SendAsync(string toPhone, string message)
    {
        SentTo.Add(toPhone);
        return Task.CompletedTask;
    }
}

internal sealed class FakePublicBookingNotificationService : IPublicBookingNotificationService
{
    public readonly List<int> ConfirmedRequestIds = new();

    public Task NotifyAppointmentConfirmedAsync(PublicAppointmentRequest request, string doctorDisplayName)
    {
        ConfirmedRequestIds.Add(request.Id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>();
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
    }
}
