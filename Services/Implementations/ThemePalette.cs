using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// Purpose: Readable colours for every workspace theme. A theme only sets its brand colours (--hms-*, --sb-*);
// this works out text and button colours with enough contrast (WCAG AA, 4.5:1) for those colours, so text stays
// readable whatever theme is chosen – including dark themes and themes with pale primary or accent colours.
namespace MedyxHMS.Services.Implementations
{
    public static class ThemePalette
    {
        private const double TextContrast = 4.5;

        // Values of site.css :root, used when a theme does not set them.
        private static readonly Dictionary<string, string> Defaults = new(StringComparer.OrdinalIgnoreCase)
        {
            ["--hms-primary"] = "#0b5ed7", ["--hms-primary-dark"] = "#0a4eb4", ["--hms-accent"] = "#20c997",
            ["--hms-bg"] = "#f5f7fb", ["--hms-bg-start"] = "#ffffff", ["--hms-text"] = "#1f2937",
            ["--hms-card-bg"] = "#ffffff", ["--hms-input-bg"] = "#ffffff",
            ["--sb-bg"] = "#15202b", ["--sb-text"] = "#8899aa", ["--sb-text-muted"] = "#5a6a7a", ["--sb-text-hover"] = "#e8edf2",
        };

        /// <summary>The CSS added after a theme's own stylesheet: computed colour tokens, and the dark-page rules for dark themes.</summary>
        public static string BuildAdditions(string themeCss, string darkRulesCss)
        {
            var v = ReadColours(themeCss);
            Rgb Get(string name) => v.TryGetValue(name, out var c) ? c : Parse(Defaults[name])!.Value;

            var primary = Get("--hms-primary");
            var primaryDark = Get("--hms-primary-dark");
            var accent = Get("--hms-accent");
            var text = Get("--hms-text");
            var card = Get("--hms-card-bg");
            var page = Get("--hms-bg");
            var pageStart = Get("--hms-bg-start");
            var input = Get("--hms-input-bg");
            var surface = v.TryGetValue("--hms-surface-muted", out var s) ? s : Mix(primary, Parse("#f8fafc")!.Value, 0.06);
            var dark = Luminance(card) < 0.2 || Luminance(page) < 0.2;

            // Text sits on cards, muted panels and the page background: it must be readable on all of them.
            var backgrounds = new[] { card, surface, page, pageStart };
            var target = dark ? White : Black;

            var css = new StringBuilder();
            css.AppendLine();
            css.AppendLine("/* Readable colours computed for this theme (ThemePalette): contrast of at least 4.5:1 on its backgrounds. */");
            css.AppendLine(":root {");
            css.AppendLine($"    color-scheme: {(dark ? "dark" : "light")};");
            Token(css, "--hms-text", Readable(text, backgrounds, target));
            Token(css, "--hms-text-muted", Readable(Mix(text, card, 0.38), backgrounds, target));
            Token(css, "--hms-primary-text", Readable(primary, backgrounds, target));
            Token(css, "--hms-accent-text", Readable(accent, backgrounds, target));
            Token(css, "--hms-input-text", Readable(text, new[] { input }, target));
            // Bootstrap's status colours, darker on light themes and lighter on dark ones.
            Token(css, "--hms-success-text", Readable(Parse(dark ? "#75b798" : "#198754")!.Value, backgrounds, target));
            Token(css, "--hms-danger-text", Readable(Parse(dark ? "#ea868f" : "#dc3545")!.Value, backgrounds, target));
            Token(css, "--hms-warning-text", Readable(Parse(dark ? "#ffda6a" : "#b88a00")!.Value, backgrounds, target));
            Token(css, "--hms-info-text", Readable(Parse(dark ? "#6edff6" : "#0aa2c0")!.Value, backgrounds, target));
            // Buttons filled with the primary / accent colour: white or dark caption, or a deeper fill for white text.
            ButtonTokens(css, "primary", primary, primaryDark);
            ButtonTokens(css, "accent", accent, Mix(accent, Black, 0.15));

            // Sidebar: link and label colours readable on the sidebar colour; hover, active and brand-strip text on
            // their own (blended) backgrounds.
            var sbBg = Get("--sb-bg");
            var hoverBg = Mix(sbBg, White, 0.08);                               // link :hover – rgba(255,255,255,.08)
            var activeBg = Mix(sbBg, White, 0.15);                              // active link – rgba(255,255,255,.15)
            var expandedBg = Blend(ReadLayer(themeCss, "--sb-hover-bg"), sbBg); // open group – the theme's hover colour
            var brandBg = Mix(sbBg, Black, 0.2);                                // .sidebar-brand – rgba(0,0,0,.2)
            Token(css, "--sb-text", Readable(Get("--sb-text"), new[] { sbBg, hoverBg }, Towards(sbBg)));
            Token(css, "--sb-text-muted", Readable(Get("--sb-text-muted"), new[] { sbBg }, Towards(sbBg)));
            Token(css, "--sb-text-hover", Readable(Get("--sb-text-hover"), new[] { hoverBg }, Towards(hoverBg)));
            Token(css, "--sb-expanded-text", Readable(Get("--sb-text-hover"), new[] { expandedBg }, Towards(expandedBg)));
            // Active items: pure white or black keeps a safe margin on the tinted strip.
            Token(css, "--sb-active-text", Towards(activeBg));
            Token(css, "--sb-brand-text", Readable(Get("--sb-text-hover"), new[] { brandBg }, Towards(brandBg)));
            css.AppendLine("}");

            if (dark && !string.IsNullOrWhiteSpace(darkRulesCss))
            {
                css.AppendLine();
                css.AppendLine(darkRulesCss);
            }
            return css.ToString();
        }

