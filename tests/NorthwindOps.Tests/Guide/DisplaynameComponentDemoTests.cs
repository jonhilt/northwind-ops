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
    public async Task In_English_both_grids_show_the_model_names()
    {
        Assert.Equal(["Supplier code", "Company name", "Main contact"],
            await HeadersAsync("/demo/displayname/suppliers/net10"));
        Assert.Equal(["Supplier code", "Company name", "Main contact"],
            await HeadersAsync("/demo/displayname/suppliers"));
    }

    [Fact]
    public async Task Only_DisplayName_headers_follow_the_model_into_German()
    {
        Assert.Equal(["Supplier code", "Company name", "Main contact"],
            await HeadersAsync("/demo/displayname/suppliers/net10?culture=de-DE"));
        Assert.Equal(["Lieferantencode", "Firmenname", "Hauptansprechpartner"],
            await HeadersAsync("/demo/displayname/suppliers?culture=de-DE"));
    }
}
