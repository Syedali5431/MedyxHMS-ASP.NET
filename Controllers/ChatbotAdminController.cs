using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for ChatbotAdminController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ChatbotAdminController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotAdminController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var settings = await _chatbotService.GetAdminSettingsAsync();
            var vm = new ChatbotAdminSettingsViewModel
            {
                Enabled = settings.Enabled,
                EnableEscalation = settings.EnableEscalation,
                EnableAppointmentGuidance = settings.EnableAppointmentGuidance,
                EnableBillingGuidance = settings.EnableBillingGuidance,
                EnableMultilingual = settings.EnableMultilingual,
                EnabledForPatients = settings.EnabledForPatients,
                EnabledForStaff = settings.EnabledForStaff,
                EnabledForAdmins = settings.EnabledForAdmins,
                Model = settings.Model,
                Temperature = settings.Temperature,
                MaxTokens = settings.MaxTokens,
                UnresolvedThreshold = settings.UnresolvedThreshold,
                HourlyUsageLimit = settings.HourlyUsageLimit,
                SupportedLanguagesCsv = settings.SupportedLanguagesCsv,
                DefaultLanguage = settings.DefaultLanguage,
                RetentionDays = settings.RetentionDays,
                EventLogRetentionDays = settings.EventLogRetentionDays,
                EnablePiiRedaction = settings.EnablePiiRedaction,
                RedactionLevel = settings.RedactionLevel,
                DeleteUnconsentedData = settings.DeleteUnconsentedData
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(ChatbotAdminSettingsViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var updated = await _chatbotService.UpdateAdminSettingsAsync(new Models.ChatbotAdminSettings
            {
                Enabled = vm.Enabled,
                EnableEscalation = vm.EnableEscalation,
                EnableAppointmentGuidance = vm.EnableAppointmentGuidance,
                EnableBillingGuidance = vm.EnableBillingGuidance,
                EnableMultilingual = vm.EnableMultilingual,
                EnabledForPatients = vm.EnabledForPatients,
                EnabledForStaff = vm.EnabledForStaff,
                EnabledForAdmins = vm.EnabledForAdmins,
                Model = vm.Model,
                Temperature = vm.Temperature,
                MaxTokens = vm.MaxTokens,
                UnresolvedThreshold = vm.UnresolvedThreshold,
                HourlyUsageLimit = vm.HourlyUsageLimit,
                SupportedLanguagesCsv = vm.SupportedLanguagesCsv,
                DefaultLanguage = vm.DefaultLanguage,
                RetentionDays = vm.RetentionDays,
                EventLogRetentionDays = vm.EventLogRetentionDays,
                EnablePiiRedaction = vm.EnablePiiRedaction,
                RedactionLevel = vm.RedactionLevel,
                DeleteUnconsentedData = vm.DeleteUnconsentedData
            }, userId);

            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "Chatbot settings updated."
                : "Unable to update chatbot settings.";

            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> Analytics(int days = 30)
        {
            var snapshot = await _chatbotService.GetAnalyticsAsync(days);
            return View(new ChatbotAdminAnalyticsViewModel
            {
                Days = days,
                Snapshot = snapshot
            });
        }

        [HttpGet]
        public async Task<IActionResult> Escalations(string status = "Pending")
        {
            var items = await _chatbotService.GetEscalationsAsync(status, 200);
            return View(new ChatbotEscalationsViewModel
            {
                Status = status,
                Items = items.ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveEscalation(long id, string targetContact)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var ok = await _chatbotService.ResolveEscalationAsync(id, targetContact, userId);

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Escalation resolved and handoff logged."
                : "Failed to resolve escalation.";

            return RedirectToAction(nameof(Escalations));
        }
    }
}
