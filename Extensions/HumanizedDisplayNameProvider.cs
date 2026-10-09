using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

// Purpose: Forms show readable field labels and validation messages ("Date of birth", "Doctor") for properties
// that have no [Display]/[DisplayName] attribute, instead of the raw property name ("DateOfBirth", "DoctorId").
namespace MedyxHMS.Extensions
{
    public class HumanizedDisplayNameProvider : IDisplayMetadataProvider
    {
        private static readonly Dictionary<string, string> Acronyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Tpa"] = "TPA", ["Opd"] = "OPD", ["Ipd"] = "IPD", ["Ot"] = "OT", ["Url"] = "URL", ["Mfa"] = "MFA", ["Id"] = "ID",
            ["Pdf"] = "PDF", ["Sms"] = "SMS", ["Api"] = "API", ["Ip"] = "IP", ["Gst"] = "GST", ["Icu"] = "ICU", ["Cms"] = "CMS"
        };

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.Key.MetadataKind != ModelMetadataKind.Property || string.IsNullOrEmpty(context.Key.Name)) return;
            if (context.DisplayMetadata.DisplayName != null) return;
            if (context.Attributes.OfType<DisplayAttribute>().Any(a => a.Name != null) || context.Attributes.OfType<DisplayNameAttribute>().Any()) return;

            var text = Humanize(context.Key.Name);
            if (text != context.Key.Name)
            {
                context.DisplayMetadata.DisplayName = () => text;
            }
        }

        public static string Humanize(string name)
        {
            var words = Regex.Matches(name, "[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+").Select(m => m.Value).ToList();
            if (words.Count <= 1) return name;
            // "DoctorId" → "Doctor": the user picks the doctor, not an id.
            if (words.Count > 1 && words[^1] == "Id") words.RemoveAt(words.Count - 1);
            var parts = words.Select((w, i) => Acronyms.TryGetValue(w, out var a) ? a : i == 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w.ToLowerInvariant());
            return string.Join(' ', parts);
        }
    }
}
