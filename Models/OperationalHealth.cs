// Purpose: Contains application code for OperationalHealth and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class SmtpHealthStatus
    {
        public bool IsConfigured { get; set; }

        public bool ConnectivityOk { get; set; }

        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public bool EnableSsl { get; set; }

        public string FromEmail { get; set; } = string.Empty;

        public List<string> Issues { get; set; } = new List<string>();

        public string Status => !IsConfigured
            ? "ConfigurationMissing"
            : ConnectivityOk
                ? "Healthy"
                : "ConnectivityFailed";
    }
}
