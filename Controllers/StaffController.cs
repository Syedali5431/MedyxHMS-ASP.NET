using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Purpose: Contains application code for StaffController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly IStaffService _staffService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly MedyxHMS.Services.Interfaces.IAuthorizationService _authorizationService;

        public StaffController(
            IStaffService staffService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAuditService auditService,
            MedyxHMS.Services.Interfaces.IAuthorizationService authorizationService)
        {
            _staffService = staffService;
            _userManager = userManager;
            _context = context;
            _auditService = auditService;
            _authorizationService = authorizationService;
        }

        // GET: Staff
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Index(string searchTerm, string departmentFilter, string roleFilter, bool? isActiveFilter, int page = 1, int pageSize = 25)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check permissions
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var viewModel = new StaffIndexViewModel
            {
                SearchTerm = searchTerm,
                DepartmentFilter = departmentFilter,
                RoleFilter = roleFilter,
                IsActiveFilter = isActiveFilter,
                CurrentPage = page,
                PageSize = pageSize
            };

            // Get all staff
            var allStaff = await _staffService.GetAllStaffAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allStaff = allStaff.Where(s =>
                    s.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.EmployeeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.User.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(departmentFilter))
            {
                allStaff = allStaff.Where(s => s.Department == departmentFilter);
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                allStaff = allStaff.Where(s => s.StaffRoles.Any(sr => sr.Role.Name == roleFilter));
            }

            if (isActiveFilter.HasValue)
            {
                allStaff = allStaff.Where(s => s.IsActive == isActiveFilter.Value);
            }

            // Convert to DTOs
            viewModel.Staff = allStaff.Select(s => new StaffDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.User.Email,
                Phone = s.Phone,
                Department = s.Department,
                Designation = s.Designation,
                DateOfJoining = s.DateOfJoining,
                Salary = s.Salary,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                LastLoginDate = s.User.LastLoginDate,
                Roles = s.StaffRoles.Select(sr => sr.Role.Name).ToList(),
                Permissions = new List<string>() // Will be populated if needed
            }).ToList();

            // Populate dropdown options
            viewModel.DepartmentOptions = (await _staffService.GetAllStaffAsync())
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            viewModel.RoleOptions = await _context.Roles
                .Select(r => r.Name)
                .OrderBy(r => r)
                .ToListAsync();

            // Statistics
            var stats = await _staffService.GetStaffStatisticsAsync();
            viewModel.TotalStaff = stats["TotalStaff"];
            viewModel.ActiveStaff = stats["ActiveStaff"];
            viewModel.StaffThisMonth = stats["NewStaffThisMonth"];

            // Pagination
            viewModel.TotalRecords = viewModel.Staff.Count;
            viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalRecords / (double)pageSize);

            // Apply pagination
            viewModel.Staff = viewModel.Staff
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(viewModel);
        }

        // GET: Staff/Details/5
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var viewModel = new StaffDetailsViewModel
            {
                Staff = new StaffDto
                {
                    Id = staff.Id,
                    EmployeeId = staff.EmployeeId,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Email = staff.User.Email,
                    Phone = staff.Phone,
                    Department = staff.Department,
                    Designation = staff.Designation,
                    DateOfJoining = staff.DateOfJoining,
                    Salary = staff.Salary,
                    Address = staff.Address,
                    IsActive = staff.IsActive,
                    CreatedDate = staff.CreatedDate,
                    LastLoginDate = staff.User.LastLoginDate,
                    Roles = staff.StaffRoles.Select(sr => sr.Role.Name).ToList(),
                    Permissions = (await _staffService.GetStaffPermissionsAsync(id)).ToList()
                },
                Roles = staff.StaffRoles.Select(sr => new StaffRoleDto
                {
                    RoleId = sr.RoleId,
                    RoleName = sr.Role.Name,
                    RoleDescription = sr.Role.Description,
                    AssignedDate = sr.AssignedDate,
                    AssignedBy = sr.AssignedBy,
                    IsAssigned = true
                }).ToList(),
                RecentActivity = await _context.AuditLogs
                    .Where(a => a.UserId == id)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync()
            };

            // Get related data counts (simplified)
            viewModel.TotalPatients = 0; // Would need to implement based on staff role
            viewModel.TotalAppointments = 0;
            viewModel.TotalBills = 0;

            return View(viewModel);
        }

        // GET: Staff/Create
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var viewModel = new StaffCreateViewModel();

            // Load available roles
            var roles = await _context.Roles.ToListAsync();
            viewModel.AvailableRoles = roles.Select(r => new RoleSelectionViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSelected = false
            }).ToList();

            return View(viewModel);
        }

        // POST: Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Create(StaffCreateViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if employee ID or email already exists
                    var existingStaff = await _staffService.GetStaffByEmployeeIdAsync(viewModel.Staff.EmployeeId);
                    if (existingStaff != null)
                    {
                        ModelState.AddModelError("Staff.EmployeeId", "Employee ID already exists");
                        return View(viewModel);
                    }

                    var existingUser = await _userManager.FindByEmailAsync(viewModel.Staff.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Staff.Email", "Email already exists");
                        return View(viewModel);
                    }

                    var normalizedUserName = _userManager.NormalizeName(viewModel.Staff.UserName);
                    if (!string.IsNullOrWhiteSpace(normalizedUserName) &&
                        await _userManager.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName))
                    {
                        ModelState.AddModelError("Staff.UserName", "User name already exists");
                        return View(viewModel);
                    }

                    // Create staff object
                    var staff = new Staff
                    {
                        EmployeeId = viewModel.Staff.EmployeeId,
                        FirstName = viewModel.Staff.FirstName,
                        LastName = viewModel.Staff.LastName,
                        Department = viewModel.Staff.Department,
                        Designation = viewModel.Staff.Designation,
                        DateOfJoining = viewModel.Staff.DateOfJoining ?? DateTime.UtcNow,
                        Salary = viewModel.Staff.Salary,
                        Phone = viewModel.Staff.Phone,
                        Address = viewModel.Staff.Address,
                        IsActive = true,
                        User = new ApplicationUser
                        {
                            UserName = viewModel.Staff.UserName,
                            Email = viewModel.Staff.Email,
                            PhoneNumber = viewModel.Staff.Phone
                        }
                    };

                    // Create the staff
                    var createdStaff = await _staffService.CreateStaffAsync(staff, viewModel.Staff.Password, viewModel.Staff.SelectedRoleIds);

                    TempData["SuccessMessage"] = "Staff member created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating staff: {ex.Message}");
                }
            }

            // Reload roles if validation failed
            var roles = await _context.Roles.ToListAsync();
            viewModel.AvailableRoles = roles.Select(r => new RoleSelectionViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSelected = viewModel.Staff.SelectedRoleIds?.Contains(r.Id) == true
            }).ToList();

            return View(viewModel);
        }

        // GET: Staff/Edit/5
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Edit(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var viewModel = new StaffEditViewModel
            {
                Staff = new StaffUpdateDto
                {
                    Id = staff.Id,
                    EmployeeId = staff.EmployeeId,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Email = staff.User.Email,
                    Phone = staff.Phone,
                    Department = staff.Department,
                    Designation = staff.Designation,
                    DateOfJoining = staff.DateOfJoining,
                    Salary = staff.Salary,
                    Address = staff.Address,
                    IsActive = staff.IsActive,
                    SelectedRoleIds = staff.StaffRoles.Select(sr => sr.RoleId).ToList()
                }
            };

            // Load available roles
            var roles = await _context.Roles.ToListAsync();
            viewModel.AvailableRoles = roles.Select(r => new RoleSelectionViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSelected = staff.StaffRoles.Any(sr => sr.RoleId == r.Id),
                IsAssigned = staff.StaffRoles.Any(sr => sr.RoleId == r.Id)
            }).ToList();

            // Load current roles
            viewModel.CurrentRoles = staff.StaffRoles.Select(sr => new StaffRoleDto
            {
                RoleId = sr.RoleId,
                RoleName = sr.Role.Name,
                RoleDescription = sr.Role.Description,
                AssignedDate = sr.AssignedDate,
                AssignedBy = sr.AssignedBy,
                IsAssigned = true
            }).ToList();

            return View(viewModel);
        }

        // POST: Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Edit(string id, StaffEditViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            if (id != viewModel.Staff.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if employee ID is unique (excluding current user)
                    var existingStaff = await _staffService.GetStaffByEmployeeIdAsync(viewModel.Staff.EmployeeId);
                    if (existingStaff != null && existingStaff.Id != id)
                    {
                        ModelState.AddModelError("Staff.EmployeeId", "Employee ID already exists");
                        return View(viewModel);
                    }

                    // Check if email is unique (excluding current user)
                    var existingUser = await _userManager.FindByEmailAsync(viewModel.Staff.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        ModelState.AddModelError("Staff.Email", "Email already exists");
                        return View(viewModel);
                    }

                    // Update staff object
                    var staff = new Staff
                    {
                        Id = viewModel.Staff.Id,
                        EmployeeId = viewModel.Staff.EmployeeId,
                        FirstName = viewModel.Staff.FirstName,
                        LastName = viewModel.Staff.LastName,
                        Department = viewModel.Staff.Department,
                        Designation = viewModel.Staff.Designation,
                        DateOfJoining = viewModel.Staff.DateOfJoining,
                        Salary = viewModel.Staff.Salary,
                        Phone = viewModel.Staff.Phone,
                        Address = viewModel.Staff.Address,
                        IsActive = viewModel.Staff.IsActive
                    };

                    // Update the staff
                    await _staffService.UpdateStaffAsync(staff, viewModel.Staff.SelectedRoleIds);

                    TempData["SuccessMessage"] = "Staff member updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating staff: {ex.Message}");
                }
            }

            // Reload roles if validation failed
            var roles = await _context.Roles.ToListAsync();
            viewModel.AvailableRoles = roles.Select(r => new RoleSelectionViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSelected = viewModel.Staff.SelectedRoleIds?.Contains(r.Id) == true,
                IsAssigned = viewModel.Staff.SelectedRoleIds?.Contains(r.Id) == true
            }).ToList();

            return View(viewModel);
        }

        // GET: Staff/Delete/5
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var viewModel = new StaffDeleteViewModel
            {
                Staff = new StaffDto
                {
                    Id = staff.Id,
                    EmployeeId = staff.EmployeeId,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Email = staff.User.Email,
                    Phone = staff.Phone,
                    Department = staff.Department,
                    Designation = staff.Designation,
                    DateOfJoining = staff.DateOfJoining,
                    Salary = staff.Salary,
                    Address = staff.Address,
                    IsActive = staff.IsActive,
                    CreatedDate = staff.CreatedDate,
                    LastLoginDate = staff.User.LastLoginDate,
                    Roles = staff.StaffRoles.Select(sr => sr.Role.Name).ToList()
                }
            };

            // Get impact assessment data (simplified)
            viewModel.PatientsManaged = 0; // Would need to implement based on staff role
            viewModel.AppointmentsScheduled = 0;
            viewModel.BillsCreated = 0;
            viewModel.RecentActivities = await _context.AuditLogs.CountAsync(a => a.UserId == id && a.Timestamp > DateTime.UtcNow.AddDays(-30));

            // Get available staff for data transfer
            var allStaff = await _staffService.GetAllStaffAsync();
            viewModel.AvailableStaffForTransfer = allStaff
                .Where(s => s.Id != id && s.IsActive)
                .Select(s => new StaffDto
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.User.Email
                })
                .ToList();

            return View(viewModel);
        }

        // POST: Staff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> DeleteConfirmed(string id, bool transferData, string transferToStaffId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            try
            {
                // If transfer is requested, implement data transfer logic here
                if (transferData && !string.IsNullOrEmpty(transferToStaffId))
                {
                    // Transfer logic would go here
                    // This is a placeholder for future implementation
                }

                var result = await _staffService.DeleteStaffAsync(id);

                if (result)
                {
                    TempData["SuccessMessage"] = "Staff member deleted successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete staff member";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting staff: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Staff/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Activate(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            try
            {
                var result = await _staffService.ActivateStaffAsync(id);

                if (result)
                {
                    TempData["SuccessMessage"] = "Staff member activated successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to activate staff member";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error activating staff: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Staff/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            try
            {
                var result = await _staffService.DeactivateStaffAsync(id);

                if (result)
                {
                    TempData["SuccessMessage"] = "Staff member deactivated successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to deactivate staff member";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deactivating staff: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Staff/ChangePassword/5
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> ChangePassword(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            if (!await CanManagePasswordForTargetAsync(staff.Id))
            {
                return Forbid();
            }

            var viewModel = new StaffPasswordChangeViewModel
            {
                PasswordChange = new StaffPasswordChangeDto { StaffId = id },
                StaffName = $"{staff.FirstName} {staff.LastName}",
                StaffEmail = staff.User.Email
            };

            return View(viewModel);
        }

        // POST: Staff/ChangePassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> ChangePassword(StaffPasswordChangeViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            if (!await CanManagePasswordForTargetAsync(viewModel.PasswordChange.StaffId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _staffService.ResetStaffPasswordAsync(viewModel.PasswordChange.StaffId, viewModel.PasswordChange.NewPassword);

                    if (result)
                    {
                        TempData["SuccessMessage"] = "Password changed successfully";
                        return RedirectToAction(nameof(Details), new { id = viewModel.PasswordChange.StaffId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to change password");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error changing password: {ex.Message}");
                }
            }

            // Reload staff info if validation failed
            var staff = await _staffService.GetStaffByIdAsync(viewModel.PasswordChange.StaffId);
            if (staff != null)
            {
                viewModel.StaffName = $"{staff.FirstName} {staff.LastName}";
                viewModel.StaffEmail = staff.User.Email;
            }

            return View(viewModel);
        }

        private async Task<bool> CanManagePasswordForTargetAsync(string targetUserId)
        {
            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return false;

            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null)
                return false;

            var actorRoles = await _userManager.GetRolesAsync(actor);
            var targetRoles = await _userManager.GetRolesAsync(targetUser);

            var actorIsSuperAdmin = actorRoles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
            var actorIsAdmin = actorRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            var targetIsSuperAdmin = targetRoles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

            if (actorIsSuperAdmin)
                return true;

            if (actorIsAdmin && !targetIsSuperAdmin)
                return true;

            return false;
        }

        // GET: Staff/Dashboard
        [PermissionAuthorize("ManageUsers")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _authorizationService.HasPermissionAsync(userId, "ManageUsers"))
            {
                return Forbid();
            }

            var viewModel = new StaffDashboardViewModel();

            // Get statistics
            var stats = await _staffService.GetStaffStatisticsAsync();
            viewModel.TotalStaff = stats["TotalStaff"];
            viewModel.ActiveStaff = stats["ActiveStaff"];
            viewModel.NewStaffThisMonth = stats["NewStaffThisMonth"];

            // Get department breakdown
            var allStaff = await _staffService.GetAllStaffAsync();
            viewModel.StaffByDepartment = allStaff
                .Where(s => !string.IsNullOrWhiteSpace(s.Department))
                .GroupBy(s => s.Department)
                .ToDictionary(g => g.Key, g => g.Count());

            // Get role distribution
            var staffWithRoles = await _context.Staff
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .ToListAsync();

            viewModel.StaffByRole = staffWithRoles
                .SelectMany(s => s.StaffRoles)
                .GroupBy(sr => sr.Role.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            // Get recent staff
            var recentStaff = await _staffService.GetRecentStaffAsync(5);
            viewModel.RecentStaff = recentStaff.Select(s => new StaffDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.User.Email,
                Department = s.Department,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate
            }).ToList();

            // Get top active users (simplified)
            viewModel.TopActiveUsers = await _context.AuditLogs
                .Where(a => a.Timestamp > DateTime.UtcNow.AddDays(-7))
                .GroupBy(a => a.UserId)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToListAsync();

            return View(viewModel);
        }
    }
}
