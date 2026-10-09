using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MedyxHMS.Extensions
{
    /// <summary>
    /// Works out which hospital of the group the signed-in staff member is working in and fills the
    /// per-request <see cref="IHospitalContext"/>. The choice is kept in a cookie (set by Hospital/Switch)
    /// and is always re-checked against the user's hospital access.
    /// </summary>
    public class HospitalContextMiddleware
    {
        public const string CookieName = "MedyxHMS.Hospital";
        public const string AllHospitalsValue = "all";
        private const string HospitalsCacheKey = "hospitals:list";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        private readonly RequestDelegate _next;

        public HospitalContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IHospitalContext hospitalContext, ApplicationDbContext db, IMemoryCache cache)
        {
            if (hospitalContext is HospitalContext holder && !IsStaticFile(context.Request.Path))
            {
                await FillAsync(context, holder, db, cache);
            }

            await _next(context);
        }

        /// <summary>Clears cached hospital lists after hospitals or access rights change.</summary>
        public static void InvalidateCache(IMemoryCache cache, string? userId = null)
        {
            cache.Remove(HospitalsCacheKey);
            if (!string.IsNullOrEmpty(userId))
            {
                cache.Remove(UserCacheKey(userId));
            }
        }

        public static void InvalidateUsers(IMemoryCache cache, IEnumerable<string> userIds)
        {
            foreach (var id in userIds)
            {
                cache.Remove(UserCacheKey(id));
            }
        }

        private static string UserCacheKey(string userId) => "hospitals:user:" + userId;

        /// <summary>
        /// Hospitals a staff member can work in (active only): every hospital for a SuperAdmin, otherwise the
        /// assigned ones – or the default hospital when nothing is assigned. Also returns the hospital chosen
        /// for them after signing in (their default). Used by the sign-in page, where the request is not yet
        /// signed in and the per-request hospital context is empty.
        /// </summary>
        public static async Task<(List<HospitalOption> Hospitals, int? DefaultId)> GetUserHospitalsAsync(ApplicationDbContext db, string userId, bool isSuperAdmin)
        {
            var active = await db.Hospitals.AsNoTracking()
                .Where(h => h.IsActive)
                .OrderByDescending(h => h.IsDefault).ThenBy(h => h.Name)
                .Select(h => new HospitalOption(h.Id, h.Code, h.Name, h.IsActive, h.IsDefault, h.City))
                .ToListAsync();
            var groupDefault = (active.FirstOrDefault(h => h.IsDefault) ?? active.FirstOrDefault())?.Id;
            var access = await db.UserHospitalAccesses.AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => new { a.HospitalId, a.IsDefault })
                .ToListAsync();

            var assigned = access.Select(a => a.HospitalId).ToHashSet();
            var hospitals = isSuperAdmin ? active : active.Where(h => assigned.Contains(h.Id)).ToList();
            if (hospitals.Count == 0)
            {
                hospitals = active.Where(h => h.Id == groupDefault).ToList();
            }

            var userDefault = access.FirstOrDefault(a => a.IsDefault && hospitals.Any(h => h.Id == a.HospitalId))?.HospitalId
                              ?? (hospitals.Any(h => h.Id == groupDefault) ? groupDefault : hospitals.FirstOrDefault()?.Id);
            return (hospitals, userDefault);
        }

        /// <summary>The hospital the user chose last on this browser ("all", a hospital id or null).</summary>
        public static string? ReadSelection(HttpRequest request, string userId)
        {
            // Cookie format: "<userId>|<hospitalId or all>" so a shared browser does not carry one user's choice to another.
            if (!request.Cookies.TryGetValue(CookieName, out var raw) || string.IsNullOrEmpty(raw))
            {
                return null;
            }

            var parts = raw.Split('|');
            return parts.Length == 2 && parts[0] == userId ? parts[1] : null;
        }

        /// <summary>Remembers the hospital the user works in (sign-in page and the hospital switcher).</summary>
        public static void WriteSelection(HttpContext context, string userId, string value)
        {
            context.Response.Cookies.Append(CookieName, $"{userId}|{value}", new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            // The request that sets the cookie (sign-in) redirects at once; the next request reads it.
        }

        private static async Task FillAsync(HttpContext context, HospitalContext holder, ApplicationDbContext db, IMemoryCache cache)
        {
            List<HospitalOption> all;
            try
            {
                all = await cache.GetOrCreateAsync(HospitalsCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    return await db.Hospitals.AsNoTracking()
                        .OrderByDescending(h => h.IsDefault).ThenBy(h => h.Name)
                        .Select(h => new HospitalOption(h.Id, h.Code, h.Name, h.IsActive, h.IsDefault, h.City))
                        .ToListAsync();
                }) ?? new List<HospitalOption>();
            }
            catch (Exception)
            {
                // Hospitals table not available (e.g. during first start-up): behave as a single-hospital system.
                return;
            }

            var active = all.Where(h => h.IsActive).ToList();
            holder.AllHospitals = all;
            holder.DefaultHospitalId = (active.FirstOrDefault(h => h.IsDefault) ?? active.FirstOrDefault())?.Id;
            holder.HospitalIdForNewRecords = holder.DefaultHospitalId;

            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true
                || context.Request.Path.StartsWithSegments("/PatientPortal")
                || !user.FindAll(ClaimTypes.Role).Any(r => !string.Equals(r.Value, "Patient", StringComparison.OrdinalIgnoreCase)))
            {
                // Patients, the public site and anonymous calls see their records across the whole group.
                return;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || active.Count == 0)
            {
                return;
            }

            var access = await cache.GetOrCreateAsync(UserCacheKey(userId), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await db.UserHospitalAccesses.AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.HospitalId, a.IsDefault })
                    .ToListAsync();
            });

            var isSuperAdmin = user.IsInRole("SuperAdmin");
            var assigned = access?.Select(a => a.HospitalId).ToHashSet() ?? new HashSet<int>();
            List<HospitalOption> accessible;
            if (isSuperAdmin)
            {
                accessible = active;
            }
            else
            {
                accessible = active.Where(h => assigned.Contains(h.Id)).ToList();
                if (accessible.Count == 0)
                {
                    // Staff without an assignment (all staff before multi-hospital was set up) work in the default hospital.
                    accessible = active.Where(h => h.Id == holder.DefaultHospitalId).ToList();
                }
            }

            holder.IsStaffContext = true;
            holder.AccessibleHospitals = accessible;
            holder.CanViewAllHospitals = isSuperAdmin && all.Count > 1;

            var userDefault = access?.FirstOrDefault(a => a.IsDefault && accessible.Any(h => h.Id == a.HospitalId))?.HospitalId
                              ?? (accessible.Any(h => h.Id == holder.DefaultHospitalId) ? holder.DefaultHospitalId : accessible.FirstOrDefault()?.Id);

            var selection = ReadSelection(context.Request, userId);
            // "All hospitals" is offered only when the group has more than one hospital (the switcher is hidden otherwise).
            if (selection == AllHospitalsValue && isSuperAdmin && all.Count > 1)
            {
                holder.FilterEnabled = false;
                holder.ActiveHospitalId = null;
                holder.HospitalIdForNewRecords = userDefault ?? holder.DefaultHospitalId;
                return;
            }

            int? chosen = int.TryParse(selection, out var id) && accessible.Any(h => h.Id == id) ? id : userDefault;
            if (!chosen.HasValue)
            {
                return;
            }

            holder.ActiveHospitalId = chosen;
            holder.HospitalIdForNewRecords = chosen;
            holder.FilterEnabled = true;
        }

        private static bool IsStaticFile(PathString path)
        {
            var value = path.Value ?? string.Empty;
            return value.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
        }
    }
}
