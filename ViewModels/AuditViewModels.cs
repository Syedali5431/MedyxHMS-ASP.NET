using MedyxHMS.Models;

namespace MedyxHMS.ViewModels
{
    public class AuditLogIndexViewModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EntityType { get; set; }
        public int TotalLogs { get; set; }
    }

    public class AuditLogDetailsViewModel
    {
        public AuditLog AuditLog { get; set; }
        public string OldValuesFormatted { get; set; }
        public string NewValuesFormatted { get; set; }
    }

    public class AuditSummaryViewModel
    {
        public Dictionary<string, int> ActionSummary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalLogs { get; set; }
        public Dictionary<string, int> EntitiesAffected { get; set; }
        public Dictionary<string, int> UserActions { get; set; }
    }

    public class UserActionLogViewModel
    {
        public IEnumerable<UserActionLog> UserActionLogs { get; set; }
        public string UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LoginHistoryViewModel
    {
        public string UserId { get; set; }
        public IEnumerable<UserActionLog> LoginHistory { get; set; }
        public int TotalSessions { get; set; }
        public int FailedAttempts { get; set; }
    }

    public class FailedLoginViewModel
    {
        public IEnumerable<UserActionLog> FailedAttempts { get; set; }
        public DateTime StartDate { get; set; }
        public int TotalFailedAttempts { get; set; }
        public Dictionary<string, int> FailuresByUser { get; set; }
    }

    public class AuditFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EntityType { get; set; }
        public string UserId { get; set; }
        public string ActionType { get; set; }
        public List<string> EntityTypes { get; set; }
        public List<string> ActionTypes { get; set; }
    }
}
