using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NorthwindOps.Tests;

/// <summary>
/// The session entry's two sides: .NET 10 hidden fields lose the draft on the next GET and send
/// whatever is posted; the .NET 11 session property keeps it on the server until it's sent.
/// </summary>
public partial class SupplyParameterFromSessionDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    const string Before = "/demo/session/restock/net10";
    const string After = "/demo/session/restock";

    // Its own cookie jar, so its own session. Redirects are checked, not followed.
    readonly HttpClient browser = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Hidden_fields_carry_the_draft_until_the_next_get()
    {
        var page = await browser.GetStringAsync(Before);
        var (first, second) = FirstTwoProducts(page);

        var review = await Read(await Post(Before, page, "restock-lines", ($"Packs[{first}]", "5"), ($"Packs[{second}]", "7")));
        Assert.Contains("2. Review", review);
        Assert.Contains($"""name="Draft[{first}]" value="5" """.TrimEnd(), review);

        // A refresh or a link is a GET: the draft is gone.
        var refreshed = WithoutScopes(await browser.GetStringAsync(Before));
        Assert.Contains("1. Quantities", refreshed);
        Assert.DoesNotContain("name=\"Draft[", refreshed);
    }

    [Fact]
    public async Task Hidden_fields_send_a_tampered_quantity_past_the_cap()
    {
        var page = await browser.GetStringAsync(Before);
        var (first, _) = FirstTwoProducts(page);
        var review = await Read(await Post(Before, page, "restock-lines", ($"Packs[{first}]", "5")));

        var sent = WebUtility.HtmlDecode(await Read(await Post(Before, review, "restock-send", ($"Draft[{first}]", "9999"))));

        Assert.Contains("Sent to the supplier: 9999 packs of", sent);

        // The demo's own button posts the same edit, so the reader needn't open DevTools.
        review = await Read(await Post(Before, page, "restock-lines", ($"Packs[{first}]", "5")));
        Assert.Contains($"""name="EditedDraft[{first}]" value="9999" """.TrimEnd(), review);
        var edited = WebUtility.HtmlDecode(await Read(await Post(Before, review, "restock-send-edited", ($"EditedDraft[{first}]", "9999"))));
        Assert.Contains("Sent to the supplier: 9999 packs of", edited);
    }

    [Fact]
    public async Task Session_keeps_the_draft_across_requests_until_it_is_sent()
    {
        var page = await browser.GetStringAsync(After);
        var (first, second) = FirstTwoProducts(page);

        var response = await Post(After, page, "restock-lines", ($"Packs[{first}]", "5"), ($"Packs[{second}]", "7"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/demo/session/restock/review", response.Headers.Location!.AbsolutePath);

        // A refresh of the review, and the step 1 page, both find the draft on the server.
        var review = WebUtility.HtmlDecode(await browser.GetStringAsync("/demo/session/restock/review"));
        Assert.Contains("5 packs of", review);
        var lines = await browser.GetStringAsync(After);
        Assert.Contains($"""name="Packs[{first}]" value="5" """.TrimEnd(), lines);
        Assert.DoesNotContain("type=\"hidden\" name=\"Draft", lines);

        // Send sets the property to null, which removes the key.
        var sent = await Post("/demo/session/restock/review", review, "restock-send");
        Assert.Equal(HttpStatusCode.Redirect, sent.StatusCode);
        var after = WithoutScopes(await browser.GetStringAsync("/demo/session/restock/review?sent=true"));
        Assert.Contains("Sent to the supplier", after);
        Assert.DoesNotContain("value=\"5\"", await browser.GetStringAsync(After));
    }

    [Fact]
    public async Task Tampered_send_is_flagged_over_the_cap()
    {
        var page = await browser.GetStringAsync(Before);
        var (first, _) = FirstTwoProducts(page);
        var review = await Read(await Post(Before, page, "restock-lines", ($"Packs[{first}]", "5")));
        var honest = WebUtility.HtmlDecode(await Read(await Post(Before, review, "restock-send", ($"Draft[{first}]", "5"))));
        Assert.DoesNotContain("99-pack cap", honest);

        review = await Read(await Post(Before, page, "restock-lines", ($"Packs[{first}]", "5")));
        var tampered = WebUtility.HtmlDecode(await Read(await Post(Before, review, "restock-send", ($"Draft[{first}]", "9999"))));
        Assert.Contains("Over step 1's 99-pack cap", tampered);
    }

    [Fact]
    public async Task Session_by_hand_saves_and_loads_the_draft()
    {
        const string ByHand = "/demo/session/restock/by-hand";
        var page = await browser.GetStringAsync(ByHand);
        var (first, _) = FirstTwoProducts(page);

        var response = await Post(ByHand, page, "restock-by-hand", ($"Packs[{first}]", "6"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var again = await browser.GetStringAsync(ByHand);
        Assert.Contains($"""name="Packs[{first}]" value="6" """.TrimEnd(), again);
    }

    static (string, string) FirstTwoProducts(string page)
    {
        var ids = PacksInput().Matches(page).Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(4, ids.Count);
        return (ids[0], ids[1]);
    }

    /// <summary>Posts a Blazor SSR form the way a browser would, with the page's antiforgery token.</summary>
    async Task<HttpResponseMessage> Post(string url, string page, string formName, params (string Name, string Value)[] fields)
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new("_handler", formName),
            new("__RequestVerificationToken", WebUtility.HtmlDecode(AntiforgeryToken().Match(page).Groups[1].Value)),
        };
        values.AddRange(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(values) };
        request.Headers.Add("Origin", "http://localhost");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        return await browser.SendAsync(request);
    }

    static async Task<string> Read(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    static string WithoutScopes(string html) => ScopeAttribute().Replace(html, "");

    [GeneratedRegex("""name="Packs\[(\d+)\]" """)]
    private static partial Regex PacksInput();

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();

    [GeneratedRegex(""" b-[a-z0-9]{10}""")]
    private static partial Regex ScopeAttribute();
}
