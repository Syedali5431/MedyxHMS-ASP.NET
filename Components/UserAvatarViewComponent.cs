using MedyxHMS.Data;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Purpose: Another user's avatar in lists (e.g. Staff Management): the picture uploaded in My Profile, or the
// initials in a circle when there is none. Pictures are looked up once per request and user.
namespace MedyxHMS.Components
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private const string CacheKey = "MedyxHMS.UserAvatarCache";
        private readonly ApplicationDbContext _db;
        private readonly IProfileImageService _images;

        public UserAvatarViewComponent(ApplicationDbContext db, IProfileImageService images)
        {
            _db = db;
            _images = images;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? userId, string? name, int size = 40)
        {
            string? image = null;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (HttpContext.Items[CacheKey] is not Dictionary<string, string?> cache)
                {
                    cache = new Dictionary<string, string?>();
                    HttpContext.Items[CacheKey] = cache;
                }
                if (!cache.TryGetValue(userId, out image))
                {
                    image = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.ProfileImage).FirstOrDefaultAsync();
                    cache[userId] = image;
                }
            }

            var initials = string.Concat((name ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => !p.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .Select(p => char.ToUpperInvariant(p[0])));
            return View("Default", new UserAvatarModel(string.IsNullOrWhiteSpace(image) ? null : _images.GetDisplayPath(image), initials, size, name ?? string.Empty));
        }
    }

    public sealed record UserAvatarModel(string? ImageUrl, string Initials, int Size, string Name);
}
