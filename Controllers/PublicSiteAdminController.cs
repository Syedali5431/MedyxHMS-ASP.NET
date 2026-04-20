using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PublicSiteAdminController : Controller
    {
        private readonly ISettingService _settingService;

        public PublicSiteAdminController(ISettingService settingService)
        {
            _settingService = settingService;
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
                CareersContent = await _settingService.GetSettingValueAsync("PublicSiteCareersContent") ?? string.Empty
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

            TempData["SuccessMessage"] = "Public site settings saved.";
            return RedirectToAction(nameof(Settings));
        }
    }
}
