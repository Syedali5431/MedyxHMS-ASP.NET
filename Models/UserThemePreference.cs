namespace MedyxHMS.Models
{
    public class UserThemePreference
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ThemeId { get; set; } = "sunflower";
        public DateTime PreferenceSince { get; set; } = DateTime.UtcNow;
        public bool IsDefault { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
