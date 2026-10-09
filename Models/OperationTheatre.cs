using System.ComponentModel.DataAnnotations;

// Purpose: Operation theatre master (theatres of each hospital with opening hours and turnover time)
// and theatre block times (maintenance, deep cleaning, closure) used for OT availability and clash prevention.
namespace MedyxHMS.Models
{
    public class OperationTheatre : IHospitalScoped
    {
        public int Id { get; set; }
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Theatre code")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Location / floor")]
        public string Location { get; set; } = string.Empty;

        /// <summary>General, Cardiac, Orthopaedic, Neuro, Obstetric, Ophthalmic, Day care / minor, Emergency.</summary>
        [Required, StringLength(50)]
        [Display(Name = "Theatre type")]
        public string TheatreType { get; set; } = "General";

        [StringLength(500)]
        [Display(Name = "Equipment / features")]
        public string Features { get; set; } = string.Empty;

        [Display(Name = "Opens at")]
        public TimeSpan OpensAt { get; set; } = new TimeSpan(8, 0, 0);

        [Display(Name = "Closes at")]
        public TimeSpan ClosesAt { get; set; } = new TimeSpan(20, 0, 0);

        /// <summary>Emergency theatres can be booked at any time of day.</summary>
        [Display(Name = "Open 24 hours")]
        public bool Is24Hours { get; set; }

        /// <summary>Cleaning and set-up time kept free after every case.</summary>
        [Range(0, 240)]
        [Display(Name = "Turnover time (minutes)")]
        public int TurnoverMinutes { get; set; } = 30;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<OTBlock> Blocks { get; set; } = new List<OTBlock>();

        public string DisplayName => string.IsNullOrWhiteSpace(Name) || Name == Code ? Code : $"{Code} – {Name}";
    }

    /// <summary>A period in which a theatre cannot be booked (maintenance, deep cleaning, staff shortage, closure).</summary>
    public class OTBlock
    {
        public int Id { get; set; }
        public int OperationTheatreId { get; set; }
        public OperationTheatre Theatre { get; set; } = null!;

        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
