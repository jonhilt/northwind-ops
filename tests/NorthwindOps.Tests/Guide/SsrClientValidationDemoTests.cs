using System.Net;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The client-validation entry's two forms: .NET 10's server-only checks beside .NET 11's browser rules.</summary>
public partial class SsrClientValidationDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    const string Before = "/demo/client-validation/suppliers/new/net10";
    const string After = "/demo/client-validation/suppliers/new";

    [Fact]
    public async Task Only_the_dotnet11_form_writes_its_rules_into_the_page()
    {
        var client = app.CreateClient();
        var before = await client.GetStringAsync(Before);
        var after = await client.GetStringAsync(After);

        // .NET 10's bare input, as the repro rendered it: no rules for the browser to check.
        Assert.Contains("""<input id="email" name="Input.Email" class="valid" />""", before);
        Assert.DoesNotContain("blazor-client-validation-data", before);

        var rules = WebUtility.HtmlDecode(Rules().Match(after).Groups[1].Value);
        Assert.Contains("\"name\":\"Input.Company\"", rules);
        Assert.Contains("\"name\":\"Input.Email\"", rules);
        Assert.Contains("The Email field is not a valid e-mail address.", rules);
        Assert.All([before, after], html => Assert.Contains("Posts to the server: <b", html));
    }

    [Theory]
    [InlineData(Before)]
    [InlineData(After)]
    public async Task A_post_that_gets_past_the_browser_is_checked_and_counted_by_the_server(string url)
    {
        var html = await Post(url, new() { ["Posts"] = "1", ["Input.Company"] = "", ["Input.Email"] = "sales@" });

        Assert.Contains("The Company field is required.", html);
        Assert.Contains("The Email field is not a valid e-mail address.", html);
        Assert.Matches(@"Posts to the server: <b[^>]*>1</b>", html);
        Assert.Contains("""name="Posts" value="2""", html);
    }

    async Task<string> Post(string url, Dictionary<string, string> fields)
    {
        var client = app.CreateClient(new() { HandleCookies = true });
        var form = await client.GetStringAsync(url);
        fields["_handler"] = "new-supplier";
        var token = Token().Match(form);
        if (token.Success) fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        var response = await client.PostAsync(url, new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [GeneratedRegex("""<blazor-client-validation-data data-rules="(.*?)">""", RegexOptions.Singleline)]
    private static partial Regex Rules();

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex Token();
}
