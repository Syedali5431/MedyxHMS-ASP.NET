using System.Data;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Purpose: Contains application code for ConcurrentSessionService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ConcurrentSessionService : IConcurrentSessionService
    {
        private static readonly string[] ExemptRoles = { "SuperAdmin", "Admin", "Patient" };
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConcurrentSessionService> _logger;

        public ConcurrentSessionService(ApplicationDbContext context, ILogger<ConcurrentSessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ConcurrentLoginDecision> TryRegisterLoginAsync(string userId, string activeRole, string sessionId, string? ipAddress, string? userAgent)
        {
            try
            {
                var now = DateTime.UtcNow;
                var cutoff = now.Subtract(IdleTimeout);

                await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // Expire stale sessions first so concurrent counting is always based on fresh server-side state.
                var staleSessions = await _context.UserSessions
                    .Where(s => s.IsActive && s.LastActivityUtc < cutoff)
                    .ToListAsync();

                foreach (var stale in staleSessions)
                {
                    stale.IsActive = false;
                    stale.LogoutAtUtc = now;
                }

                var role = string.IsNullOrWhiteSpace(activeRole) ? "Unknown" : activeRole.Trim();

                if (IsExemptRole(role))
                {
                    await UpsertSessionAsync(userId, role, sessionId, now, ipAddress, userAgent);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    return new ConcurrentLoginDecision { IsAllowed = true };
                }

                var license = await _context.LicenseRecords
                    .Where(l => l.IsActive)
                    .OrderByDescending(l => l.CreatedAtUtc)
                    .FirstOrDefaultAsync();

                if (license == null || !license.IsSignatureValid || license.ExpiresAtUtc < now)
                {
                    await tx.RollbackAsync();
                    return new ConcurrentLoginDecision
                    {
                        IsAllowed = false,
                        DenyReason = "Valid signed license not found.",
                        ActiveUsers = 0,
                        MaxConcurrentUsers = 0
                    };
                }

                var maxUsers = license.MaxConcurrentUsers;
                if (maxUsers <= 0)
                {
                    await tx.RollbackAsync();
                    return new ConcurrentLoginDecision
                    {
                        IsAllowed = false,
                        DenyReason = "License concurrent-user limit is invalid.",
                        ActiveUsers = 0,
                        MaxConcurrentUsers = maxUsers
                    };
                }

                var activeSessionRows = _context.UserSessions
                    .Where(s => s.IsActive && s.LastActivityUtc >= cutoff)
                    .Where(s => s.ActiveRole != "SuperAdmin" && s.ActiveRole != "Admin" && s.ActiveRole != "Patient");

                var sameUserAlreadyCounted = await activeSessionRows.AnyAsync(s => s.UserId == userId);
                var activeUsers = await activeSessionRows
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync();

                if (!sameUserAlreadyCounted && activeUsers >= maxUsers)
                {
                    await tx.RollbackAsync();
                    return new ConcurrentLoginDecision
                    {
                        IsAllowed = false,
                        DenyReason = $"Concurrent user limit reached ({maxUsers}).",
                        ActiveUsers = activeUsers,
                        MaxConcurrentUsers = maxUsers
                    };
                }

                await UpsertSessionAsync(userId, role, sessionId, now, ipAddress, userAgent);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConcurrentLoginDecision
                {
                    IsAllowed = true,
                    ActiveUsers = sameUserAlreadyCounted ? activeUsers : activeUsers + 1,
                    MaxConcurrentUsers = maxUsers
                };
            }
            catch (SqlException ex) when (IsMissingUserSessionsTable(ex))
            {
                _logger.LogWarning(ex, "UserSessions table is missing. Concurrent-session tracking was bypassed for login {UserId}.", userId);
                return new ConcurrentLoginDecision { IsAllowed = true };
            }
        }

        public async Task EndSessionAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                var row = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
                if (row == null)
                    return;

                row.IsActive = false;
                row.LogoutAtUtc = DateTime.UtcNow;
                row.LastActivityUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (SqlException ex) when (IsMissingUserSessionsTable(ex))
            {
                _logger.LogWarning(ex, "UserSessions table is missing. EndSession was skipped for session {SessionId}.", sessionId);
            }
        }

        public async Task MarkActivityAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                var row = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
                if (row == null)
                    return;

                row.LastActivityUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (SqlException ex) when (IsMissingUserSessionsTable(ex))
            {
                _logger.LogWarning(ex, "UserSessions table is missing. MarkActivity was skipped for session {SessionId}.", sessionId);
            }
        }

        private async Task UpsertSessionAsync(string userId, string role, string sessionId, DateTime now, string? ipAddress, string? userAgent)
        {
            var existing = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (existing == null)
            {
                _context.UserSessions.Add(new UserSession
                {
                    UserId = userId,
                    ActiveRole = role,
                    SessionId = sessionId,
                    IpAddress = ipAddress,
                    UserAgent = Truncate(userAgent, 512),
                    LoginAtUtc = now,
                    LastActivityUtc = now,
                    IsActive = true
                });
                return;
            }

            existing.UserId = userId;
            existing.ActiveRole = role;
            existing.IpAddress = ipAddress;
            existing.UserAgent = Truncate(userAgent, 512);
            existing.LastActivityUtc = now;
            existing.IsActive = true;
            existing.LogoutAtUtc = null;
        }

        private static bool IsExemptRole(string role)
        {
            return ExemptRoles.Any(exempt => string.Equals(exempt, role, StringComparison.OrdinalIgnoreCase));
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static bool IsMissingUserSessionsTable(SqlException ex)
        {
            return ex.Number == 208 && ex.Message.Contains("UserSessions", StringComparison.OrdinalIgnoreCase);
        }
    }
}
