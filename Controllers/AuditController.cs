using System.Security.Claims;
using System.Text.Json;
using MedyxHMS.Data;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for AuditController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AuditController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditController> _logger;
        private readonly IExportService _exportService;

        public AuditController(IAuditService auditService, ApplicationDbContext context, ILogger<AuditController> logger, IExportService exportService)
        {
            _exportService = exportService;
            _auditService = auditService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string entityType, string userId, string? export = null)
        {
            // Meta-audit: log who viewed audit logs
            var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            await _auditService.LogActivityAsync(currentUserId, "AUDIT_LOG_VIEWED", "AuditLog", "batch");
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddDays(-7);

            if (!endDate.HasValue)
                endDate = DateTime.Now;

            // A date picked in the filter is midnight: include the whole end day.
            var endOfRange = endDate.Value.TimeOfDay == TimeSpan.Zero ? endDate.Value.Date.AddDays(1).AddTicks(-1) : endDate.Value;
            var logs = (await _auditService.GetAuditLogsAsync(startDate, endOfRange, userId)).AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityType))
                logs = logs.Where(x => x.EntityName == entityType);

            var items = logs.OrderByDescending(x => x.Timestamp).ToList();

            // The "CSV" button on the page passes export=csv with the current filters.
            if (string.Equals(export, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var headers = new[] { "Timestamp", "User", "Action", "Entity", "Entity ID", "IP Address" };
                var rows = items.Select(x => (IReadOnlyList<string>)new[]
                {
                    x.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    x.User?.UserName ?? x.UserId ?? string.Empty,
                    x.Action ?? string.Empty,
                    x.EntityName ?? string.Empty,
                    x.EntityId ?? string.Empty,
                    x.IpAddress ?? string.Empty
                }).ToList();
                var bytes = _exportService.BuildCsv("Audit Logs", headers, rows);
                return File(bytes, "text/csv", $"audit_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }

            ViewData["EntityTypes"] = await _context.AuditLogs
                .Where(a => !string.IsNullOrEmpty(a.EntityName))
                .Select(a => a.EntityName)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var vm = new AuditLogIndexViewModel
            {
                AuditLogs = items,
                StartDate = startDate,
                EndDate = endDate,
                EntityType = entityType,
                TotalLogs = items.Count
            };

            return View(vm);
        }

        [Authorize(Policy = "Permission:*")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log == null)
                return NotFound();

            var vm = new AuditLogDetailsViewModel
            {
                AuditLog = log,
                OldValuesFormatted = FormatJson(log.OldValues),
                NewValuesFormatted = FormatJson(log.NewValues)
            };

            return View(vm);
        }

        [Authorize(Policy = "Permission:*")]
        [HttpGet]
        public async Task<IActionResult> UserActions(string userId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.UserActionLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(x => x.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(x => x.LoggedDate >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfRange = endDate.Value.TimeOfDay == TimeSpan.Zero ? endDate.Value.Date.AddDays(1).AddTicks(-1) : endDate.Value;
                query = query.Where(x => x.LoggedDate <= endOfRange);
            }

            var items = await query
                .OrderByDescending(x => x.LoggedDate)
                .Take(500)
                .ToListAsync();

            var vm = new UserActionLogViewModel
            {
                UserActionLogs = items,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(vm);
        }

        private string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "-";

            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Audit log field was not valid JSON");
                return json;
            }
        }
    }
}
