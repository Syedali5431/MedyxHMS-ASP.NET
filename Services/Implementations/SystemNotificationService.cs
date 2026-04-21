using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class SystemNotificationService : ISystemNotificationService
    {
        private readonly ApplicationDbContext _context;

        public SystemNotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateForUserAsync(string userId, string title, string message, string type, string relatedEntityType, string relatedEntityId, int? patientId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            _context.SystemNotifications.Add(new SystemNotification
            {
                UserId = userId,
                PatientId = patientId,
                Title = title ?? string.Empty,
                Message = message ?? string.Empty,
                Type = string.IsNullOrWhiteSpace(type) ? "General" : type,
                RelatedEntityType = relatedEntityType ?? string.Empty,
                RelatedEntityId = relatedEntityId ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();
        }

        public async Task<int> NotifyRolesAsync(IEnumerable<string> roles, string title, string message, string type, string relatedEntityType, string relatedEntityId)
        {
            var normalizedRoles = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (normalizedRoles.Count == 0)
                return 0;

            var roleMap = await _context.Set<IdentityRole>()
                .Where(r => r.Name != null && normalizedRoles.Contains(r.Name.ToUpper()))
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            if (roleMap.Count == 0)
                return 0;

            var roleIds = roleMap.Select(r => r.Id).ToHashSet();
            var userIds = await _context.Set<IdentityUserRole<string>>()
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (userIds.Count == 0)
                return 0;

            var notifications = userIds.Select(userId => new SystemNotification
            {
                UserId = userId,
                Title = title ?? string.Empty,
                Message = message ?? string.Empty,
                Type = string.IsNullOrWhiteSpace(type) ? "General" : type,
                RelatedEntityType = relatedEntityType ?? string.Empty,
                RelatedEntityId = relatedEntityId ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            });

            _context.SystemNotifications.AddRange(notifications);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> NotifyAllNonPatientsAsync(string title, string message, string type, string relatedEntityType, string relatedEntityId)
        {
            var patientRoleId = await _context.Set<IdentityRole>()
                .Where(r => r.Name == "Patient")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var patientUserIds = string.IsNullOrWhiteSpace(patientRoleId)
                ? new HashSet<string>()
                : (await _context.Set<IdentityUserRole<string>>()
                    .Where(ur => ur.RoleId == patientRoleId)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();

            var userIds = await _context.Users
                .Where(u => u.IsActive && !patientUserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            var notifications = userIds.Select(userId => new SystemNotification
            {
                UserId = userId,
                Title = title ?? string.Empty,
                Message = message ?? string.Empty,
                Type = string.IsNullOrWhiteSpace(type) ? "General" : type,
                RelatedEntityType = relatedEntityType ?? string.Empty,
                RelatedEntityId = relatedEntityId ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            });

            _context.SystemNotifications.AddRange(notifications);
            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SystemNotification>> GetUserNotificationsAsync(string userId, int take = 50)
        {
            return await _context.SystemNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _context.SystemNotifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(long notificationId, string userId)
        {
            var notification = await _context.SystemNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> DeleteAsync(long notificationId, string userId)
        {
            var notification = await _context.SystemNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            _context.SystemNotifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.SystemNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = DateTime.UtcNow;
            }

            if (notifications.Count == 0)
                return 0;

            await _context.SaveChangesAsync();
            return notifications.Count;
        }
    }
}
