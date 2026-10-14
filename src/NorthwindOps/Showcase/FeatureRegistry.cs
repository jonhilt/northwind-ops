using System.Reflection;
using Markdig;

namespace NorthwindOps.Showcase;

/// <summary>A .NET 11 feature that X-ray can point at, loaded from Showcase/Features/{id}.md.</summary>
public sealed record Feature(
    string Id,
    string Title,
    string Badge,
    string Release,
    string Mode,
    string Summary,
    string? Docs,
    IReadOnlyList<string> Sources,
    string TryItHtml,
    string BeforeHtml,
    string HowItWorksHtml);

/// <summary>
/// Loads every feature description embedded in the assembly. Each file has a front matter
/// block of "key: value" lines, then "## Try it", "## The .NET 10 way" and "## How it works" sections.
/// </summary>
public sealed class FeatureRegistry
{
    const string ResourcePrefix = "features/";

    static readonly MarkdownPipeline Markdown = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    readonly Dictionary<string, Feature> features;

    public FeatureRegistry() : this(typeof(FeatureRegistry).Assembly) { }

    public FeatureRegistry(Assembly assembly)
    {
        features = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".md", StringComparison.Ordinal))
            .Select(name => Parse(name, Read(assembly, name)))
            .ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<Feature> All => features.Values;

    public Feature? Find(string id) => features.GetValueOrDefault(id);

    static Feature Parse(string resourceName, string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        if (lines[0] != "---")
        {
            throw new InvalidDataException($"{resourceName} must start with a '---' front matter block.");
        }

        var end = Array.IndexOf(lines, "---", 1);
        var meta = lines[1..end]
            .Where(line => line.Contains(':'))
            .ToDictionary(line => line[..line.IndexOf(':')].Trim(), line => line[(line.IndexOf(':') + 1)..].Trim());

        var sections = SplitSections(lines[(end + 1)..]);

        string Required(string key) => meta.TryGetValue(key, out var value) && value.Length > 0
            ? value
            : throw new InvalidDataException($"{resourceName} is missing '{key}' in its front matter.");

        return new Feature(
            Id: Required("id"),
            Title: Required("title"),
            Badge: Required("badge"),
            Release: Required("release"),
            Mode: Required("mode"),
            Summary: Required("summary"),
            Docs: meta.GetValueOrDefault("docs"),
            Sources: Required("sources").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            TryItHtml: Html(sections, "Try it"),
            BeforeHtml: Html(sections, "The .NET 10 way"),
            HowItWorksHtml: Html(sections, "How it works"));
    }

    static Dictionary<string, string> SplitSections(string[] lines)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? heading = null;
        var body = new List<string>();
        foreach (var line in lines.Append("## "))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (heading is not null)
                {
                    sections[heading] = string.Join('\n', body).Trim();
                }
                heading = line[3..].Trim();
                body.Clear();
            }
            else
            {
                body.Add(line);
            }
        }
        return sections;
    }

    static string Html(Dictionary<string, string> sections, string heading)
        => sections.TryGetValue(heading, out var markdown) ? Markdig.Markdown.ToHtml(markdown, Markdown) : "";

    static string Read(Assembly assembly, string name)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
        return reader.ReadToEnd();
    }
}
