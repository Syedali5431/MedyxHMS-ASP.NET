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
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActivityAsync(string? userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null)
        {
            var actor = string.IsNullOrWhiteSpace(userId) ? null : userId;
            if (actor != null && !await _context.Users.AsNoTracking().AnyAsync(u => u.Id == actor))
            {
                // Some callers pass the affected record's id (e.g. a staff member without a login): record the
                // signed-in user instead, or no user, so the entry is kept rather than rejected by the foreign key.
                var current = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                actor = !string.IsNullOrEmpty(current) && await _context.Users.AsNoTracking().AnyAsync(u => u.Id == current) ? current : null;
            }

            var auditLog = new AuditLog
            {
                UserId = actor,
                Action = action,
                EntityName = entityName,
                EntityId = string.IsNullOrWhiteSpace(entityId) ? "N/A" : entityId,
                OldValues = oldValues ?? string.Empty,
                NewValues = newValues ?? string.Empty,
                Timestamp = DateTime.Now,
                IpAddress = GetClientIpAddress(),
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty,
                SessionId = _httpContextAccessor.HttpContext?.Session.Id ?? string.Empty
            };

            _context.AuditLogs.Add(auditLog);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.Entries.Count > 0 && ex.Entries.All(e => e.Entity is AuditLog))
            {
                // Only the audit row itself failed (e.g. it references a user id that does not exist).
                // An audit write must never break the business action that triggered it.
                _context.Entry(auditLog).State = EntityState.Detached;
                _logger.LogError(ex, "Audit log write failed for {Action} on {Entity} {EntityId}", action, entityName, entityId);
            }
            catch
            {
                // Another pending change failed: keep the original behaviour (re-throw) but do not
                // leave the audit row queued for the next SaveChanges.
                _context.Entry(auditLog).State = EntityState.Detached;
                throw;
            }
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
