using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for StaffService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class StaffService : IStaffService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IFileService _fileService;

        public StaffService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            IFileService fileService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _fileService = fileService;
        }

        public async Task<IEnumerable<Staff>> GetAllStaffAsync()
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<Staff> GetStaffByIdAsync(string id)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Staff> GetStaffByEmployeeIdAsync(string employeeId)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .FirstOrDefaultAsync(s => s.User.EmployeeId == employeeId);
        }

        public async Task<Staff> CreateStaffAsync(Staff staff, string password, List<int> roleIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (staff.User == null || string.IsNullOrWhiteSpace(staff.User.UserName))
                {
                    throw new Exception("User name is required.");
                }

                // Create ApplicationUser first
                var normalizedUserName = _userManager.NormalizeName(staff.User.UserName);
                if (!string.IsNullOrWhiteSpace(normalizedUserName) &&
                    await _userManager.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName))
                {
                    throw new Exception("User name already exists.");
                }

                var user = new ApplicationUser
                {
                    Id = await GetNextNumericUserIdAsync(),
                    UserName = staff.User.UserName,
                    Email = staff.User.Email,
                    EmailConfirmed = true,
                    EmployeeId = staff.EmployeeId,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    PhoneNumber = staff.Phone,
                    IsActive = staff.IsActive,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Set the staff ID to match the user ID
                staff.Id = user.Id;
                staff.CreatedDate = DateTime.UtcNow;

                // Add staff to database
                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                // Assign roles
                if (roleIds != null && roleIds.Any())
                {
                    foreach (var roleId in roleIds)
                    {
                        var role = await _context.Roles.FindAsync(roleId);
                        if (role != null)
                        {
                            var staffRole = new StaffRole
                            {
                                StaffId = user.Id,
                                RoleId = roleId,
                                AssignedDate = DateTime.UtcNow,
                                AssignedBy = "System" // TODO: Get current user
                            };
                            _context.StaffRoles.Add(staffRole);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Log activity
                await _auditService.LogActivityAsync(user.Id, "STAFF_CREATED", "Staff", user.Id);

                return staff;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> GetNextNumericUserIdAsync()
        {
            var maxId = await _userManager.Users
                .Select(u => (int?)ConvertToNumericUserId(u.Id))
                .MaxAsync() ?? 0;

            return (maxId + 1).ToString();
        }

        private static int ConvertToNumericUserId(string? rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
                return 0;

            return int.TryParse(rawId, out var numericId) ? numericId : 0;
        }

        public async Task<Staff> UpdateStaffAsync(Staff staff, List<int> roleIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get existing staff
                var existingStaff = await _context.Staff
                    .Include(s => s.User)
                    .Include(s => s.StaffRoles)
                    .FirstOrDefaultAsync(s => s.Id == staff.Id);

                if (existingStaff == null)
                {
                    throw new Exception("Staff not found");
                }

                // Store old values for audit
                var oldValues = $"EmployeeId:{existingStaff.EmployeeId},FirstName:{existingStaff.FirstName},LastName:{existingStaff.LastName}";

                // Update user information
                existingStaff.User.EmployeeId = staff.EmployeeId;
                existingStaff.User.FirstName = staff.FirstName;
                existingStaff.User.LastName = staff.LastName;
                existingStaff.User.PhoneNumber = staff.Phone;
                existingStaff.User.IsActive = staff.IsActive;

                // Update staff information
                existingStaff.EmployeeId = staff.EmployeeId;
                existingStaff.FirstName = staff.FirstName;
                existingStaff.LastName = staff.LastName;
                existingStaff.Department = staff.Department;
                existingStaff.Designation = staff.Designation;
                existingStaff.DateOfJoining = staff.DateOfJoining;
                existingStaff.Salary = staff.Salary;
                existingStaff.Phone = staff.Phone;
                existingStaff.Address = staff.Address;
                existingStaff.IsActive = staff.IsActive;

                // Update roles - remove existing and add new ones
                var existingRoles = await _context.StaffRoles.Where(sr => sr.StaffId == staff.Id).ToListAsync();
                _context.StaffRoles.RemoveRange(existingRoles);

                if (roleIds != null && roleIds.Any())
                {
                    foreach (var roleId in roleIds)
                    {
                        var role = await _context.Roles.FindAsync(roleId);
                        if (role != null)
                        {
                            var staffRole = new StaffRole
                            {
                                StaffId = staff.Id,
                                RoleId = roleId,
                                AssignedDate = DateTime.UtcNow,
                                AssignedBy = "System" // TODO: Get current user
                            };
                            _context.StaffRoles.Add(staffRole);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log activity
                var newValues = $"EmployeeId:{staff.EmployeeId},FirstName:{staff.FirstName},LastName:{staff.LastName}";
                await _auditService.LogActivityAsync(staff.Id, "STAFF_UPDATED", "Staff", staff.Id, oldValues, newValues);

                return existingStaff;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteStaffAsync(string id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var staff = await _context.Staff
                    .Include(s => s.User)
                    .Include(s => s.StaffRoles)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (staff == null)
                {
                    return false;
                }

                // Remove role assignments
                var staffRoles = await _context.StaffRoles.Where(sr => sr.StaffId == id).ToListAsync();
                _context.StaffRoles.RemoveRange(staffRoles);

                // Remove staff record
                _context.Staff.Remove(staff);

                // Remove user account
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log activity
                await _auditService.LogActivityAsync(id, "STAFF_DELETED", "Staff", id);

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ActivateStaffAsync(string id)
        {
            var staff = await _context.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (staff == null)
            {
                return false;
            }

            staff.IsActive = true;
            staff.User.IsActive = true;

            await _context.SaveChangesAsync();

            // Log activity
            await _auditService.LogActivityAsync(id, "STAFF_ACTIVATED", "Staff", id);

            return true;
        }

        public async Task<bool> DeactivateStaffAsync(string id)
        {
            var staff = await _context.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (staff == null)
            {
                return false;
            }

            staff.IsActive = false;
            staff.User.IsActive = false;

            await _context.SaveChangesAsync();

            // Log activity
            await _auditService.LogActivityAsync(id, "STAFF_DEACTIVATED", "Staff", id);

            return true;
        }

        public async Task<IEnumerable<Staff>> SearchStaffAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllStaffAsync();
            }

            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .Where(s =>
                    s.FirstName.Contains(searchTerm) ||
                    s.LastName.Contains(searchTerm) ||
                    s.EmployeeId.Contains(searchTerm) ||
                    (s.User != null && s.User.Email.Contains(searchTerm)) ||
                    s.Department.Contains(searchTerm) ||
                    s.Designation.Contains(searchTerm))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffByDepartmentAsync(string department)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .Where(s => s.Department == department)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffByRoleAsync(string roleName)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .Where(s => s.StaffRoles.Any(sr => sr.Role.Name == roleName))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<bool> ChangeStaffPasswordAsync(string staffId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(staffId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _auditService.LogActivityAsync(staffId, "PASSWORD_CHANGED", "Staff", staffId);
                return true;
            }

            return false;
        }

        public async Task<bool> ResetStaffPasswordAsync(string staffId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(staffId);
            if (user == null)
            {
                return false;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                await _auditService.LogActivityAsync(staffId, "PASSWORD_RESET", "Staff", staffId);
                return true;
            }

            return false;
        }

        public async Task<bool> UpdateStaffProfileAsync(string staffId, string firstName, string lastName, string phone, string address, IFormFile profileImage)
        {
            var staff = await _context.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == staffId);
            if (staff == null)
            {
                return false;
            }

            // Handle profile image upload
            string? profileImagePath = null;
            if (profileImage != null && profileImage.Length > 0)
            {
                profileImagePath = await _fileService.UploadFileAsync(profileImage, "profiles");
            }

            // Update profile information
            staff.FirstName = firstName;
            staff.LastName = lastName;
            staff.Phone = phone;
            staff.Address = address;

            staff.User.FirstName = firstName;
            staff.User.LastName = lastName;
            staff.User.PhoneNumber = phone;

            if (profileImagePath != null)
            {
                // Delete old profile image if exists
                if (!string.IsNullOrEmpty(staff.User.ProfileImage))
                {
                    await _fileService.DeleteFileAsync(staff.User.ProfileImage);
                }
                staff.User.ProfileImage = profileImagePath;
            }

            await _context.SaveChangesAsync();

            // Log activity
            await _auditService.LogActivityAsync(staffId, "PROFILE_UPDATED", "Staff", staffId);

            return true;
        }

        public async Task<IEnumerable<string>> GetStaffPermissionsAsync(string staffId)
        {
            var permissions = await _context.StaffRoles
                .Include(sr => sr.Role)
                .ThenInclude(r => r.RoleFeatures)
                .ThenInclude(rf => rf.Feature)
                .Where(sr => sr.StaffId == staffId)
                .SelectMany(sr => sr.Role.RoleFeatures)
                .Where(rf => rf.CanView)
                .Select(rf => rf.Feature.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task<IEnumerable<string>> GetStaffRolesAsync(string staffId)
        {
            return await _context.StaffRoles
                .Include(sr => sr.Role)
                .Where(sr => sr.StaffId == staffId)
                .Select(sr => sr.Role.Name)
                .ToListAsync();
        }

        public async Task<bool> AssignRolesToStaffAsync(string staffId, List<int> roleIds)
        {
            // Remove existing roles
            var existingRoles = await _context.StaffRoles.Where(sr => sr.StaffId == staffId).ToListAsync();
            _context.StaffRoles.RemoveRange(existingRoles);

            // Add new roles
            foreach (var roleId in roleIds)
            {
                var role = await _context.Roles.FindAsync(roleId);
                if (role != null)
                {
                    var staffRole = new StaffRole
                    {
                        StaffId = staffId,
                        RoleId = roleId,
                        AssignedDate = DateTime.UtcNow,
                        AssignedBy = "System" // TODO: Get current user
                    };
                    _context.StaffRoles.Add(staffRole);
                }
            }

            await _context.SaveChangesAsync();

            // Log activity
            await _auditService.LogActivityAsync(staffId, "ROLES_ASSIGNED", "Staff", staffId);

            return true;
        }

        public async Task<bool> RemoveRolesFromStaffAsync(string staffId, List<int> roleIds)
        {
            foreach (var roleId in roleIds)
            {
                var staffRole = await _context.StaffRoles
                    .FirstOrDefaultAsync(sr => sr.StaffId == staffId && sr.RoleId == roleId);

                if (staffRole != null)
                {
                    _context.StaffRoles.Remove(staffRole);
                }
            }

            await _context.SaveChangesAsync();

            // Log activity
            await _auditService.LogActivityAsync(staffId, "ROLES_REMOVED", "Staff", staffId);

            return true;
        }

        public async Task<Dictionary<string, int>> GetStaffStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();

            stats["TotalStaff"] = await _context.Staff.CountAsync();
            stats["ActiveStaff"] = await _context.Staff.CountAsync(s => s.IsActive);
            stats["InactiveStaff"] = await _context.Staff.CountAsync(s => !s.IsActive);
            stats["NewStaffThisMonth"] = await _context.Staff.CountAsync(s =>
                s.CreatedDate.Month == DateTime.UtcNow.Month &&
                s.CreatedDate.Year == DateTime.UtcNow.Year);

            return stats;
        }

        public async Task<IEnumerable<Staff>> GetRecentStaffAsync(int count = 10)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .OrderByDescending(s => s.CreatedDate)
                .Take(count)
                .ToListAsync();
        }
    }
}
