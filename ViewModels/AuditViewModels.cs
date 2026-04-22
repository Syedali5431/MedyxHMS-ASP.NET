using MedyxHMS.Models;

// Purpose: Contains application code for AuditViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    public class AuditLogIndexViewModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int TotalLogs { get; set; }
    }

    public class AuditLogDetailsViewModel
    {
        public AuditLog AuditLog { get; set; } = new AuditLog();
        public string OldValuesFormatted { get; set; } = string.Empty;
        public string NewValuesFormatted { get; set; } = string.Empty;
    }

    public class AuditSummaryViewModel
    {
        public Dictionary<string, int> ActionSummary { get; set; } = new Dictionary<string, int>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalLogs { get; set; }
        public Dictionary<string, int> EntitiesAffected { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> UserActions { get; set; } = new Dictionary<string, int>();
    }

    public class UserActionLogViewModel
    {
        public IEnumerable<UserActionLog> UserActionLogs { get; set; } = new List<UserActionLog>();
        public string UserId { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LoginHistoryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public IEnumerable<UserActionLog> LoginHistory { get; set; } = new List<UserActionLog>();
        public int TotalSessions { get; set; }
        public int FailedAttempts { get; set; }
    }

    public class FailedLoginViewModel
    {
        public IEnumerable<UserActionLog> FailedAttempts { get; set; } = new List<UserActionLog>();
        public DateTime StartDate { get; set; }
        public int TotalFailedAttempts { get; set; }
        public Dictionary<string, int> FailuresByUser { get; set; } = new Dictionary<string, int>();
    }

    public class AuditFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public List<string> EntityTypes { get; set; } = new List<string>();
        public List<string> ActionTypes { get; set; } = new List<string>();
    }
}
