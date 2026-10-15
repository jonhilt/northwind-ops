using System.Net;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

public partial class RelativeNavigationDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Dotnet10_tabs_point_at_the_app_root_and_none_is_active()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/relative-nav/net10/orders/12047/summary");

        Assert.Equal(["summary", "lines", "customer"], TabHrefs(html));
        Assert.DoesNotContain("class=\"active\"", html);
    }

    [Fact]
    public async Task Dotnet11_tabs_point_under_the_order_and_mark_the_current_one()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/relative-nav/orders/12047/summary");

        Assert.Equal(
            ["/demo/relative-nav/orders/12047/summary", "/demo/relative-nav/orders/12047/lines", "/demo/relative-nav/orders/12047/customer"],
            TabHrefs(html).Select(h => new Uri(h).AbsolutePath));
        Assert.Matches("<a href=\"[^\"]*/12047/summary\" class=\"active\"", html);
    }

    [Theory]
    [InlineData("/demo/relative-nav/orders/12047")]
    [InlineData("/demo/relative-nav/orders/12047/")]
    public async Task The_order_redirects_to_its_summary_tab_with_or_without_a_trailing_slash(string url)
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/demo/relative-nav/orders/12047/summary", response.Headers.Location!.AbsolutePath);
    }

    [Theory]
    [InlineData("/demo/relative-nav/orders/12047/lines")]
    [InlineData("/demo/relative-nav/net10/orders/12047/summary")]
    public async Task Each_side_says_which_address_it_is_at(string url)
    {
        var html = await app.CreateClient().GetStringAsync(url);

        Assert.Matches($"You are at <code[^>]*>{Regex.Escape(url)}</code>", html);
    }

    [Fact]
    public async Task The_dotnet10_lines_link_lands_on_a_page_that_is_not_there()
    {
        var response = await app.CreateClient().GetAsync("/lines");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    static List<string> TabHrefs(string html) =>
        [.. TabLink().Matches(Tabs().Match(html).Value).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value))];

    [GeneratedRegex("<nav class=\"tabs\"[^>]*>.*?</nav>", RegexOptions.Singleline)]
    private static partial Regex Tabs();

    [GeneratedRegex("<a href=\"([^\"]*)\"")]
    private static partial Regex TabLink();
}
