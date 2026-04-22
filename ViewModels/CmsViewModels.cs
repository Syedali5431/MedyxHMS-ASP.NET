using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Http;

// Purpose: Contains application code for CmsViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    // â”€â”€â”€ Admin: Page List â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsPageIndexViewModel
    {
        public List<CmsPage> Pages { get; set; } = new();
        public string StatusFilter { get; set; }
        public string SearchTerm { get; set; }
    }

    // â”€â”€â”€ Admin: Create / Edit Page â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsPageEditViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Page Title")]
        public string Title { get; set; }

        [Required, MaxLength(200), RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "Slug may only contain lowercase letters, numbers, and hyphens.")]
        public string Slug { get; set; }

        [Display(Name = "Content (HTML)")]
        public string Content { get; set; }

        [MaxLength(300), Display(Name = "Meta Description")]
        public string MetaDescription { get; set; }

        [Display(Name = "Featured Image")]
        public string? FeaturedImage { get; set; }

        [Display(Name = "Upload Featured Image")]
        public IFormFile? FeaturedImageFile { get; set; }

        [MaxLength(100)]
        [Display(Name = "Font Family")]
        public string? FontFamily { get; set; }

        [Range(12, 40)]
        [Display(Name = "Font Size (px)")]
        public int? FontSizePx { get; set; }

        [Required]
        public string Status { get; set; } = "Draft";

        [Display(Name = "Show in Menu")]
        public bool ShowInMenu { get; set; }

        public int SortOrder { get; set; }
    }

    // â”€â”€â”€ Admin: Notice List â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsNoticeIndexViewModel
    {
        public List<CmsNotice> Notices { get; set; } = new();
        public string TypeFilter { get; set; }
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    }

    // â”€â”€â”€ Admin: Create / Edit Notice â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsNoticeEditViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required, MaxLength(200), RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "Slug may only contain lowercase letters, numbers, and hyphens.")]
        public string Slug { get; set; }

        [MaxLength(500)]
        public string Summary { get; set; }

        [Display(Name = "Content (HTML)")]
        public string Content { get; set; }

        [Required]
        public string Type { get; set; } = "Notice";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Published At")]
        public DateTime? PublishedAt { get; set; }
    }

    // â”€â”€â”€ Admin: Menu Item List â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsMenuIndexViewModel
    {
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    // â”€â”€â”€ Admin: Create / Edit Menu Item â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsMenuItemEditViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Label { get; set; }

        [MaxLength(300)]
        public string Url { get; set; }

        [Display(Name = "Link to CMS Page")]
        public int? CmsPageId { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Open in New Tab")]
        public bool OpenInNewTab { get; set; }

        // For dropdown in view
        public List<CmsPage> AvailablePages { get; set; } = new();
    }

    // â”€â”€â”€ Admin: Public Appointment Requests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class PublicAppointmentRequestIndexViewModel
    {
        public List<PublicAppointmentRequest> Requests { get; set; } = new();
        public List<PublicAppointmentRequestListItemViewModel> RequestRows { get; set; } = new();
        public string StatusFilter { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool DuplicatesOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    }

    public class PublicAppointmentRequestListItemViewModel
    {
        public PublicAppointmentRequest Request { get; set; }
        public int DuplicateCount { get; set; }
        public int ActiveDuplicateCount { get; set; }
        public bool HasDuplicates => DuplicateCount > 1;
    }

    public class PublicAppointmentDuplicateReviewViewModel
    {
        public PublicAppointmentRequest PrimaryRequest { get; set; }
        public List<PublicAppointmentRequest> MatchingRequests { get; set; } = new();
        public int TotalMatches => MatchingRequests.Count;
        public int ActiveMatches => MatchingRequests.Count(r => r.Status == "Pending" || r.Status == "Confirmed");
    }

    // â”€â”€â”€ Admin: Notification Settings â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CmsNotificationSettingsViewModel
    {
        [Display(Name = "Enable Email Notifications")]
        public bool EnableEmailNotifications { get; set; }

        [Display(Name = "Enable SMS Notifications")]
        public bool EnableSMSNotifications { get; set; }

        [Display(Name = "Twilio Account SID")]
        [MaxLength(100)]
        public string TwilioAccountSid { get; set; }

        [Display(Name = "Twilio Auth Token")]
        [DataType(DataType.Password)]
        [MaxLength(200)]
        public string TwilioAuthToken { get; set; }

        [Display(Name = "Twilio From Phone")]
        [MaxLength(30)]
        public string TwilioFromPhone { get; set; }

        [Display(Name = "Enable Twilio Live Send")]
        public bool TwilioEnableLiveSend { get; set; }

        [Display(Name = "SMS Provider")]
        [MaxLength(30)]
        public string SmsProvider { get; set; }

        [Display(Name = "Africa's Talking Username")]
        [MaxLength(100)]
        public string AfricaTalkingUsername { get; set; }

        [Display(Name = "Africa's Talking API Key")]
        [DataType(DataType.Password)]
        [MaxLength(200)]
        public string AfricaTalkingApiKey { get; set; }

        [Display(Name = "Africa's Talking Sender ID")]
        [MaxLength(30)]
        public string AfricaTalkingSenderId { get; set; }

        [Display(Name = "Enable Africa's Talking Live Send")]
        public bool AfricaTalkingEnableLiveSend { get; set; }

        [Display(Name = "Test SMS Phone")]
        [MaxLength(30)]
        public string TestSmsPhone { get; set; }

        [Display(Name = "Test Email Address")]
        [EmailAddress]
        [MaxLength(200)]
        public string TestEmailTo { get; set; }

        [Display(Name = "Appointment Email Subject Template")]
        [MaxLength(300)]
        public string AppointmentConfirmedEmailSubjectTemplate { get; set; }

        [Display(Name = "Appointment Email Body Template")]
        public string AppointmentConfirmedEmailBodyTemplate { get; set; }

        [Display(Name = "Appointment SMS Body Template")]
        [MaxLength(500)]
        public string AppointmentConfirmedSmsBodyTemplate { get; set; }

        [Display(Name = "Email Opt-Out List")]
        public string EmailOptOutList { get; set; }

        [Display(Name = "SMS Opt-Out List")]
        public string SmsOptOutList { get; set; }

        public bool HasSavedTwilioAuthToken { get; set; }
        public bool HasSavedAfricaTalkingApiKey { get; set; }

        public string LastSmsTestStatus { get; set; }
        public string LastSmsTestMessage { get; set; }
        public string LastSmsTestTarget { get; set; }
        public DateTime? LastSmsTestAtUtc { get; set; }

        public string LastEmailTestStatus { get; set; }
        public string LastEmailTestMessage { get; set; }
        public string LastEmailTestTarget { get; set; }
        public DateTime? LastEmailTestAtUtc { get; set; }

        public SmtpHealthStatus? SmtpHealth { get; set; }
    }

    public class NotificationDeliveryLogIndexViewModel
    {
        public List<NotificationDeliveryLog> Logs { get; set; } = new();
        public string ChannelFilter { get; set; }
        public string StatusFilter { get; set; }
        public string RecipientFilter { get; set; }
        public bool? IsTestFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    }
}
