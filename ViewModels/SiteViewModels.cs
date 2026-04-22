using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Http;

// Purpose: Contains application code for SiteViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    // â”€â”€â”€ Public Homepage â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class SiteHomeViewModel
    {
        public List<CmsNotice> RecentNotices { get; set; } = new();
        public List<CmsNotice> RecentNews { get; set; } = new();
        public List<CmsNotice> UpcomingEvents { get; set; } = new();
        public List<CmsMenuItem> MenuItems { get; set; } = new();
        public string HospitalName { get; set; }
        public string HospitalTagline { get; set; }
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string MapEmbedUrl { get; set; } = string.Empty;
        public string HeroImageUrl { get; set; } = string.Empty;
        public string HeroDescription { get; set; } = string.Empty;
        public string FontFamily { get; set; } = string.Empty;
    }

    public class SiteContactViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string MapEmbedUrl { get; set; } = string.Empty;
        public string HeroImageUrl { get; set; } = string.Empty;
        public string HeroDescription { get; set; } = string.Empty;
        public string FontFamily { get; set; } = string.Empty;
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    public class SiteCareersViewModel
    {
        public string CareersContent { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    public class PublicSiteSettingsViewModel
    {
        [MaxLength(250)]
        public string PublicAddress { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PublicPhone { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string PublicEmail { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string PublicMapEmbedUrl { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string CareersContent { get; set; } = string.Empty;

        [MaxLength(200)]
        public string HomeTitle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string HomeTagline { get; set; } = string.Empty;

        [MaxLength(500)]
        public string HomeDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string HomeFontFamily { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ContactDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContactFontFamily { get; set; } = string.Empty;

        [MaxLength(500)]
        public string LocationDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LocationFontFamily { get; set; } = string.Empty;

        [MaxLength(300)]
        public string HomeHeroImage { get; set; } = string.Empty;

        public IFormFile? HomeHeroImageFile { get; set; }

        [MaxLength(300)]
        public string ContactHeroImage { get; set; } = string.Empty;

        public IFormFile? ContactHeroImageFile { get; set; }

        [MaxLength(300)]
        public string LocationHeroImage { get; set; } = string.Empty;

        public IFormFile? LocationHeroImageFile { get; set; }

        [MaxLength(7)]
        public string PrimaryColor { get; set; } = "#1a5276";

        [MaxLength(7)]
        public string AccentColor { get; set; } = "#2980b9";

        [MaxLength(7)]
        public string SurfaceColor { get; set; } = "#f4f8fb";

        [MaxLength(50)]
        public string ThemePreset { get; set; } = "Custom";

        [MaxLength(20)]
        public string HeadingStyle { get; set; } = "Normal";

        [MaxLength(20)]
        public string ButtonStyle { get; set; } = "Rounded";
    }

    // â”€â”€â”€ Public CMS Page View â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class SitePageViewModel
    {
        public CmsPage Page { get; set; }
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    // â”€â”€â”€ Public Notice / News List â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class SiteNoticeListViewModel
    {
        public List<CmsNotice> Notices { get; set; } = new();
        public string Type { get; set; }   // e.g. "News", "Notice", "Event"
        public string PageTitle { get; set; }
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    // â”€â”€â”€ Public Doctor Listing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class SiteDoctorListViewModel
    {
        public List<DoctorWithShifts> Doctors { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public int? DepartmentFilter { get; set; }
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    public class DoctorWithShifts
    {
        public Doctor Doctor { get; set; }
        public List<DoctorShift> Shifts { get; set; } = new();
    }

    // â”€â”€â”€ Public Booking Form â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class PublicBookingViewModel
    {
        // Form fields
        [Required, MaxLength(150), Display(Name = "Full Name")]
        public string PatientName { get; set; }

        [Required, MaxLength(20), Phone]
        public string Phone { get; set; }

        [MaxLength(200), EmailAddress]
        public string Email { get; set; }

        [MaxLength(10)]
        public string Gender { get; set; }

        [MaxLength(10)]
        public string Age { get; set; }

        [Required(ErrorMessage = "Please select a doctor.")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required, Display(Name = "Preferred Date")]
        [DataType(DataType.Date)]
        public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);

        [Required, Display(Name = "Preferred Time")]
        public string PreferredTimeStr { get; set; }

        [MaxLength(500)]
        public string Symptoms { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        [Display(Name = "Captcha")]
        public string? CaptchaQuestion { get; set; }

        [Required(ErrorMessage = "Please solve the captcha challenge.")]
        [Display(Name = "Captcha Answer")]
        public string CaptchaAnswer { get; set; }

        // Honeypot field â€” must be empty on submit (anti-bot)
        public string Website { get; set; }

        // Populated for the view
        public List<Doctor> AvailableDoctors { get; set; } = new();
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    // â”€â”€â”€ Booking Confirmation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class BookingConfirmationViewModel
    {
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime PreferredDate { get; set; }
        public string PreferredTime { get; set; }
        public int RequestId { get; set; }
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    // â”€â”€â”€ Available Slot (JSON response) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class AvailableSlotDto
    {
        public string Time { get; set; }
        public string Display { get; set; }
    }
}
