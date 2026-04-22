using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for ChatbotDataCleanupService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ChatbotDataCleanupService : IChatbotDataCleanupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotDataCleanupService> _logger;

        public ChatbotDataCleanupService(ApplicationDbContext context, ILogger<ChatbotDataCleanupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ChatbotCleanupResult> CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
        {
            var retentionDays = await GetIntSettingAsync("ChatbotRetentionDays", 90, cancellationToken);
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retentionDays));

            var expiredSessionIds = await _context.ChatSessions
                .AsNoTracking()
                .Where(s => (s.EndedAtUtc ?? s.StartedAtUtc) < cutoff)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (expiredSessionIds.Count > 0)
            {
                var sessions = await _context.ChatSessions
                    .Where(s => expiredSessionIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                _context.ChatSessions.RemoveRange(sessions);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Chatbot retention removed {Count} expired sessions older than {Cutoff}", sessions.Count, cutoff);
            }

            return new ChatbotCleanupResult
            {
                Category = "ExpiredSessions",
                DeletedCount = expiredSessionIds.Count,
                ExecutedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<ChatbotCleanupResult> CleanupExpiredEventLogsAsync(CancellationToken cancellationToken)
        {
            var retentionDays = await GetIntSettingAsync("ChatbotEventLogRetentionDays", 30, cancellationToken);
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retentionDays));

            var expiredLogs = await _context.ChatbotEventLogs
                .Where(e => e.CreatedAtUtc < cutoff)
                .ToListAsync(cancellationToken);

            if (expiredLogs.Count > 0)
            {
                _context.ChatbotEventLogs.RemoveRange(expiredLogs);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Chatbot retention removed {Count} expired event logs older than {Cutoff}", expiredLogs.Count, cutoff);
            }

            return new ChatbotCleanupResult
            {
                Category = "ExpiredEventLogs",
                DeletedCount = expiredLogs.Count,
                ExecutedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<ChatbotCleanupResult> CleanupUnconsentedDataAsync(CancellationToken cancellationToken)
        {
            var enabled = await GetBoolSettingAsync("ChatbotDeleteUnconsentedData", true, cancellationToken);
            if (!enabled)
            {
                return new ChatbotCleanupResult
                {
                    Category = "UnconsentedData",
                    DeletedCount = 0,
                    ExecutedAtUtc = DateTime.UtcNow
                };
            }

            var revokedUserIds = await _context.ChatbotConsents
                .AsNoTracking()
                .Where(c => c.UserId != null && (!c.IsActive || c.RevokedAtUtc != null) && !c.ConsentedToDataRetention)
                .Select(c => c.UserId!)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (revokedUserIds.Count == 0)
            {
                return new ChatbotCleanupResult
                {
                    Category = "UnconsentedData",
                    DeletedCount = 0,
                    ExecutedAtUtc = DateTime.UtcNow
                };
            }

            var sessions = await _context.ChatSessions
                .Where(s => s.UserId != null && revokedUserIds.Contains(s.UserId))
                .ToListAsync(cancellationToken);

            if (sessions.Count > 0)
            {
                _context.ChatSessions.RemoveRange(sessions);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Chatbot retention removed {Count} sessions for revoked data-retention consent", sessions.Count);
            }

            return new ChatbotCleanupResult
            {
                Category = "UnconsentedData",
                DeletedCount = sessions.Count,
                ExecutedAtUtc = DateTime.UtcNow
            };
        }

        private async Task<int> GetIntSettingAsync(string key, int fallback, CancellationToken cancellationToken)
        {
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            return setting != null && int.TryParse(setting.Value, out var value) ? value : fallback;
        }

        private async Task<bool> GetBoolSettingAsync(string key, bool fallback, CancellationToken cancellationToken)
        {
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            return setting != null && bool.TryParse(setting.Value, out var value) ? value : fallback;
        }
    }
}
