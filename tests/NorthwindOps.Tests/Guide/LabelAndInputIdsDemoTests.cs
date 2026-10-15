using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The labels demo: hand-typed .NET 10 labels beside &lt;Label For&gt;, both posted empty.</summary>
public partial class LabelAndInputIdsDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Dotnet10_side_points_its_label_at_no_id_and_dotnet11_side_matches()
    {
        var client = app.CreateClient();
        var before = await client.GetStringAsync("/demo/labels/suppliers/new/net10");
        var after = await client.GetStringAsync("/demo/labels/suppliers/new");

        Assert.Contains("for=\"supplier-compnay\"", before);
        Assert.Contains("id=\"supplier-company\"", before);
        Assert.DoesNotContain("id=\"supplier-compnay\"", before);

        Assert.Matches(Tag("label", "for=\"Input_Company\""), after);
        Assert.Matches(Tag("input", "id=\"Input_Company\""), after);
        Assert.Matches(Tag("label", "for=\"Input_Email\""), after);
        Assert.Matches(Tag("input", "id=\"Input_Email\""), after);
    }

    [Theory]
    [InlineData("/net10", "Email")]
    [InlineData("", "Contact email")]
    public async Task Saving_empty_shows_the_display_name_in_messages_on_both_sides(string side, string emailLabel)
    {
        var client = app.CreateClient(new() { HandleCookies = true });
        var url = "/demo/labels/suppliers/new" + side;
        var form = await client.GetStringAsync(url);
        var token = System.Net.WebUtility.HtmlDecode(Token().Match(form).Groups[1].Value);

        var response = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "new-supplier",
            ["__RequestVerificationToken"] = token,
            ["Input.Company"] = "",
            ["Input.Email"] = "",
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(">The Company name field is required.<", html);
        Assert.Contains(">The Contact email field is required.<", html);
        Assert.Matches($@"<label[^>]*>\s*{Regex.Escape(emailLabel)}\s*(<input|</label>)", html);
    }

    static Regex Tag(string name, string attribute) => new($"<{name}[^>]*{Regex.Escape(attribute)}");

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex Token();
}
