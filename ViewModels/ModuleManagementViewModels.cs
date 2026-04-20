using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.ViewModels
{
    // ── Global Module Management (SuperAdmin) ─────────────────────────────────
    public class SystemModuleListViewModel
    {
        public IReadOnlyList<SystemModule> Modules { get; set; } = [];
    }

    // ── User Selection for Module Assignment (Admin + SuperAdmin) ─────────────
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

    // ── Per-User Module Access (Admin + SuperAdmin) ────────────────────────────
    public class UserModuleAccessViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public IReadOnlyList<UserModuleAccessRow> Rows { get; set; } = [];
    }
}
