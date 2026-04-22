using MedyxHMS.DTOs;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Purpose: Contains application code for AppController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class AppController : ControllerBase
    {
        private readonly ISettingService _settingService;

        public AppController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpPost]
        [Route("App/Index")]
        [Route("api/v1/app")]
        public async Task<IActionResult> Index()
        {
            if (!await IsMobileApiEnabledAsync())
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Mobile API is disabled." });
            }

            return Ok(await BuildV1ResponseAsync());
        }

        [AcceptVerbs("GET", "POST")]
        [Route("api/v2/app/config")]
        public async Task<IActionResult> Config()
        {
            if (!await IsMobileApiEnabledAsync())
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Mobile API is disabled." });
            }

            return Ok(await BuildV2ResponseAsync());
        }

        private async Task<bool> IsMobileApiEnabledAsync()
        {
            var featureToggles = await _settingService.GetFeatureTogglesAsync();
            return featureToggles.MobileAPI;
        }

        private async Task<MobileApiV1AppResponse> BuildV1ResponseAsync()
        {
            var hospitalSettings = await _settingService.GetHospitalSettingsAsync();
            var siteUrl = BuildSiteUrl();
            var apiBaseUrl = await GetMobileApiBaseUrlAsync("v1", siteUrl);

            return new MobileApiV1AppResponse
            {
                Url = apiBaseUrl,
                SiteUrl = siteUrl,
                AppLogo = await GetMobileLogoUrlAsync(siteUrl),
                AppPrimaryColorCode = await _settingService.GetSettingValueAsync("PublicSitePrimaryColor") ?? "#1a5276",
                AppSecondaryColorCode = await _settingService.GetSettingValueAsync("PublicSiteAccentColor") ?? "#2980b9",
                LangCode = hospitalSettings.DefaultLanguage
            };
        }

        private async Task<MobileApiV2ConfigResponse> BuildV2ResponseAsync()
        {
            var hospitalSettings = await _settingService.GetHospitalSettingsAsync();
            var featureToggles = await _settingService.GetFeatureTogglesAsync();
            var siteUrl = BuildSiteUrl();

            return new MobileApiV2ConfigResponse
            {
                BaseUrl = await GetMobileApiBaseUrlAsync("v2", siteUrl),
                SiteUrl = siteUrl,
                AppLogoUrl = await GetMobileLogoUrlAsync(siteUrl),
                PrimaryColor = await _settingService.GetSettingValueAsync("PublicSitePrimaryColor") ?? "#1a5276",
                SecondaryColor = await _settingService.GetSettingValueAsync("PublicSiteAccentColor") ?? "#2980b9",
                DefaultLanguage = hospitalSettings.DefaultLanguage,
                SupportedLanguages = hospitalSettings.SupportedLanguages,
                Capabilities = new MobileApiV2Capabilities
                {
                    PatientPortalEnabled = featureToggles.PatientPortal,
                    AppointmentSystemEnabled = featureToggles.AppointmentSystem,
                    BillingModuleEnabled = featureToggles.BillingModule,
                    PublicWebsiteEnabled = featureToggles.PublicWebsite,
                    MobileApiEnabled = featureToggles.MobileAPI
                }
            };
        }

        private string BuildSiteUrl()
        {
            var request = HttpContext?.Request;
            if (request == null || string.IsNullOrWhiteSpace(request.Host.Value))
            {
                return string.Empty;
            }

            return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
        }

        private async Task<string> GetMobileApiBaseUrlAsync(string version, string siteUrl)
        {
            var configured = await _settingService.GetSettingValueAsync("MobileApiBaseUrl");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var normalized = configured.TrimEnd('/');
                var versionSegment = $"/api/{version}";
                if (normalized.EndsWith(versionSegment, StringComparison.OrdinalIgnoreCase)
                    || normalized.EndsWith($"/{version}", StringComparison.OrdinalIgnoreCase))
                {
                    return normalized;
                }

                return $"{normalized}/{version}";
            }

            if (string.IsNullOrWhiteSpace(siteUrl))
            {
                return $"/api/{version}";
            }

            return $"{siteUrl}/api/{version}";
        }

        private async Task<string> GetMobileLogoUrlAsync(string siteUrl)
        {
            var configured = await _settingService.GetSettingValueAsync("MobileAppLogo");
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = await _settingService.GetSettingValueAsync("PublicSiteHomeHeroImage");
            }

            if (string.IsNullOrWhiteSpace(configured))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(configured, UriKind.Absolute, out _))
            {
                return configured;
            }

            var normalized = configured.StartsWith('/') ? configured : $"/{configured}";
            return string.IsNullOrWhiteSpace(siteUrl) ? normalized : $"{siteUrl}{normalized}";
        }
    }
}
