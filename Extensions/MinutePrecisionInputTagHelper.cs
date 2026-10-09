using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

// Purpose: Date-time and time fields are filled in to the minute. Browsers use the pre-filled value as the base for
// the minute steps, so a value with seconds (e.g. 08:11:52.229 from DateTime.Now) made normal times such as 17:00
// invalid ("nearest valid values are 16:59:52 and 17:00:52"). Runs after the built-in input tag helper.
namespace MedyxHMS.Extensions
{
    [HtmlTargetElement("input", Attributes = "asp-for")]
    public class MinutePrecisionInputTagHelper : TagHelper
    {
        public override int Order => 1000;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var type = output.Attributes["type"]?.Value?.ToString();
            if (type is not ("datetime-local" or "time") || output.Attributes.ContainsName("step")) return;
            var value = output.Attributes["value"]?.Value?.ToString();
            if (string.IsNullOrEmpty(value)) return;

            if (type == "datetime-local" && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                output.Attributes.SetAttribute("value", dateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
            else if (type == "time" && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var time))
                output.Attributes.SetAttribute("value", time.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
        }
    }
}
