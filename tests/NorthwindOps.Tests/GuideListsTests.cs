using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>Checks the claims the "Grids &amp; lists" guide entries make about their demos.</summary>
public partial class GuideListsTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Csp_demo_refuses_inline_styles()
    {
        var response = await app.CreateClient().GetAsync("/demo/virtualize-csp/orders");

        var policy = string.Join("; ", response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("style-src 'self'", policy);
        Assert.DoesNotContain("unsafe-inline", policy);
        // The page replaces Blazor's own header, so it must keep the guide's frame allowed.
        Assert.Contains("frame-ancestors 'self'", policy);
    }

    [Fact]
    public async Task Scroll_to_item_demo_prerenders_around_the_linked_message()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/scroll-to-item/incident?message=240");

        var numbers = MessageNumber().Matches(html).Select(m => int.Parse(m.Groups[1].Value)).ToList();
        Assert.Contains(240, numbers);
        Assert.DoesNotContain(1, numbers);
    }

    [Fact]
    public async Task Row_click_demo_lists_eight_clickable_tickets()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/row-click/tickets");

        Assert.Equal(8, Regex.Count(html, """<tr[^>]*class="row-clickable"""));
    }

    [GeneratedRegex("""#(\d+)</header>""")]
    private static partial Regex MessageNumber();
}
