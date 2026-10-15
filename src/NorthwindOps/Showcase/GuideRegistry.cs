using System.Reflection;

namespace NorthwindOps.Showcase;

/// <summary>
/// One .NET 11 feature in the Field Guide, loaded from the front matter of Showcase/Guide/{id}.md: its card on
/// /guide, and the demos its walkthrough (/learn/{id}) runs.
/// </summary>
public sealed record GuideEntry(
    string Id,
    string Title,
    string Group,
    int Order,
    string Hook,
    string Mode,
    string DemoUrl,
    GuideBefore? Before);

/// <summary>
/// The same demo without the feature, the .NET 10 way, which the walkthrough shows failing first.
/// The labels name each side, like ".NET 10: a flag in the URL" and ".NET 11: TempData".
/// </summary>
public sealed record GuideBefore(string Url, string Label, string AfterLabel);

/// <summary>Loads the Field Guide entries embedded in the assembly. CLAUDE.md describes the format.</summary>
public sealed class GuideRegistry
{
    const string ResourcePrefix = "guide/";

    readonly List<GuideEntry> entries;

    public GuideRegistry() : this(typeof(GuideRegistry).Assembly) { }

    public GuideRegistry(Assembly assembly)
    {
        entries = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".md", StringComparison.Ordinal))
            .Select(name => Parse(name, Read(assembly, name)))
            .OrderBy(e => e.Order)
            .ThenBy(e => e.Title, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Every entry, in reading order.</summary>
    public IReadOnlyList<GuideEntry> All => entries;

    public GuideEntry? Find(string id) => entries.Find(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

    public static GuideEntry Parse(string resourceName, string text)
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

        string Required(string key) => meta.TryGetValue(key, out var value) && value.Length > 0
            ? value
            : throw new InvalidDataException($"{resourceName} is missing '{key}' in its front matter.");

        return new GuideEntry(
            Id: Required("id"),
            Title: Required("title"),
            Group: Required("group"),
            Order: int.Parse(Required("order")),
            Hook: Required("hook"),
            Mode: Required("mode"),
            DemoUrl: Required("demo"),
            Before: Before(meta, resourceName));
    }

    /// <summary>"before: demo/x/net10" and "labels: .NET 10: … | .NET 11: …", or neither.</summary>
    static GuideBefore? Before(Dictionary<string, string> meta, string resourceName)
    {
        if (!meta.TryGetValue("before", out var url))
        {
            return null;
        }
        var labels = meta.GetValueOrDefault("labels")?.Split('|', StringSplitOptions.TrimEntries);
        return labels is [{ Length: > 0 } before, { Length: > 0 } after]
            ? new GuideBefore(url, before, after)
            : throw new InvalidDataException($"{resourceName} has 'before' but no 'labels: … | …'.");
    }

    static string Read(Assembly assembly, string name)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
        return reader.ReadToEnd();
    }
}
