using System.Text.RegularExpressions;
using NorthwindOps.Showcase;

namespace NorthwindOps.Tests;

/// <summary>Keeps the X-ray write-ups, the source markers and the components that reference them in step.</summary>
public partial class XRayContentTests
{
    readonly FeatureRegistry registry = new();
    readonly SourceCatalog sources = new();

    [Fact]
    public void Every_feature_points_at_source_that_marks_it()
    {
        Assert.NotEmpty(registry.All);
        foreach (var feature in registry.All)
        {
            foreach (var path in feature.Sources)
            {
                var view = sources.View(path, feature.Id);
                Assert.True(view is not null, $"{feature.Id} lists {path}, which isn't embedded source.");
                Assert.True(view.HighlightedLines.Length > 0, $"{path} has no xray:{feature.Id} markers.");
            }
        }
    }

    [Fact]
    public void Every_View_on_GitHub_link_points_at_a_file_in_the_repo()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "NorthwindOps.slnx"))) root = root.Parent!;

        foreach (var path in registry.All.SelectMany(f => f.Sources).Distinct())
        {
            var url = NorthwindOps.Components.Showcase.XRayPanel.GitHubUrl(path);
            var file = url[url.IndexOf("/blob/main/", StringComparison.Ordinal)..]["/blob/main/".Length..];
            Assert.True(File.Exists(Path.Combine(root.FullName, file)), $"{path} links to {url}, which isn't in the repo.");
        }
    }

    [Fact]
    public void Every_XRay_component_refers_to_a_registered_feature()
    {
        var used = sources.Paths
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => XRayUsage().Matches(sources.Read(path)!).Select(m => (path, id: m.Groups["id"].Value)))
            .ToList();

        Assert.NotEmpty(used);
        foreach (var (path, id) in used)
        {
            Assert.True(registry.Find(id) is not null, $"{path} uses <XRay For=\"{id}\"> but there's no Showcase/Features/{id}.md.");
        }
    }

    [Fact]
    public void Every_feature_has_all_three_sections()
    {
        foreach (var feature in registry.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(feature.TryItHtml), $"{feature.Id} has no 'Try it' section.");
            Assert.False(string.IsNullOrWhiteSpace(feature.BeforeHtml), $"{feature.Id} has no 'The .NET 10 way' section.");
            Assert.False(string.IsNullOrWhiteSpace(feature.HowItWorksHtml), $"{feature.Id} has no 'How it works' section.");
        }
    }

    [Fact]
    public void Markers_are_stripped_and_their_lines_highlighted()
    {
        var text = """
            @page "/demo"
            @* xray:grid *@
            <QuickGrid>
            </QuickGrid>
            @* /xray:grid *@
            @* xray:other *@
            <p>Other feature</p>
            @* /xray:other *@
            // xray:grid
            var x = 1;
            // /xray:grid
            """;

        var view = SourceCatalog.Render("Components/Pages/Demo.razor", text, "grid");

        Assert.Equal("razor", view.Language);
        Assert.DoesNotContain("xray:", view.Code);
        Assert.Equal(["@page \"/demo\"", "<QuickGrid>", "</QuickGrid>", "<p>Other feature</p>", "var x = 1;"], view.Code.Split('\n'));
        Assert.Equal("2-3,5", view.HighlightedLines);
    }

    [GeneratedRegex("""<XRay\s+For="(?<id>[^"]+)""")]
    private static partial Regex XRayUsage();
}
