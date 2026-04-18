using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? sessionId = null)
        {
            var vm = new ChatbotPageViewModel
            {
                SessionId = sessionId
            };

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                vm.History = (await _chatbotService.GetSessionMessagesAsync(sessionId, User, 30)).ToList();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask(ChatbotPageViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Prompt))
            {
                TempData["ErrorMessage"] = "Please enter a message for the assistant.";
                return RedirectToAction(nameof(Index), new { sessionId = vm.SessionId });
            }

            var response = await _chatbotService.AskAsync(User, vm.Prompt.Trim(), vm.SessionId);

            var resultModel = new ChatbotPageViewModel
            {
                SessionId = response.SessionId,
                Prompt = string.Empty,
                LastAnswer = response.Answer,
                EscalationSuggested = response.EscalationSuggested,
                History = (await _chatbotService.GetSessionMessagesAsync(response.SessionId, User, 30)).ToList()
            };

            return View("Index", resultModel);
        }
    }
}
