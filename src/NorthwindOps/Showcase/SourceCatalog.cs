using System.Reflection;
using System.Text.RegularExpressions;

namespace NorthwindOps.Showcase;

/// <summary>A source file as X-ray shows it: marker lines removed, the feature's lines flagged for highlighting.</summary>
public sealed record SourceView(string Path, string Language, string Code, string HighlightedLines);

/// <summary>
/// Serves the app's own source code, which is embedded in the assembly at build time.
/// Code that demonstrates a feature is wrapped in marker comments:
/// <c>@* xray:feature-id *@ ... @* /xray:feature-id *@</c> in Razor, or <c>// xray:feature-id</c> ... <c>// /xray:feature-id</c> in C# and JS.
/// </summary>
public sealed partial class SourceCatalog
{
    const string ResourcePrefix = "source/";

    readonly Assembly assembly;
    readonly Dictionary<string, string> resourceByPath;

    public SourceCatalog() : this(typeof(SourceCatalog).Assembly) { }

    public SourceCatalog(Assembly assembly)
    {
        this.assembly = assembly;
        resourceByPath = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .ToDictionary(name => name[ResourcePrefix.Length..].Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> Paths => resourceByPath.Keys;

    public string? Read(string path)
    {
        if (!resourceByPath.TryGetValue(path, out var resource))
        {
            return null;
        }
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        return reader.ReadToEnd();
    }

    /// <summary>Returns the file with every X-ray marker stripped and the given feature's regions flagged as highlighted lines.</summary>
    public SourceView? View(string path, string featureId)
        => Read(path) is { } text ? Render(path, text, featureId) : null;

    public static SourceView Render(string path, string text, string featureId)
    {
        var kept = new List<string>();
        var ranges = new List<string>();
        int? start = null;
        foreach (var line in text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n'))
        {
            var marker = Marker().Match(line);
            if (!marker.Success)
            {
                kept.Add(line);
                continue;
            }
            if (!string.Equals(marker.Groups["id"].Value, featureId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (marker.Groups["end"].Success)
            {
                if (start is int first && kept.Count >= first)
                {
                    ranges.Add(first == kept.Count ? $"{first}" : $"{first}-{kept.Count}");
                }
                start = null;
            }
            else
            {
                start = kept.Count + 1;
            }
        }

        return new SourceView(path, LanguageFor(path), string.Join('\n', kept), string.Join(',', ranges));
    }

    /// <summary>Feature ids that have marked regions in the given file.</summary>
    public IEnumerable<string> MarkedFeatures(string path)
        => (Read(path) ?? "").Split('\n')
            .Select(line => Marker().Match(line))
            .Where(m => m.Success && !m.Groups["end"].Success)
            .Select(m => m.Groups["id"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    static string LanguageFor(string path) => System.IO.Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".js" => "javascript",
        ".css" => "css",
        ".json" => "json",
        _ => "none",
    };

    [GeneratedRegex(@"^\s*(?:@\*|//|/\*|<!--)\s*(?<end>/)?xray:(?<id>[a-z0-9-]+)\s*(?:\*@|\*/|-->)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Marker();
}
