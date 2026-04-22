using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Purpose: Contains application code for AuthorizationService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class AuthorizationService : MedyxHMS.Services.Interfaces.IAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthorizationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            // Check if user is SuperAdmin (has all permissions)
            var userRoles = await _context.StaffRoles
                .Include(sr => sr.Role)
                .Where(sr => sr.StaffId == userId)
                .Select(sr => sr.Role)
                .ToListAsync();

            if (userRoles.Any(r => r.Name == "SuperAdmin"))
                return true;

            // Check specific permission
            var hasPermission = await _context.StaffRoles
                .Include(sr => sr.Role)
                .ThenInclude(r => r.RoleFeatures)
                .ThenInclude(rf => rf.Feature)
                .Where(sr => sr.StaffId == userId)
                .SelectMany(sr => sr.Role.RoleFeatures)
                .AnyAsync(rf => rf.Feature.Name == permission && rf.CanView);

            return hasPermission;
        }

        public async Task<bool> HasAnyPermissionAsync(string userId, IEnumerable<string> permissions)
        {
            foreach (var permission in permissions)
            {
                if (await HasPermissionAsync(userId, permission))
                    return true;
            }
            return false;
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return new List<string>();

            // Check if user is SuperAdmin
            var userRoles = await _context.StaffRoles
                .Include(sr => sr.Role)
                .Where(sr => sr.StaffId == userId)
                .Select(sr => sr.Role)
                .ToListAsync();

            if (userRoles.Any(r => r.Name == "SuperAdmin"))
            {
                // Return all permissions for SuperAdmin
                return await _context.Features.Select(f => f.Name).ToListAsync();
            }

            // Return specific permissions
            var permissions = await _context.StaffRoles
                .Include(sr => sr.Role)
                .ThenInclude(r => r.RoleFeatures)
                .ThenInclude(rf => rf.Feature)
                .Where(sr => sr.StaffId == userId)
                .SelectMany(sr => sr.Role.RoleFeatures)
                .Where(rf => rf.CanView)
                .Select(rf => rf.Feature.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            return await _context.StaffRoles
                .Include(sr => sr.Role)
                .Where(sr => sr.StaffId == userId)
                .Select(sr => sr.Role.Name)
                .ToListAsync();
        }

        public async Task<bool> AssignRoleToUserAsync(string userId, int roleId)
        {
            // Check if assignment already exists
            var existingAssignment = await _context.StaffRoles
                .FirstOrDefaultAsync(sr => sr.StaffId == userId && sr.RoleId == roleId);

            if (existingAssignment != null)
                return true; // Already assigned

            var staffRole = new StaffRole
            {
                StaffId = userId,
                RoleId = roleId,
                AssignedDate = DateTime.UtcNow,
                AssignedBy = "System"
            };

            _context.StaffRoles.Add(staffRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveRoleFromUserAsync(string userId, int roleId)
        {
            var staffRole = await _context.StaffRoles
                .FirstOrDefaultAsync(sr => sr.StaffId == userId && sr.RoleId == roleId);

            if (staffRole == null)
                return false;

            _context.StaffRoles.Remove(staffRole);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
