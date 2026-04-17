using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    // ─── Admin: Page List ───────────────────────────────────────────────────────
    public class CmsPageIndexViewModel
    {
        public List<CmsPage> Pages { get; set; } = new();
        public string StatusFilter { get; set; }
        public string SearchTerm { get; set; }
    }

    // ─── Admin: Create / Edit Page ─────────────────────────────────────────────
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

        [Required]
        public string Status { get; set; } = "Draft";

        [Display(Name = "Show in Menu")]
        public bool ShowInMenu { get; set; }

        public int SortOrder { get; set; }
    }

    // ─── Admin: Notice List ────────────────────────────────────────────────────
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

    // ─── Admin: Create / Edit Notice ──────────────────────────────────────────
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

    // ─── Admin: Menu Item List ─────────────────────────────────────────────────
    public class CmsMenuIndexViewModel
    {
        public List<CmsMenuItem> MenuItems { get; set; } = new();
    }

    // ─── Admin: Create / Edit Menu Item ───────────────────────────────────────
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

    // ─── Admin: Public Appointment Requests ───────────────────────────────────
    public class PublicAppointmentRequestIndexViewModel
    {
        public List<PublicAppointmentRequest> Requests { get; set; } = new();
        public string StatusFilter { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    }
}
