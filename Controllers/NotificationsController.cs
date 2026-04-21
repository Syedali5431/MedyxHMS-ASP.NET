using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ISystemNotificationService _notificationService;

        public NotificationsController(ISystemNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToAction("Login", "Account");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

            var success = await _notificationService.MarkAsReadAsync(id, userId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success });

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

            await _notificationService.MarkAllAsReadAsync(userId);
            TempData["SuccessMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

            await _notificationService.DeleteAsync(id, userId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true });

            TempData["SuccessMessage"] = "Notification deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
