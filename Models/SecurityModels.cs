// Purpose: Security hardening (ISO 27001-style controls): database backup history.
namespace MedyxHMS.Models
{
    /// <summary>One database backup run started from Security &amp; Backups (success or failure).</summary>
    public class DatabaseBackup
    {
        public int Id { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? SizeBytes { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        /// <summary>Running, Succeeded or Failed.</summary>
        public string Status { get; set; } = "Running";
        /// <summary>True when RESTORE VERIFYONLY confirmed the backup file is readable.</summary>
        public bool Verified { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
