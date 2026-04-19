using System.Security.Claims;
using System.Text;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class ChatbotKnowledgeService : IChatbotKnowledgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingService _settingService;

        public ChatbotKnowledgeService(ApplicationDbContext context, ISettingService settingService)
        {
            _context = context;
            _settingService = settingService;
        }

        public async Task<ChatKnowledgeContext> RetrieveContextAsync(ClaimsPrincipal user, string message, string? languageCode = null)
        {
            var tokens = Tokenize(message);
            var context = new ChatKnowledgeContext();
            var builder = new StringBuilder();
            var normalizedLanguage = NormalizeLanguage(languageCode);
            context.LanguageCode = normalizedLanguage;
            context.DetectedCategory = DetectCategory(message);

            var role = ResolveRole(user);
            builder.AppendLine($"Role Context: {role}");

            var hospital = await _settingService.GetHospitalSettingsAsync();
            builder.AppendLine($"Hospital: {hospital.Name}");

            var supportEmail = await _settingService.GetSettingValueAsync("LicenseSuperAdminContact") ?? "superadmin@hospital.com";
            var supportContact = await _settingService.GetSettingValueAsync("ChatbotSupportContact") ?? supportEmail;
            context.Sources.Add(new ChatKnowledgeSource
            {
                SourceType = "Contact",
                SourceName = "Support Contact",
                SourcePath = "Settings:LicenseSuperAdminContact",
                Excerpt = supportContact
            });

            var publishedPages = await _context.CmsPages.AsNoTracking()
                .Where(p => p.Status == "Published")
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(60)
                .ToListAsync();

            var matchedPages = publishedPages
                .Where(p => tokens.Count == 0 || MatchesAnyToken(tokens, p.Title, p.Slug, p.Content, p.MetaDescription))
                .Take(3)
                .ToList();

            foreach (var page in matchedPages)
            {
                context.Sources.Add(new ChatKnowledgeSource
                {
                    SourceType = "CMS Page",
                    SourceName = page.Title,
                    SourcePath = $"/Cms/Page/{page.Slug}",
                    Excerpt = Truncate(page.Content ?? page.MetaDescription ?? string.Empty, 220)
                });
            }

            var activeNotices = await _context.CmsNotices.AsNoTracking()
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
                .Take(60)
                .ToListAsync();

            var matchedNotices = activeNotices
                .Where(n => tokens.Count == 0 || MatchesAnyToken(tokens, n.Title, n.Slug, n.Summary, n.Content))
                .Take(3)
                .ToList();

            foreach (var notice in matchedNotices)
            {
                context.Sources.Add(new ChatKnowledgeSource
                {
                    SourceType = "CMS Notice",
                    SourceName = notice.Title,
                    SourcePath = $"/Cms/Notice/{notice.Slug}",
                    Excerpt = Truncate(notice.Summary ?? notice.Content ?? string.Empty, 220)
                });
            }

            var lower = message.ToLowerInvariant();
            if (lower.Contains("appointment") || lower.Contains("book"))
            {
                var appointmentGuidance = await GetLocalizedSettingAsync("ChatbotAppointmentGuidance", normalizedLanguage,
                    "Use the appointment module to create, reschedule, or cancel bookings.");

                context.Sources.Add(new ChatKnowledgeSource
                {
                    SourceType = "Workflow",
                    SourceName = "Appointment Guidance",
                    SourcePath = role == "Patient" ? "/PatientPortal/Appointment" : "/Appointment",
                    Excerpt = appointmentGuidance
                });
            }

            if (lower.Contains("bill") || lower.Contains("payment") || lower.Contains("invoice"))
            {
                var billingGuidance = await GetLocalizedSettingAsync("ChatbotBillingGuidance", normalizedLanguage,
                    "Use the billing module to review invoices, payment status, and outstanding balances.");

                context.Sources.Add(new ChatKnowledgeSource
                {
                    SourceType = "Workflow",
                    SourceName = "Billing Guidance",
                    SourcePath = role == "Patient" ? "/PatientPortal/Billing" : "/Billing",
                    Excerpt = billingGuidance
                });
            }

            if (role == "Patient")
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
                    if (patient != null)
                    {
                        var upcomingAppointments = await _context.Appointments.AsNoTracking()
                            .Where(a => a.PatientId == patient.Id && a.AppointmentDate.Date >= DateTime.UtcNow.Date)
                            .OrderBy(a => a.AppointmentDate)
                            .Take(3)
                            .Select(a => new { a.AppointmentDate, a.Status })
                            .ToListAsync();

                        var pendingBills = await _context.Bills.AsNoTracking()
                            .Where(b => b.PatientId == patient.Id && b.Status != "Paid")
                            .Select(b => new { b.PendingAmount })
                            .ToListAsync();

                        var outstanding = pendingBills.Sum(b => b.PendingAmount);
                        builder.AppendLine($"Patient Context: UpcomingAppointments={upcomingAppointments.Count}, PendingBills={pendingBills.Count}, Outstanding={outstanding:0.##}");

                        if (upcomingAppointments.Count > 0)
                        {
                            builder.AppendLine($"Nearest Appointment Date: {upcomingAppointments[0].AppointmentDate:yyyy-MM-dd}");
                        }

                        context.Sources.Add(new ChatKnowledgeSource
                        {
                            SourceType = "Patient Data",
                            SourceName = "Patient Appointment Summary",
                            SourcePath = "/PatientPortal/Appointment",
                            Excerpt = $"Upcoming appointments: {upcomingAppointments.Count}"
                        });

                        context.Sources.Add(new ChatKnowledgeSource
                        {
                            SourceType = "Patient Data",
                            SourceName = "Patient Billing Summary",
                            SourcePath = "/PatientPortal/Billing",
                            Excerpt = $"Pending bills: {pendingBills.Count}, Outstanding: {outstanding:0.##}"
                        });
                    }
                }
            }

            if (context.Sources.Count == 0)
            {
                context.Sources.Add(new ChatKnowledgeSource
                {
                    SourceType = "Fallback",
                    SourceName = "General Support Guidance",
                    SourcePath = "/Dashboard",
                    Excerpt = "No specific knowledge source matched this query. Use module navigation and support contacts."
                });
            }

            builder.AppendLine("Grounding Sources:");
            foreach (var source in context.Sources.Take(8))
            {
                builder.AppendLine($"- [{source.SourceType}] {source.SourceName}: {source.Excerpt} (Path: {source.SourcePath})");
            }

            context.SystemContext = builder.ToString();
            return context;
        }

        private async Task<string> GetLocalizedSettingAsync(string baseKey, string languageCode, string fallback)
        {
            var langSpecific = await _settingService.GetSettingValueAsync($"{baseKey}.{languageCode}");
            if (!string.IsNullOrWhiteSpace(langSpecific))
            {
                return langSpecific;
            }

            var defaultValue = await _settingService.GetSettingValueAsync($"{baseKey}.en");
            return string.IsNullOrWhiteSpace(defaultValue) ? fallback : defaultValue;
        }

        private static HashSet<string> Tokenize(string message)
        {
            var chars = message.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();
            return chars
                .AsSpan()
                .ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3)
                .Take(12)
                .ToHashSet();
        }

        private static string ResolveRole(ClaimsPrincipal user)
        {
            if (user.IsInRole("Patient")) return "Patient";
            if (user.IsInRole("SuperAdmin")) return "SuperAdmin";
            if (user.IsInRole("Admin")) return "Admin";
            if (user.IsInRole("Doctor")) return "Doctor";
            if (user.IsInRole("Nurse")) return "Nurse";
            return "Staff";
        }

        private static string DetectCategory(string message)
        {
            var lower = message.ToLowerInvariant();
            if (lower.Contains("appointment") || lower.Contains("book") || lower.Contains("schedule")) return "Appointment";
            if (lower.Contains("bill") || lower.Contains("invoice") || lower.Contains("payment")) return "Billing";
            if (lower.Contains("help") || lower.Contains("support") || lower.Contains("contact")) return "Support";
            return "Navigation";
        }

        private static string NormalizeLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "en";
            }

            var normalized = languageCode.Trim().ToLowerInvariant();
            return normalized.Length <= 5 ? normalized : "en";
        }

        private static bool MatchesAnyToken(IEnumerable<string> tokens, params string?[] values)
        {
            var haystack = string.Join(' ', values.Where(v => !string.IsNullOrWhiteSpace(v))).ToLowerInvariant();
            return tokens.Any(t => haystack.Contains(t));
        }

        private static string Truncate(string input, int max)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            if (input.Length <= max) return input;
            return input[..max] + "...";
        }
    }
}
