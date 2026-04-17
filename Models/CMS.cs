using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedyxHMS.Models
{
    // ─────────────────────────────────────────────
    // CMS Page (public website pages, e.g. About, Services)
    // ─────────────────────────────────────────────
    public class CmsPage
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required, MaxLength(200)]
        public string Slug { get; set; }   // URL-friendly identifier, e.g. "about-us"

        public string? Content { get; set; }  // HTML content

        [MaxLength(300)]
        public string? MetaDescription { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Draft";  // Draft | Published

        public bool ShowInMenu { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
    }

    // ─────────────────────────────────────────────
    // CMS Navigation Menu Item
    // ─────────────────────────────────────────────
    public class CmsMenuItem
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Label { get; set; }

        [MaxLength(300)]
        public string? Url { get; set; }    // External URL or relative path

        public int? CmsPageId { get; set; }  // Link to a CmsPage (optional)

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool OpenInNewTab { get; set; } = false;

        // Navigation
        [ForeignKey(nameof(CmsPageId))]
        public CmsPage? CmsPage { get; set; }
    }

    // ─────────────────────────────────────────────
    // CMS Notice / News / Event / Program
    // ─────────────────────────────────────────────
    public class CmsNotice
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required, MaxLength(200)]
        public string Slug { get; set; }

        public string? Summary { get; set; }   // Short description for listings

        public string? Content { get; set; }   // Full HTML content

        [MaxLength(20)]
        public string Type { get; set; } = "Notice";  // Notice | News | Event | Program

        [MaxLength(300)]
        public string? FeaturedImage { get; set; }  // File path or URL

        public bool IsActive { get; set; } = true;

        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }
    }

    // ─────────────────────────────────────────────
    // Doctor Shift Schedule (weekly availability for public booking)
    // ─────────────────────────────────────────────
    public class DoctorShift
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }

        [Range(0, 6)]
        public int DayOfWeek { get; set; }  // 0=Sunday … 6=Saturday

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [Range(5, 120)]
        public int SlotDurationMinutes { get; set; } = 15;

        [Range(1, 200)]
        public int MaxPatientsPerSlot { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(DoctorId))]
        public Doctor Doctor { get; set; }
    }

    // ─────────────────────────────────────────────
    // Public Appointment Request (submitted from public booking form)
    // ─────────────────────────────────────────────
    public class PublicAppointmentRequest
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string PatientName { get; set; }

        [Required, MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(200), EmailAddress]
        public string? Email { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(10)]
        public string? Age { get; set; }

        public int DoctorId { get; set; }

        public DateTime PreferredDate { get; set; }

        public TimeSpan PreferredTime { get; set; }

        [MaxLength(500)]
        public string? Symptoms { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";  // Pending | Confirmed | Cancelled

        [MaxLength(300)]
        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        // Navigation
        [ForeignKey(nameof(DoctorId))]
        public Doctor Doctor { get; set; }
    }
}
