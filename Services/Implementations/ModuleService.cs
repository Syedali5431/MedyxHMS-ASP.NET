using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for ModuleService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ModuleService : IModuleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ModuleService> _logger;

        public ModuleService(ApplicationDbContext context, ILogger<ModuleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SystemModule>> GetAllModulesAsync()
        {
            return await _context.SystemModules
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.DisplayName)
                .ToListAsync();
        }

        public async Task<bool> IsModuleEnabledForUserAsync(string moduleKey, string userId, bool isSuperAdmin = false)
        {
            // SuperAdmin always has access to everything
            if (isSuperAdmin)
                return true;

            var module = await _context.SystemModules
                .Where(m => m.Key == moduleKey)
                .Select(m => new { m.Id, m.IsGloballyEnabled })
                .FirstOrDefaultAsync();

            if (module == null)
                return false;

            if (!module.IsGloballyEnabled)
                return false;

            // Check per-user override
            var access = await _context.UserModuleAccesses
                .Where(u => u.UserId == userId && u.ModuleId == module.Id)
                .Select(u => (bool?)u.IsEnabled)
                .FirstOrDefaultAsync();

            // No record â†’ inherits global (= true at this point)
            return access ?? true;
        }

        public async Task<Dictionary<string, bool>> GetUserModuleMapAsync(string userId, bool isSuperAdmin = false)
        {
            var modules = await _context.SystemModules
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            if (isSuperAdmin)
                return modules.ToDictionary(m => m.Key, _ => true);

            var userAccesses = await _context.UserModuleAccesses
                .Where(u => u.UserId == userId)
                .ToDictionaryAsync(u => u.ModuleId, u => u.IsEnabled);

            var result = new Dictionary<string, bool>(modules.Count);
            foreach (var m in modules)
            {
                if (!m.IsGloballyEnabled)
                {
                    result[m.Key] = false;
                    continue;
                }

                result[m.Key] = userAccesses.TryGetValue(m.Id, out var overrideEnabled)
                    ? overrideEnabled
                    : true;
            }

            return result;
        }

        public async Task<bool> SetGlobalModuleEnabledAsync(int moduleId, bool isEnabled, string performedByUserId)
        {
            var module = await _context.SystemModules.FindAsync(moduleId);
            if (module == null)
                return false;

            module.IsGloballyEnabled = isEnabled;
            module.UpdatedAtUtc = DateTime.UtcNow;
            module.UpdatedByUserId = performedByUserId;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Module {Key} global status set to {Enabled} by {UserId}",
                module.Key, isEnabled, performedByUserId);

            return true;
        }

        public async Task<bool> SetUserModuleAccessAsync(string userId, int moduleId, bool isEnabled, string performedByUserId)
        {
            // Guard: cannot enable a globally-disabled module for a specific user
            var module = await _context.SystemModules.FindAsync(moduleId);
            if (module == null)
                return false;

            if (!module.IsGloballyEnabled && isEnabled)
            {
                _logger.LogWarning("Attempt to enable globally-disabled module {Key} for user {UserId} denied",
                    module.Key, userId);
                return false;
            }

            var existing = await _context.UserModuleAccesses
                .FirstOrDefaultAsync(u => u.UserId == userId && u.ModuleId == moduleId);

            var now = DateTime.UtcNow;
            if (existing != null)
            {
                existing.IsEnabled = isEnabled;
                existing.UpdatedAtUtc = now;
                existing.UpdatedByUserId = performedByUserId;
            }
            else
            {
                _context.UserModuleAccesses.Add(new UserModuleAccess
                {
                    UserId = userId,
                    ModuleId = moduleId,
                    IsEnabled = isEnabled,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    UpdatedByUserId = performedByUserId
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<UserModuleAccessRow>> GetUserModuleAccessRowsAsync(string userId)
        {
            var modules = await _context.SystemModules
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.DisplayName)
                .ToListAsync();

            var userAccesses = await _context.UserModuleAccesses
                .Where(u => u.UserId == userId)
                .ToDictionaryAsync(u => u.ModuleId, u => u.IsEnabled);

            var rows = new List<UserModuleAccessRow>(modules.Count);
            foreach (var m in modules)
            {
                bool? userOverride = userAccesses.TryGetValue(m.Id, out var ov) ? ov : null;
                bool effective = m.IsGloballyEnabled && (userOverride ?? true);

                rows.Add(new UserModuleAccessRow
                {
                    ModuleId          = m.Id,
                    Key               = m.Key,
                    DisplayName       = m.DisplayName,
                    Icon              = m.Icon,
                    IsGloballyEnabled = m.IsGloballyEnabled,
                    UserOverride      = userOverride,
                    EffectivelyEnabled = effective,
                    SortOrder         = m.SortOrder
                });
            }

            return rows;
        }
    }
}
