using MedyxHMS.Controllers;
using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MedyxHMS.MobileApi.Tests;

public class AppControllerTests
{
    [Fact]
    public async Task LegacyIndex_ReturnsCompatibilityPayload()
    {
        var controller = CreateController(new FakeSettingService());

        var result = await controller.Index();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<MobileApiV1AppResponse>(ok.Value);
        Assert.Equal("https://mobile.medyx.test/api/v1", payload.Url);
        Assert.Equal("https://localhost:5001", payload.SiteUrl);
        Assert.Equal("https://localhost:5001/uploads/mobile-logo.png", payload.AppLogo);
        Assert.Equal("#102030", payload.AppPrimaryColorCode);
        Assert.Equal("#405060", payload.AppSecondaryColorCode);
        Assert.Equal("en", payload.LangCode);
    }

    [Fact]
    public async Task Config_ReturnsServiceUnavailable_WhenMobileApiDisabled()
    {
        var controller = CreateController(new FakeSettingService(mobileApiEnabled: false));

        var result = await controller.Config();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task Config_ReturnsExpandedVersionedPayload()
    {
        var controller = CreateController(new FakeSettingService());

        var result = await controller.Config();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<MobileApiV2ConfigResponse>(ok.Value);
        Assert.Equal("v2", payload.ApiVersion);
        Assert.Equal("https://mobile.medyx.test/api/v2", payload.BaseUrl);
        Assert.True(payload.Capabilities.MobileApiEnabled);
        Assert.True(payload.Capabilities.PatientPortalEnabled);
        Assert.True(payload.Capabilities.AppointmentSystemEnabled);
        Assert.True(payload.Capabilities.BillingModuleEnabled);
    }

    private static AppController CreateController(ISettingService settingService)
    {
        var controller = new AppController(settingService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("localhost:5001");
        return controller;
    }

    private sealed class FakeSettingService : ISettingService
    {
        private readonly bool _mobileApiEnabled;
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MobileApiBaseUrl"] = "https://mobile.medyx.test/api",
            ["MobileAppLogo"] = "/uploads/mobile-logo.png",
            ["PublicSitePrimaryColor"] = "#102030",
            ["PublicSiteAccentColor"] = "#405060"
        };

        public FakeSettingService(bool mobileApiEnabled = true)
        {
            _mobileApiEnabled = mobileApiEnabled;
        }

        public Task<HospitalSettings> GetHospitalSettingsAsync()
        {
            return Task.FromResult(new HospitalSettings
            {
                Name = "Medyx Hospital",
                Version = "1.0.0",
                DefaultLanguage = "en",
                SupportedLanguages = new List<string> { "en", "es" },
                TimeZone = "UTC",
                Currency = "USD",
                DateFormat = "yyyy-MM-dd",
                EnableAuditLogging = true,
                EnableEmailNotifications = true,
                EnableSMSNotifications = false,
                FileUploadPath = "uploads/",
                MaxFileSizeMB = 10,
                AllowedFileTypes = new List<string> { ".png" }
            });
        }

        public Task<FeatureToggles> GetFeatureTogglesAsync()
        {
            return Task.FromResult(new FeatureToggles
            {
                MobileAPI = _mobileApiEnabled,
                PatientPortal = true,
                AppointmentSystem = true,
                BillingModule = true,
                PublicWebsite = true
            });
        }

        public Task<string?> GetSettingValueAsync(string key)
        {
            _values.TryGetValue(key, out var value);
            return Task.FromResult<string?>(value);
        }

        public Task<bool> UpdateSettingAsync(string key, string value)
        {
            _values[key] = value;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<Language>> GetSupportedLanguagesAsync()
        {
            IEnumerable<Language> languages = new[]
            {
                new Language { Code = "en", Name = "English", NativeName = "English", IsActive = true },
                new Language { Code = "es", Name = "Spanish", NativeName = "Español", IsActive = true }
            };

            return Task.FromResult(languages);
        }
    }
}