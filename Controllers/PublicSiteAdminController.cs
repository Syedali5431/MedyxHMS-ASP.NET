using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Purpose: Contains application code for PublicSiteAdminController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PublicSiteAdminController : Controller
    {
        private readonly ISettingService _settingService;
        private readonly IFileService _fileService;

        public PublicSiteAdminController(ISettingService settingService, IFileService fileService)
        {
            _settingService = settingService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var vm = new PublicSiteSettingsViewModel
            {
                PublicAddress = await _settingService.GetSettingValueAsync("PublicSiteAddress") ?? string.Empty,
                PublicPhone = await _settingService.GetSettingValueAsync("PublicSitePhone") ?? string.Empty,
                PublicEmail = await _settingService.GetSettingValueAsync("PublicSiteEmail") ?? string.Empty,
                PublicMapEmbedUrl = await _settingService.GetSettingValueAsync("PublicSiteMapEmbedUrl") ?? string.Empty,
                CareersContent = await _settingService.GetSettingValueAsync("PublicSiteCareersContent") ?? string.Empty,
                HomeTitle = await _settingService.GetSettingValueAsync("PublicSiteHomeTitle") ?? "Medyx Hospital",
                HomeTagline = await _settingService.GetSettingValueAsync("PublicSiteHomeTagline") ?? "Compassionate Care, Advanced Medicine",
                HomeDescription = await _settingService.GetSettingValueAsync("PublicSiteHomeDescription") ?? string.Empty,
                HomeFontFamily = await _settingService.GetSettingValueAsync("PublicSiteHomeFontFamily") ?? string.Empty,
                ContactDescription = await _settingService.GetSettingValueAsync("PublicSiteContactDescription") ?? string.Empty,
                ContactFontFamily = await _settingService.GetSettingValueAsync("PublicSiteContactFontFamily") ?? string.Empty,
                LocationDescription = await _settingService.GetSettingValueAsync("PublicSiteLocationDescription") ?? string.Empty,
                LocationFontFamily = await _settingService.GetSettingValueAsync("PublicSiteLocationFontFamily") ?? string.Empty,
                HomeHeroImage = await _settingService.GetSettingValueAsync("PublicSiteHomeHeroImage") ?? string.Empty,
                ContactHeroImage = await _settingService.GetSettingValueAsync("PublicSiteContactHeroImage") ?? string.Empty,
                LocationHeroImage = await _settingService.GetSettingValueAsync("PublicSiteLocationHeroImage") ?? string.Empty,
                PrimaryColor = await _settingService.GetSettingValueAsync("PublicSitePrimaryColor") ?? "#1a5276",
                AccentColor = await _settingService.GetSettingValueAsync("PublicSiteAccentColor") ?? "#2980b9",
                SurfaceColor = await _settingService.GetSettingValueAsync("PublicSiteSurfaceColor") ?? "#f4f8fb",
                ThemePreset = await _settingService.GetSettingValueAsync("PublicSiteThemePreset") ?? "Custom",
                HeadingStyle = await _settingService.GetSettingValueAsync("PublicSiteHeadingStyle") ?? "Normal",
                ButtonStyle = await _settingService.GetSettingValueAsync("PublicSiteButtonStyle") ?? "Rounded"
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(PublicSiteSettingsViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            await _settingService.UpdateSettingAsync("PublicSiteAddress", vm.PublicAddress ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSitePhone", vm.PublicPhone ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteEmail", vm.PublicEmail ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteMapEmbedUrl", vm.PublicMapEmbedUrl ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteCareersContent", vm.CareersContent ?? string.Empty);

            if (vm.HomeHeroImageFile != null)
            {
                vm.HomeHeroImage = await _fileService.UploadFileAsync(vm.HomeHeroImageFile, "public-site");
            }

            if (vm.ContactHeroImageFile != null)
            {
                vm.ContactHeroImage = await _fileService.UploadFileAsync(vm.ContactHeroImageFile, "public-site");
            }

            if (vm.LocationHeroImageFile != null)
            {
                vm.LocationHeroImage = await _fileService.UploadFileAsync(vm.LocationHeroImageFile, "public-site");
            }

            await _settingService.UpdateSettingAsync("PublicSiteHomeTitle", vm.HomeTitle ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteHomeTagline", vm.HomeTagline ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteHomeDescription", vm.HomeDescription ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteHomeFontFamily", vm.HomeFontFamily ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteContactDescription", vm.ContactDescription ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteContactFontFamily", vm.ContactFontFamily ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteLocationDescription", vm.LocationDescription ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteLocationFontFamily", vm.LocationFontFamily ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteHomeHeroImage", vm.HomeHeroImage ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteContactHeroImage", vm.ContactHeroImage ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSiteLocationHeroImage", vm.LocationHeroImage ?? string.Empty);
            await _settingService.UpdateSettingAsync("PublicSitePrimaryColor", vm.PrimaryColor ?? "#1a5276");
            await _settingService.UpdateSettingAsync("PublicSiteAccentColor", vm.AccentColor ?? "#2980b9");
            await _settingService.UpdateSettingAsync("PublicSiteSurfaceColor", vm.SurfaceColor ?? "#f4f8fb");
            await _settingService.UpdateSettingAsync("PublicSiteThemePreset", vm.ThemePreset ?? "Custom");
            await _settingService.UpdateSettingAsync("PublicSiteHeadingStyle", vm.HeadingStyle ?? "Normal");
            await _settingService.UpdateSettingAsync("PublicSiteButtonStyle", vm.ButtonStyle ?? "Rounded");

            TempData["SuccessMessage"] = "Public site settings saved.";
            return RedirectToAction(nameof(Settings));
        }
    }
}
