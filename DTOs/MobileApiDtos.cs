using System.Text.Json.Serialization;

namespace MedyxHMS.DTOs
{
    public class MobileApiV1AppResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("site_url")]
        public string SiteUrl { get; set; } = string.Empty;

        [JsonPropertyName("app_logo")]
        public string AppLogo { get; set; } = string.Empty;

        [JsonPropertyName("app_primary_color_code")]
        public string AppPrimaryColorCode { get; set; } = string.Empty;

        [JsonPropertyName("app_secondary_color_code")]
        public string AppSecondaryColorCode { get; set; } = string.Empty;

        [JsonPropertyName("lang_code")]
        public string LangCode { get; set; } = string.Empty;
    }

    public class MobileApiV2ConfigResponse
    {
        public string ApiVersion { get; set; } = "v2";
        public string BaseUrl { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public string AppLogoUrl { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = string.Empty;
        public string SecondaryColor { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = "en";
        public List<string> SupportedLanguages { get; set; } = new();
        public MobileApiV2Capabilities Capabilities { get; set; } = new();
    }

    public class MobileApiV2Capabilities
    {
        public bool PatientPortalEnabled { get; set; }
        public bool AppointmentSystemEnabled { get; set; }
        public bool BillingModuleEnabled { get; set; }
        public bool PublicWebsiteEnabled { get; set; }
        public bool MobileApiEnabled { get; set; }
    }
}