using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// Purpose: The signed-in user's profile picture in the top bar (uploaded in My Profile), linking to My Profile.
namespace MedyxHMS.Components
{
    public class ProfileImageViewComponent : ViewComponent
    {
        private readonly IProfileImageService _profileImageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileImageViewComponent(IProfileImageService profileImageService, UserManager<ApplicationUser> userManager)
        {
            _profileImageService = profileImageService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int size = 32, string? linkUrl = "/Account/Profile")
        {
            var user = UserClaimsPrincipal?.Identity?.IsAuthenticated == true ? await _userManager.GetUserAsync(UserClaimsPrincipal) : null;
            var model = new ProfileImageModel(_profileImageService.GetDisplayPath(user?.ProfileImage), size, linkUrl,
                user == null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim());
            return View("Default", model);
        }
    }

    public sealed record ProfileImageModel(string ImageUrl, int Size, string? LinkUrl, string Name);
}
