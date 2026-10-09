// Purpose: Lets navigation (sidebar, menus, buttons) show only links the current user is allowed to open.
namespace MedyxHMS.Services.Interfaces
{
    public interface INavAccessService
    {
        /// <summary>True when the current user would be allowed to open the given local URL (e.g. "/Billing/Create").</summary>
        Task<bool> CanAccessAsync(string url);

        /// <summary>True when the current user may open at least one of the given URLs.</summary>
        Task<bool> CanAccessAnyAsync(params string[] urls);
    }
}
