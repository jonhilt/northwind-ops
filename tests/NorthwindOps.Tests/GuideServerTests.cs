using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>Guide demos that set browser options with &lt;ConfigureBrowser&gt; keep them to their own pages.</summary>
public partial class GuideServerTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Demo_browser_options_stay_on_their_own_pages()
    {
        var client = app.CreateClient();

        var stockTake = await BrowserOptionsOf(client, "/demo/auto-pause/stock-take");
        var pickList = await BrowserOptionsOf(client, "/demo/browser-options/pick-list");
        var environment = await BrowserOptionsOf(client, "/demo/env-config/orders");
        // Visited after them: in RC2, a leak through WithBrowserOptions' shared object would show up here.
        var elsewhere = await BrowserOptionsOf(client, "/demo/anchor-mode/inbox");

        Assert.Equal(10_000, stockTake.GetProperty("server").GetProperty("autoPauseHiddenDelayMilliseconds").GetInt32());
        Assert.False(stockTake.GetProperty("server").TryGetProperty("reconnectionMaxRetries", out _));
        Assert.Equal(2_000, pickList.GetProperty("server").GetProperty("reconnectionRetryIntervalMilliseconds").GetInt32());
        Assert.False(pickList.GetProperty("server").TryGetProperty("autoPauseEnabled", out _));
        Assert.Contains("Orders__ApiUrl", environment.GetRawText());
        Assert.Equal("{}", elsewhere.GetProperty("server").GetRawText());
        Assert.DoesNotContain("Orders__ApiUrl", elsewhere.GetRawText());
    }

    static async Task<JsonElement> BrowserOptionsOf(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var base64 = BrowserConfiguration().Match(html).Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(base64), $"{url} has no Blazor-Configuration comment.");
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64))).RootElement;
    }

    [GeneratedRegex("<!--Blazor-Configuration:([A-Za-z0-9+/=]+)-->")]
    private static partial Regex BrowserConfiguration();
}
