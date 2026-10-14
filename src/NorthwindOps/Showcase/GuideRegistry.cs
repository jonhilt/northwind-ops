using System.Reflection;
using System.Text.RegularExpressions;
using Markdig;

namespace NorthwindOps.Showcase;

/// <summary>One .NET 11 feature as the Field Guide teaches it, loaded from Showcase/Guide/{id}.md.</summary>
public sealed record GuideEntry(
    string Id,
    string Title,
    string Group,
    int Order,
    string Hook,
    string Intro,
    string Mode,
    string DemoUrl,
    string? AppUrl,
    GuideProblem Problem,
    GuideTry Try,
    GuideCode Code,
    IReadOnlyList<GuideNote> GoodToKnow);

/// <summary>Step 1: the question a developer would recognise (as HTML), and how .NET 10 made you answer it.</summary>
public sealed record GuideProblem(string Question, string LeadHtml, IReadOnlyList<GuideOption> Options);

/// <summary>A .NET 10 workaround: its name, and markdown with the code and why it hurts.</summary>
public sealed record GuideOption(string Title, string Html);

/// <summary>Step 2: what to do in the demo, and what to watch for.</summary>
public sealed record GuideTry(string Question, string LeadHtml, IReadOnlyList<string> WatchHtml);

/// <summary>Step 3: the demo's own source files, each with a caption.</summary>
public sealed record GuideCode(string Question, string LeadHtml, IReadOnlyList<GuideCodeCard> Cards);

public sealed record GuideCodeCard(string Caption, string Path, string? NoteHtml);

/// <summary>A gotcha, folded away until the reader opens it. Title and body are HTML.</summary>
public sealed record GuideNote(string Title, string Html);

/// <summary>
/// Loads the Field Guide scripts embedded in the assembly. Each has front matter, then
/// "## The problem", "## See it work", "## How it's built" and "## Good to know" sections.
/// CLAUDE.md describes the format.
/// </summary>
public sealed partial class GuideRegistry
{
    const string ResourcePrefix = "guide/";

    static readonly MarkdownPipeline Markdown = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

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

    /// <summary>The entry after this one in reading order, if there is one.</summary>
    public GuideEntry? After(GuideEntry entry) => entries.SkipWhile(e => e != entry).Skip(1).FirstOrDefault();

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

        var sections = Split(lines[(end + 1)..], "## ");
        Section Need(string heading) => sections.Find(s => s.Heading == heading)
            ?? throw new InvalidDataException($"{resourceName} has no '## {heading}' section.");

        var problem = Need("The problem");
        var (problemQuestion, problemLead) = QuestionAndLead(problem);

        var tryIt = Need("See it work");
        var (tryQuestion, tryLeadAndSteps) = QuestionAndLead(tryIt);
        var watch = new List<string>();
        var tryLead = new List<string>();
        foreach (var line in tryLeadAndSteps.Split('\n'))
        {
            if (Step().Match(line) is { Success: true } step)
            {
                watch.Add(Inline(step.Groups[1].Value));
            }
            else
            {
                tryLead.Add(line);
            }
        }

        var code = Need("How it's built");
        var (codeQuestion, codeLead) = QuestionAndLead(code);

        var notes = sections.Find(s => s.Heading == "Good to know")?.Body.Split('\n')
            .Select(line => Note().Match(line))
            .Where(m => m.Success)
            .Select(m => new GuideNote(Inline(m.Groups["title"].Value), Inline(m.Groups["body"].Value)))
            .ToList() ?? [];

        return new GuideEntry(
            Id: Required("id"),
            Title: Required("title"),
            Group: Required("group"),
            Order: int.Parse(Required("order")),
            Hook: Required("hook"),
            Intro: Required("intro"),
            Mode: Required("mode"),
            DemoUrl: Required("demo"),
            AppUrl: meta.GetValueOrDefault("app"),
            Problem: new GuideProblem(Inline(problemQuestion), Html(problemLead),
                Split(problem.Body.Split('\n'), "### ").Where(s => s.Heading.Length > 0)
                    .Select(s => new GuideOption(s.Heading, Html(s.Body))).ToList()),
            Try: new GuideTry(Inline(tryQuestion), Html(string.Join('\n', tryLead)), watch),
            Code: new GuideCode(Inline(codeQuestion), Html(codeLead),
                Split(code.Body.Split('\n'), "### ").Where(s => s.Heading.Length > 0)
                    .Select(s => CodeCard(resourceName, s)).ToList()),
            GoodToKnow: notes);
    }

    static GuideCodeCard CodeCard(string resourceName, Section section)
    {
        var lines = section.Body.Split('\n');
        var path = lines.Select(line => SourceLine().Match(line)).FirstOrDefault(m => m.Success)?.Groups[1].Value
            ?? throw new InvalidDataException($"{resourceName}: '### {section.Heading}' needs a 'code: path' line.");
        var note = string.Join('\n', lines.Where(line => !SourceLine().IsMatch(line))).Trim();
        return new GuideCodeCard(section.Heading, path, note.Length > 0 ? Html(note) : null);
    }

    /// <summary>The first paragraph of a section, before any "###", is its question; the rest of that preamble is the lead.</summary>
    static (string Question, string Lead) QuestionAndLead(Section section)
    {
        var preamble = Split(section.Body.Split('\n'), "### ")[0].Body.Trim();
        var split = preamble.IndexOf("\n\n", StringComparison.Ordinal);
        return split < 0 ? (preamble, "") : (preamble[..split].Trim(), preamble[(split + 2)..].Trim());
    }

    sealed record Section(string Heading, string Body);

    /// <summary>Splits lines at headings with the given prefix. Text before the first heading has an empty heading.</summary>
    static List<Section> Split(string[] lines, string prefix)
    {
        var sections = new List<Section>();
        var heading = "";
        var body = new List<string>();
        var inFence = false;
        foreach (var line in lines)
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }
            if (!inFence && line.StartsWith(prefix, StringComparison.Ordinal))
            {
                sections.Add(new Section(heading, string.Join('\n', body).Trim()));
                heading = line[prefix.Length..].Trim();
                body.Clear();
            }
            else
            {
                body.Add(line);
            }
        }
        sections.Add(new Section(heading, string.Join('\n', body).Trim()));
        return sections;
    }

    static string Html(string markdown) => markdown.Length == 0 ? "" : Markdig.Markdown.ToHtml(markdown, Markdown);

    /// <summary>Markdown for a single line, without the surrounding paragraph.</summary>
    static string Inline(string markdown)
    {
        var html = Html(markdown).Trim();
        return html.StartsWith("<p>", StringComparison.Ordinal) && html.EndsWith("</p>", StringComparison.Ordinal) ? html[3..^4] : html;
    }

    static string Read(Assembly assembly, string name)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"^\d+\.\s+(.+)$")]
    private static partial Regex Step();

    [GeneratedRegex(@"^-\s+\*\*(?<title>[^*]+?)\.?\*\*\s*(?<body>.+)$")]
    private static partial Regex Note();

    [GeneratedRegex(@"^code:\s*(\S+)\s*$")]
    private static partial Regex SourceLine();
}
