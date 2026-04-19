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
                ConfidenceScore = response.ConfidenceScore,
                Sources = response.Sources,
                History = (await _chatbotService.GetSessionMessagesAsync(response.SessionId, User, 30)).ToList()
            };

            return View("Index", resultModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskJson([FromForm] ChatbotAskRequestViewModel vm)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.Prompt))
            {
                return BadRequest(new { error = "Please enter a message for the assistant." });
            }

            var response = await _chatbotService.AskAsync(User, vm.Prompt.Trim(), vm.SessionId);
            var history = await _chatbotService.GetSessionMessagesAsync(response.SessionId, User, 30);

            return Json(new
            {
                sessionId = response.SessionId,
                answer = response.Answer,
                escalationSuggested = response.EscalationSuggested,
                confidenceScore = response.ConfidenceScore,
                providerModel = response.ProviderModel,
                sources = response.Sources.Select(s => new
                {
                    sourceType = s.SourceType,
                    sourceName = s.SourceName,
                    sourcePath = s.SourcePath,
                    excerpt = s.Excerpt
                }),
                history = history.Select(m => new
                {
                    id = m.Id,
                    senderType = m.SenderType,
                    content = m.Content,
                    createdAtUtc = m.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss")
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback([FromForm] ChatbotFeedbackRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid feedback request." });
            }

            var ok = await _chatbotService.SubmitFeedbackAsync(User, vm.SessionId, vm.MessageId, vm.FeedbackType, vm.Comment);
            if (!ok)
            {
                return Forbid();
            }

            return Json(new { success = true });
        }
    }
}