        private static void ButtonTokens(StringBuilder css, string name, Rgb fill, Rgb hover)
        {
            // White caption when it is readable; a dark caption on pale fills; otherwise deepen the fill for white text.
            Rgb bg = fill, caption;
            if (Contrast(White, fill) >= TextContrast) caption = White;
            else if (Contrast(Black, fill) >= TextContrast && Luminance(fill) >= 0.3) caption = Ink;
            else { bg = Readable(fill, new[] { White }, Black); caption = White; }
            var hoverBg = caption.Equals(White) ? Readable(Mix(bg, Black, 0.12), new[] { White }, Black) : Mix(bg, Black, 0.08);
            if (!caption.Equals(White) && Contrast(caption, hoverBg) < TextContrast) hoverBg = bg;
            Token(css, $"--hms-btn-{name}-bg", bg);
            Token(css, $"--hms-btn-{name}-hover-bg", hoverBg);
            Token(css, $"--hms-on-{name}", caption);
        }

        private static void Token(StringBuilder css, string name, Rgb c) => css.AppendLine($"    {name}: {c};");

        // ── colour maths ─────────────────────────────────────────────

        public readonly record struct Rgb(double R, double G, double B)
        {
            public override string ToString() => $"#{Clamp(R):x2}{Clamp(G):x2}{Clamp(B):x2}";
            private static int Clamp(double x) => (int)Math.Round(Math.Clamp(x, 0, 255));
        }

        private static readonly Rgb White = new(255, 255, 255);
        private static readonly Rgb Black = new(0, 0, 0);
        private static readonly Rgb Ink = new(17, 24, 39);

        /// <summary>The colour, moved towards <paramref name="towards"/> in small steps until it reaches 4.5:1 on every background.</summary>
        public static Rgb Readable(Rgb colour, IEnumerable<Rgb> backgrounds, Rgb towards)
        {
            var bgs = backgrounds.ToList();
            for (var step = 0; step <= 20; step++)
            {
                var c = Mix(colour, towards, step * 0.05);
                if (bgs.All(b => Contrast(c, b) >= TextContrast)) return c;
            }
            return towards;
        }

        /// <summary>White or black, whichever contrasts more with the background.</summary>
        private static Rgb Towards(Rgb background) => Contrast(White, background) >= Contrast(Black, background) ? White : Black;

        /// <summary>A colour with its opacity (1 = opaque), from #rgb, #rrggbb, #rrggbbaa or rgb()/rgba(); null if the property is missing.</summary>
        private static (Rgb Colour, double Alpha)? ReadLayer(string css, string property)
        {
            var m = Regex.Match(css ?? string.Empty, Regex.Escape(property) + @"\s*:\s*([^;]+);");
            if (!m.Success) return null;
            var value = m.Groups[1].Value.Trim();
            var hex8 = Regex.Match(value, @"^#([0-9a-fA-F]{6})([0-9a-fA-F]{2})$");
            if (hex8.Success)
                return (Parse("#" + hex8.Groups[1].Value)!.Value, int.Parse(hex8.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0);
            var rgba = Regex.Match(value, @"rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+)\s*)?\)");
            if (rgba.Success)
            {
                double D(int i) => double.Parse(rgba.Groups[i].Value, CultureInfo.InvariantCulture);
                return (new Rgb(D(1), D(2), D(3)), rgba.Groups[4].Success ? D(4) : 1);
            }
            var hex = Parse(value);
            return hex.HasValue ? (hex.Value, 1) : null;
        }

        /// <summary>A (semi-transparent) layer over a background; the background itself when there is no layer.</summary>
        private static Rgb Blend((Rgb Colour, double Alpha)? layer, Rgb background) =>
            layer.HasValue ? Mix(background, layer.Value.Colour, Math.Clamp(layer.Value.Alpha, 0, 1)) : background;

        public static Rgb Mix(Rgb a, Rgb b, double weightOfB) =>
            new(a.R + (b.R - a.R) * weightOfB, a.G + (b.G - a.G) * weightOfB, a.B + (b.B - a.B) * weightOfB);

        public static double Luminance(Rgb c)
        {
            static double Channel(double v) { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        public static double Contrast(Rgb a, Rgb b)
        {
            var la = Luminance(a); var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        public static Rgb? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var m = Regex.Match(value, @"#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})\b");
            if (!m.Success) return null;
            var hex = m.Groups[1].Value;
            if (hex.Length == 3) hex = string.Concat(hex.Select(ch => $"{ch}{ch}"));
            int P(int i) => int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Rgb(P(0), P(2), P(4));
        }

        /// <summary>The colour custom properties of a theme stylesheet (first hex colour of each value; gradients use their first stop).</summary>
        public static Dictionary<string, Rgb> ReadColours(string css)
        {
            var result = new Dictionary<string, Rgb>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(css ?? string.Empty, @"(--(?:hms|sb)-[\w-]+)\s*:\s*([^;]+);"))
            {
                var c = Parse(m.Groups[2].Value);
                if (c.HasValue) result[m.Groups[1].Value] = c.Value;
            }
            return result;
        }
    }
}
