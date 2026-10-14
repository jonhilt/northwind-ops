using System.Net;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

public partial class PageTests(AppFactory app) : IClassFixture<AppFactory>
{
    readonly HttpClient client = app.CreateClient();

    [Fact]
    public async Task Orders_grid_renders_sort_and_paging_as_links_on_a_static_page()
    {
        var html = await client.GetStringAsync("/orders");

        Assert.Equal(1, CurrentPage(html));
        Assert.Contains("sort=Total&amp;direction=asc", html);
        Assert.Contains("page=2", html);
        // No interactive component on the page, so no circuit is ever started.
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [Fact]
    public async Task Orders_view_is_restored_from_the_url()
    {
        var html = await client.GetStringAsync("/orders?sort=Total&direction=desc&page=3");

        Assert.Equal(3, CurrentPage(html));
        var totals = Totals().Matches(html).Select(m => decimal.Parse(m.Groups[1].Value.Replace(",", ""))).ToList();
        Assert.Equal(15, totals.Count);
        Assert.Equal(totals.OrderByDescending(t => t), totals);
    }

    [Fact]
    public async Task Status_filter_shares_the_query_string_with_the_grid()
    {
        var html = await client.GetStringAsync("/orders?status=Cancelled");

        Assert.Contains("""<span class="pill cancelled">""", html);
        Assert.DoesNotContain("""<span class="pill delivered">""", html);
        Assert.Contains("status=Cancelled&amp;sort=Total", html);
    }

    [Fact]
    public async Task XRay_panel_is_an_html_fragment_with_highlighted_source()
    {
        var html = await client.GetStringAsync("/xray/quickgrid-url-paging/panel");

        Assert.StartsWith("<article", html.TrimStart());
        Assert.Contains("QuickGrid sorting and paging in the URL", html);
        Assert.Matches("""data-line="\d+-\d+""", html);
    }

    [Theory]
    [InlineData("/xray/no-such-feature/panel")]
    [InlineData("/xray/no-such-feature")]
    public async Task Unknown_features_are_not_found(string url)
    {
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Feature_page_works_without_javascript()
    {
        var html = await client.GetStringAsync("/xray/environment-view");

        Assert.Contains("The .NET 10 way", html);
        Assert.Contains("Components/Layout/MainLayout.razor", html);
    }

    [Fact]
    public async Task Environment_banner_shows_outside_production()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("class=\"env-banner\"", html);
        Assert.Contains("DEVELOPMENT", html);
    }

    static int CurrentPage(string html) => int.Parse(PageNumber().Match(html).Groups[1].Value);

    [GeneratedRegex(@"Page\s*<strong[^>]*>(\d+)</strong>")]
    private static partial Regex PageNumber();

    [GeneratedRegex(@"<td class=""[^""]*num[^""]*""[^>]*>\$([\d,]+\.\d\d)</td>")]
    private static partial Regex Totals();
}

public class ProductionTests
{
    [Fact]
    public async Task Environment_banner_is_not_rendered_in_production()
    {
        using var app = new AppFactory { Environment = "Production" };
        var html = await app.CreateClient().GetStringAsync("/");

        Assert.DoesNotContain("env-banner", html);
    }
}
