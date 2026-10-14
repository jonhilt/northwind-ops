using System.Globalization;

namespace NorthwindOps.Features.Suppliers;

/// <summary>
/// The Suppliers screen speaks English and German. The choice travels in the query string (?culture=de-DE),
/// which the request localization middleware for /suppliers reads (see Program.cs).
/// </summary>
public static class SupplierCultures
{
    public const string English = "en-US";
    public const string German = "de-DE";

    /// <summary>The value to put in ?culture= to stay in the current language, or null for the default (English).</summary>
    public static string? Current => CultureInfo.CurrentUICulture.Name == German ? German : null;

    /// <summary>Appends the current culture to a supplier page link.</summary>
    public static string Link(string url) => Current is { } culture
        ? $"{url}{(url.Contains('?') ? '&' : '?')}culture={culture}"
        : url;
}
