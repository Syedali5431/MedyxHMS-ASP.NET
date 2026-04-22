using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

// Purpose: Contains application code for ModuleManagementViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    // â”€â”€ Global Module Management (SuperAdmin) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class SystemModuleListViewModel
    {
        public IReadOnlyList<SystemModule> Modules { get; set; } = [];
    }

    // â”€â”€ User Selection for Module Assignment (Admin + SuperAdmin) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class UserListForModulesViewModel
    {
        public IReadOnlyList<UserSummary> Users { get; set; } = [];
        public string? SearchTerm { get; set; }
    }

    public class UserSummary
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public IList<string> Roles { get; set; } = [];
    }

    // â”€â”€ Per-User Module Access (Admin + SuperAdmin) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class UserModuleAccessViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public IReadOnlyList<UserModuleAccessRow> Rows { get; set; } = [];
    }
}
