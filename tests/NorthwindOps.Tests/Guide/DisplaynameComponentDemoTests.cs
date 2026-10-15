using System.Net;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The DisplayName entry: typed Titles beside DisplayName headers, in English and German.</summary>
public partial class DisplaynameComponentDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [GeneratedRegex("<th[^>]*>(.*?)</th>", RegexOptions.Singleline)]
    private static partial Regex HeaderCell();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tag();

    async Task<string[]> HeadersAsync(string url)
    {
        var html = WebUtility.HtmlDecode(await app.CreateClient().GetStringAsync(url));
        return [.. HeaderCell().Matches(html).Select(m => Tag().Replace(m.Groups[1].Value, "").Trim())];
    }

    [Fact]
    public async Task In_English_only_the_untitled_column_misses_the_model()
    {
        Assert.Equal(["Supplier code", "Company name", "Contact"],
            await HeadersAsync("/demo/displayname/suppliers/net10"));
        Assert.Equal(["Supplier code", "Company name", "Main contact"],
            await HeadersAsync("/demo/displayname/suppliers"));
    }

    [Fact]
    public async Task Only_DisplayName_headers_follow_the_model_into_German()
    {
        Assert.Equal(["Supplier code", "Company name", "Contact"],
            await HeadersAsync("/demo/displayname/suppliers/net10?culture=de-DE"));
        Assert.Equal(["Lieferantencode", "Firmenname", "Hauptansprechpartner"],
            await HeadersAsync("/demo/displayname/suppliers?culture=de-DE"));
    }

    [Theory]
    [InlineData("/demo/displayname/suppliers/net10?culture=de-DE")]
    [InlineData("/demo/displayname/suppliers?culture=de-DE")]
    public async Task Both_sides_show_the_models_names_above_the_grid(string url)
    {
        var html = await app.CreateClient().GetStringAsync(url);
        Assert.Contains("data-form-labels", html);
        Assert.Matches("<b[^>]*>Hauptansprechpartner</b>", html);
    }
}
