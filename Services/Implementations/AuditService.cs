using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for AuditService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(string? userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null)
        {
            var auditLog = new AuditLog
            {
                UserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                Action = action,
                EntityName = entityName,
                EntityId = string.IsNullOrWhiteSpace(entityId) ? "N/A" : entityId,
                OldValues = oldValues ?? string.Empty,
                NewValues = newValues ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty,
                SessionId = _httpContextAccessor.HttpContext?.Session.Id ?? string.Empty
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            return await query
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetEntityAuditLogsAsync(string entityName, string entityId)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return "Unknown";

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                return ipAddress.Split(',')[0].Trim();
            }

            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                return ipAddress;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
