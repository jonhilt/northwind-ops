using Microsoft.AspNetCore.Components;

namespace NorthwindOps.Components.Learn;

/// <summary>Which features have a walkthrough: every page routed at /learn/{id}.</summary>
public static class LearnPages
{
    static readonly HashSet<string> Ids = typeof(LearnPages).Assembly.GetTypes()
        .SelectMany(t => t.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>())
        .Select(r => r.Template)
        .Where(t => t.StartsWith("/learn/", StringComparison.Ordinal))
        .Select(t => t["/learn/".Length..])
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool Has(string id) => Ids.Contains(id);

    /// <summary>Where the guide sends a reader for a feature: its walkthrough if it has one, else its guide steps.</summary>
    public static string UrlFor(string id) => Has(id) ? $"learn/{id}" : $"guide/{id}";
}
