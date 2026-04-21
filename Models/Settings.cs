namespace MedyxHMS.Models
{
    public class Setting
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Type { get; set; } // string, int, bool, json
        public string Category { get; set; } // General, Email, SMS, Payment, etc.
        public string Description { get; set; }
        public bool IsSystem { get; set; } = false; // System settings cannot be deleted
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class Language
    {
        public int Id { get; set; }
        public string Code { get; set; } // en, es, fr, ar, etc.
        public string Name { get; set; } // English, Spanish, French, Arabic
        public string NativeName { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; } // CREATE, UPDATE, DELETE, LOGIN, etc.
        public string EntityName { get; set; } // Patient, Appointment, Bill, etc.
        public string EntityId { get; set; }
        public string OldValues { get; set; } // JSON of old values
        public string NewValues { get; set; } // JSON of new values
        public string Details
        {
            get => NewValues;
            set => NewValues = value;
        }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; }
    }

    // Configuration classes for strongly-typed settings
    public class HospitalSettings
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string DefaultLanguage { get; set; }
        public List<string> SupportedLanguages { get; set; }
        public string TimeZone { get; set; }
        public string Currency { get; set; }
        public string DateFormat { get; set; }
        public bool EnableAuditLogging { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public bool EnableSMSNotifications { get; set; }
        public string FileUploadPath { get; set; }
        public int MaxFileSizeMB { get; set; }
        public List<string> AllowedFileTypes { get; set; }
    }

    public class FeatureToggles
    {
        public bool ChatbotEnabled { get; set; }
        public bool PatientPortal { get; set; }
        public bool AppointmentSystem { get; set; }
        public bool BillingModule { get; set; }
        public bool OPDModule { get; set; }
        public bool IPDModule { get; set; }
        public bool PharmacyModule { get; set; }
        public bool LabModule { get; set; }
        public bool RadiologyModule { get; set; }
        public bool HRModule { get; set; }
        public bool PayrollModule { get; set; }
        public bool InventoryModule { get; set; }
        public bool ReportsModule { get; set; }
        public bool PublicWebsite { get; set; }
        public bool MobileAPI { get; set; }
    }
}