using System.Text;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The browser-options guide entry: the .NET 10 Blazor.start() page runs live beside BrowserOptions.</summary>
public partial class BrowserOptionsDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    const string Net10 = "/demo/browser-options/pick-list/net10";

    [Fact]
    public async Task The_dotnet10_page_starts_Blazor_itself_with_misspelt_names()
    {
        var response = await app.CreateClient().GetAsync(Net10);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("autostart=\"false\"", html);
        Assert.Contains("maxRetry: 20,", html);
        Assert.Contains("retryIntervalMs: 2000", html);
        Assert.DoesNotContain("maxRetries", html);
        // Its own page, so this app's App.razor (which starts Blazor automatically) isn't in it.
        Assert.Single(Regex.Matches(html, "_framework/blazor.web.js"));
        Assert.Contains("data-demo-body", html);
        Assert.Contains("data-sent", html);
        Assert.Contains("data-gaps", html);
        // Its two ways are full page loads: Blazor.start() runs once per page.
        Assert.Contains("data-enhance-nav=\"false\"", html);

        // The inline script runs under the page's policy, by its nonce.
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        var nonce = Nonce().Match(policy).Groups[1].Value;
        Assert.Contains($"<script nonce=\"{nonce}\" data-start", html);
        Assert.DoesNotContain("unsafe-inline", policy);
    }

    [Fact]
    public async Task The_dotnet10_page_from_config_renders_a_script_that_cannot_parse()
    {
        var html = await app.CreateClient().GetStringAsync(Net10 + "?from=config");

        Assert.Matches(@"maxRetries: ,\s+retryIntervalMilliseconds: 2000", html);
    }

    [Fact]
    public async Task The_dotnet11_page_sends_its_options_in_the_page()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/browser-options/pick-list");
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(Configuration().Match(html).Groups[1].Value));

        Assert.Contains("\"reconnectionMaxRetries\":20", json);
        Assert.Contains("\"reconnectionRetryIntervalMilliseconds\":2000", json);
        Assert.DoesNotContain("Blazor.start", html);
        Assert.Contains("data-sent", html);
        Assert.Contains("data-gaps", html);
    }

    [GeneratedRegex("'nonce-([^']+)'")]
    private static partial Regex Nonce();

    [GeneratedRegex("<!--Blazor-Configuration:([A-Za-z0-9+/=]+)-->")]
    private static partial Regex Configuration();
}
