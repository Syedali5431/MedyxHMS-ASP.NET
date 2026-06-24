using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Components
{
    public class ProfileImageViewComponent : ViewComponent
    {
        private readonly IProfileImageService _profileImageService;

        public ProfileImageViewComponent(IProfileImageService profileImageService)
        {
            _profileImageService = profileImageService;
        }

        public IViewComponentResult Invoke()
        {
            var profileImage = ViewData["CurrentUserProfileImage"] as string;
            var path = _profileImageService.GetDisplayPath(profileImage);
            return View("Default", path);
        }
    }
}