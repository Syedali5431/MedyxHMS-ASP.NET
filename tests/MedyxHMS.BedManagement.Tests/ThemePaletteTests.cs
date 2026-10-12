using System.Text.RegularExpressions;
using MedyxHMS.Services.Implementations;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

// Every workspace theme gets readable colours: text, links, muted text, status colours, button captions and sidebar
// text reach a contrast of 4.5:1 on the backgrounds they are used on.
public class ThemePaletteTests
{
    public static IEnumerable<object[]> Themes() =>
        Directory.GetFiles(Path.Combine(RepositoryRoot(), "wwwroot", "css", "themes"), "*.css")
            .Where(f => !Path.GetFileName(f).StartsWith('_'))
            .Select(f => new object[] { Path.GetFileNameWithoutExtension(f) });

    [Theory]
    [MemberData(nameof(Themes))]
    public void ComputedColours_AreReadable(string theme)
    {
        var themeCss = File.ReadAllText(Path.Combine(RepositoryRoot(), "wwwroot", "css", "themes", theme + ".css"));
        var added = ThemePalette.BuildAdditions(themeCss, string.Empty);
        var tokens = ThemePalette.ReadColours(added);
        var theirs = ThemePalette.ReadColours(themeCss);
        ThemePalette.Rgb Theme(string name, string fallback) => theirs.TryGetValue(name, out var c) ? c : ThemePalette.Parse(fallback)!.Value;

        var card = Theme("--hms-card-bg", "#ffffff");
        var page = Theme("--hms-bg", "#f5f7fb");
        foreach (var token in new[] { "--hms-text", "--hms-text-muted", "--hms-primary-text", "--hms-accent-text", "--hms-success-text", "--hms-danger-text", "--hms-warning-text", "--hms-info-text" })
        {
            Assert.True(ThemePalette.Contrast(tokens[token], card) >= 4.5, $"{theme}: {token} {tokens[token]} on card {card}");
            Assert.True(ThemePalette.Contrast(tokens[token], page) >= 4.5, $"{theme}: {token} {tokens[token]} on page {page}");
        }
        foreach (var name in new[] { "primary", "accent" })
        {
            var fill = tokens[$"--hms-btn-{name}-bg"];
            var caption = tokens[$"--hms-on-{name}"];
            Assert.True(ThemePalette.Contrast(caption, fill) >= 4.5, $"{theme}: {name} button caption {caption} on {fill}");
            Assert.True(ThemePalette.Contrast(caption, tokens[$"--hms-btn-{name}-hover-bg"]) >= 4.5, $"{theme}: {name} button hover");
        }
        var sidebar = Theme("--sb-bg", "#15202b");
        Assert.True(ThemePalette.Contrast(tokens["--sb-text"], sidebar) >= 4.5, $"{theme}: sidebar text");
        Assert.True(ThemePalette.Contrast(tokens["--sb-text-muted"], sidebar) >= 4.5, $"{theme}: sidebar labels");
    }

    [Fact]
    public void DarkThemes_GetTheDarkPageRules()
    {
        var root = RepositoryRoot();
        var darkRules = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "themes", "_dark-pages.css"));
        Assert.Contains("color-scheme: dark", ThemePalette.BuildAdditions(File.ReadAllText(Path.Combine(root, "wwwroot", "css", "themes", "dark.css")), darkRules));
        Assert.Contains(".table-light", ThemePalette.BuildAdditions(File.ReadAllText(Path.Combine(root, "wwwroot", "css", "themes", "midnight.css")), darkRules));
        Assert.DoesNotContain(".table-light", ThemePalette.BuildAdditions(File.ReadAllText(Path.Combine(root, "wwwroot", "css", "themes", "sunflower.css")), darkRules));
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MedyxHMS.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("MedyxHMS.csproj not found above the test folder.");
    }
}
