using System.Globalization;
using System.Text;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Code 128 (subset B) barcode as inline SVG, generated on the server so labels print without internet access.
    /// Subset B covers printable ASCII (space to ~), which includes accession and order numbers.
    /// </summary>
    public static class Code128Barcode
    {
        // Bar/space widths (in modules) for symbol values 0–106; 106 is the stop pattern (7 elements).
        private static readonly string[] Patterns =
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
        };

        private const int StartB = 104;
        private const int Stop = 106;
        private const int QuietZone = 10;

        public static bool CanEncode(string text) =>
            !string.IsNullOrEmpty(text) && text.All(c => c >= 32 && c <= 126);

        /// <summary>Symbol values including start, checksum and stop.</summary>
        public static List<int> Encode(string text)
        {
            if (!CanEncode(text))
            {
                throw new ArgumentException("Only printable ASCII characters can be encoded.", nameof(text));
            }

            var values = new List<int> { StartB };
            var checksum = StartB;
            for (var i = 0; i < text.Length; i++)
            {
                var value = text[i] - 32;
                values.Add(value);
                checksum += value * (i + 1);
            }

            values.Add(checksum % 103);
            values.Add(Stop);
            return values;
        }

        /// <summary>Bar/space module widths of the whole symbol (without quiet zones), starting with a bar.</summary>
        public static List<int> ModuleWidths(string text) =>
            Encode(text).SelectMany(v => Patterns[v].Select(ch => ch - '0')).ToList();

        /// <summary>SVG with crisp bars; height and module width in the given unit (e.g. "mm").</summary>
        public static string Svg(string text, double moduleWidth = 0.25, double height = 10, string unit = "mm", bool showText = false)
        {
            var widths = ModuleWidths(text);
            var totalModules = widths.Sum() + 2 * QuietZone;
            var textHeight = showText ? 3.0 : 0;
            var w = totalModules * moduleWidth;
            var h = height + textHeight;
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w.ToString("0.###", inv)}{unit}\" height=\"{h.ToString("0.###", inv)}{unit}\" viewBox=\"0 0 {totalModules} {(height + textHeight) / moduleWidth:0.###}\" shape-rendering=\"crispEdges\" role=\"img\" aria-label=\"Barcode {System.Net.WebUtility.HtmlEncode(text)}\">");
            sb.Append($"<rect width=\"{totalModules}\" height=\"{((height + textHeight) / moduleWidth).ToString("0.###", inv)}\" fill=\"#fff\"/>");
            var x = QuietZone;
            var barHeight = (height / moduleWidth).ToString("0.###", inv);
            for (var i = 0; i < widths.Count; i++)
            {
                if (i % 2 == 0)
                {
                    sb.Append($"<rect x=\"{x}\" y=\"0\" width=\"{widths[i]}\" height=\"{barHeight}\" fill=\"#000\"/>");
                }

                x += widths[i];
            }

            if (showText)
            {
                var fontSize = (2.6 / moduleWidth).ToString("0.###", inv);
                var y = ((height + 2.6) / moduleWidth).ToString("0.###", inv);
                sb.Append($"<text x=\"{totalModules / 2}\" y=\"{y}\" font-family=\"monospace\" font-size=\"{fontSize}\" text-anchor=\"middle\">{System.Net.WebUtility.HtmlEncode(text)}</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
