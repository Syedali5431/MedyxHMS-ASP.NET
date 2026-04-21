using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [AllowAnonymous]
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;
        private readonly IChatbotConsentService _consentService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, IChatbotConsentService consentService, 
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _consentService = consentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? sessionId = null)
        {
            if (!await _chatbotService.IsChatbotEnabledForUserAsync(User))
            {
                return Forbid();
            }

            // Check if authenticated user requires consent
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var requiresConsent = await _consentService.RequiresConsentRenewalAsync(userId);

                if (requiresConsent)
                {
                    // Redirect to consent modal/page
                    return RedirectToAction(nameof(RequestConsent));
                }
            }

            var vm = new ChatbotPageViewModel
            {
                SessionId = sessionId
            };

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    vm.History = (await _chatbotService.GetSessionMessagesAsync(sessionId, User, 30)).ToList();
                }
            }

            return View(vm);
        }

        /// <summary>
        /// Display consent request form/modal for authenticated users.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RequestConsent()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var terms = await _consentService.GetConsentTermsAsync("1.0");
            var currentConsent = await _consentService.GetCurrentConsentAsync(userId);

            var vm = new ChatbotConsentViewModel
            {
                ConsentTerms = terms,
                ConsentVersion = "1.0",
                IsRenewal = currentConsent != null && currentConsent.RevokedAtUtc.HasValue
            };

            return View(vm);
        }

        /// <summary>
        /// Accept consent terms.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AcceptConsent([FromForm] ChatbotConsentAcceptViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid consent submission." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                await _consentService.AcceptConsentAsync(
                    userId,
                    vm.ConsentedToAiProcessing,
                    vm.ConsentedToDataRetention,
                    vm.ConsentedToThirdPartyProcessing,
                    "1.0",
                    ipAddress,
                    userAgent
                );

                _logger.LogInformation($"User {userId} accepted chatbot consent");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting consent for user {userId}");
                return StatusCode(500, new { error = "Failed to record consent." });
            }
        }

        /// <summary>
        /// Reject consent terms (prevents chatbot use).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RejectConsent()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                await _consentService.RejectConsentAsync(userId, "1.0", ipAddress, userAgent);
                _logger.LogInformation($"User {userId} rejected chatbot consent");
                
                // Redirect to denied page or back to dashboard
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting consent for user {userId}");
                return StatusCode(500, new { error = "Failed to record rejection." });
            }
        }

        /// <summary>
        /// Get consent status and terms (for AJAX calls).
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetConsentStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasConsent = await _consentService.HasActiveConsentAsync(userId);
            var requiresRenewal = await _consentService.RequiresConsentRenewalAsync(userId);

            return Json(new
            {
                hasConsent,
                requiresRenewal,
                consentVersion = "1.0"
            });
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

            // Check chatbot enabled
            if (!await _chatbotService.IsChatbotEnabledForUserAsync(User))
            {
                TempData["ErrorMessage"] = "Chatbot access is disabled for your role.";
                return RedirectToAction("Index", "Home");
            }

            // Check consent for authenticated users
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var hasConsent = await _consentService.HasActiveConsentAsync(userId);
                
                if (!hasConsent)
                {
                    return RedirectToAction(nameof(RequestConsent));
                }
            }

            var response = await _chatbotService.AskAsync(User, vm.Prompt.Trim(), vm.SessionId);
            var history = User.Identity?.IsAuthenticated == true
                ? (await _chatbotService.GetSessionMessagesAsync(response.SessionId, User, 30)).ToList()
                : new List<Models.ChatMessage>();

            var resultModel = new ChatbotPageViewModel
            {
                SessionId = response.SessionId,
                Prompt = string.Empty,
                LastAnswer = response.Answer,
                EscalationSuggested = response.EscalationSuggested,
                ConfidenceScore = response.ConfidenceScore,
                EscalationId = response.EscalationId,
                LanguageCode = response.DetectedLanguage,
                DetectedCategory = response.DetectedCategory,
                Sources = response.Sources,
                History = history
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

            if (!await _chatbotService.IsChatbotEnabledForUserAsync(User))
            {
                return StatusCode(403, new { error = "Chatbot access is disabled for your role." });
            }

            // Check consent for authenticated users
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var hasConsent = await _consentService.HasActiveConsentAsync(userId);

                if (!hasConsent)
                {
                    return StatusCode(403, new { error = "Consent required to use chatbot.", requiresConsent = true });
                }
            }

            var response = await _chatbotService.AskAsync(User, vm.Prompt.Trim(), vm.SessionId, vm.LanguageCode);
            var history = User.Identity?.IsAuthenticated == true
                ? await _chatbotService.GetSessionMessagesAsync(response.SessionId, User, 30)
                : Array.Empty<Models.ChatMessage>();

            return Json(new
            {
                sessionId = response.SessionId,
                answer = response.Answer,
                escalationSuggested = response.EscalationSuggested,
                escalationId = response.EscalationId,
                detectedCategory = response.DetectedCategory,
                detectedLanguage = response.DetectedLanguage,
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
        [Authorize]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Escalate([FromForm] ChatbotEscalationRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid escalation request." });
            }

            var escalation = await _chatbotService.EscalateAsync(User, vm.SessionId, vm.MessageId, vm.Reason, vm.EscalationType);
            if (escalation == null)
            {
                return Forbid();
            }

            return Json(new
            {
                success = true,
                escalationId = escalation.Id,
                status = escalation.Status,
                targetContact = escalation.TargetContact
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MarkUnresolved([FromForm] ChatbotUnresolvedRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid unresolved request." });
            }

            var ok = await _chatbotService.MarkSessionUnresolvedAsync(User, vm.SessionId, vm.Reason);
            if (!ok)
            {
                return Forbid();
            }

            return Json(new { success = true });
        }
    }
}
