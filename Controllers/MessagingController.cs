using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    // Staff only: the patient-portal role must not reach this staff area (it exposes other patients' data).
    [Authorize(Roles = AppRoles.Staff)]
    public class MessagingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagingController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>Display names ("First Last (username)") for the senders / recipients of the given messages.</summary>
        private async Task<Dictionary<string, string>> UserNamesAsync(IEnumerable<InternalMessage> messages)
        {
            var ids = messages.SelectMany(m => new[] { m.SenderId, m.RecipientId, m.ParentMessage?.SenderId })
                .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var users = await _context.Users.Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName }).ToListAsync();
            return users.ToDictionary(u => u.Id, u =>
            {
                var name = $"{u.FirstName} {u.LastName}".Trim();
                return string.IsNullOrEmpty(name) ? u.UserName ?? u.Id : $"{name} ({u.UserName})";
            });
        }

        // ── Inbox ─────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var uid = CurrentUserId;
            var messages = await _context.InternalMessages
                .Where(m => (m.RecipientId == uid || m.IsBroadcast) && !m.IsDeletedByRecipient)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
            ViewBag.UserNames = await UserNamesAsync(messages);
            return View(messages);
        }

        // ── Sent ──────────────────────────────────────────────────

        public async Task<IActionResult> Sent()
        {
            var uid = CurrentUserId;
            var messages = await _context.InternalMessages
                .Where(m => m.SenderId == uid && !m.IsDeletedBySender)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
            ViewBag.UserNames = await UserNamesAsync(messages);
            return View(messages);
        }

        // ── Compose ───────────────────────────────────────────────

        public async Task<IActionResult> Compose(int? replyToId = null)
        {
            ViewBag.Staff = await _context.Users
                .Where(u => u.Id != CurrentUserId)
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName, u.Email })
                .ToListAsync();

            InternalMessage model = new()
            {
                SentAt = DateTime.Now,
                ParentMessageId = replyToId
            };

            if (replyToId.HasValue)
            {
                var parent = await _context.InternalMessages.FindAsync(replyToId.Value);
                if (parent != null)
                    model.Subject = parent.Subject.StartsWith("Re: ", StringComparison.OrdinalIgnoreCase)
                        ? parent.Subject
                        : "Re: " + parent.Subject;
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(InternalMessage model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Staff = await _context.Users
                    .Where(u => u.Id != CurrentUserId)
                    .OrderBy(u => u.UserName)
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToListAsync();
                return View(model);
            }

            model.SenderId = CurrentUserId;
            model.SentAt = DateTime.Now;
            model.IsRead = false;

            _context.InternalMessages.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Message sent.";
            return RedirectToAction(nameof(Sent));
        }

        // ── Read ──────────────────────────────────────────────────

        public async Task<IActionResult> Read(int id)
        {
            var uid = CurrentUserId;
            var msg = await _context.InternalMessages
                .Include(m => m.ParentMessage)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (msg == null) return NotFound();

            // Only sender and recipient may read
            if (msg.SenderId != uid && msg.RecipientId != uid && !msg.IsBroadcast)
                return Forbid();

            if (msg.RecipientId == uid && !msg.IsRead)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            ViewBag.UserNames = await UserNamesAsync(new[] { msg });
            return View(msg);
        }

        // ── Delete ────────────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string folder)
        {
            var uid = CurrentUserId;
            var msg = await _context.InternalMessages.FindAsync(id);
            if (msg == null) return NotFound();

            if (msg.RecipientId == uid || msg.IsBroadcast) msg.IsDeletedByRecipient = true;
            if (msg.SenderId == uid) msg.IsDeletedBySender = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message deleted.";
            return RedirectToAction(folder == "sent" ? nameof(Sent) : nameof(Index));
        }

        // ── Broadcast (Admin/SuperAdmin) ───────────────────────────

        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Broadcast()
        {
            return View(new InternalMessage { IsBroadcast = true, SentAt = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Broadcast(InternalMessage model)
        {
            if (!ModelState.IsValid) return View(model);

            model.SenderId = CurrentUserId;
            model.SentAt = DateTime.Now;
            model.IsBroadcast = true;
            model.RecipientId = string.Empty;

            _context.InternalMessages.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Broadcast sent to all staff.";
            return RedirectToAction(nameof(Sent));
        }

        // ── Unread count (AJAX) ───────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var uid = CurrentUserId;
            var count = await _context.InternalMessages
                .CountAsync(m => (m.RecipientId == uid || m.IsBroadcast)
                              && !m.IsRead
                              && !m.IsDeletedByRecipient);
            return Json(new { count });
        }
    }
}
