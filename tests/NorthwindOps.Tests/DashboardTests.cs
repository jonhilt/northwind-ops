using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NorthwindOps.Features.Dashboard;

namespace NorthwindOps.Tests;

public partial class DashboardTests(AppFactory app) : IClassFixture<AppFactory>
{
    readonly HttpClient client = app.CreateClient();

    DashboardQueries Queries => app.Services.GetRequiredService<DashboardQueries>();

    [Fact]
    public async Task Kpi_tiles_come_from_the_cache_on_the_next_request()
    {
        var first = await client.GetStringAsync("/");
        var runs = Queries.KpiQueryRuns;
        var second = await client.GetStringAsync("/");

        Assert.True(runs >= 1);
        Assert.Equal(runs, Queries.KpiQueryRuns); // the slow query didn't run again
        Assert.Equal(Stamp(first, "data-kpis-as-of"), Stamp(second, "data-kpis-as-of"));
        Assert.NotEqual(Stamp(first, "data-page-rendered-at"), Stamp(second, "data-page-rendered-at"));
        // The [CacheBehavior(Rerender)] hole still ran, and knows the figures came from the cache, even straight after the miss.
        Assert.Contains("data-cache-state=\"hit\"", second);
    }

    [Fact]
    public async Task Kpi_tiles_show_the_figures()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("Orders today", html);
        Assert.Contains("Open orders", html);
        Assert.Contains("Revenue · last 30 days", html);
        Assert.Matches(@"pending · \d+ packing", html);
    }

    [Fact]
    public async Task Dashboard_is_static_with_three_interactive_server_islands()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("Static SSR · Interactive Server islands", html);
        Assert.Equal(3, ServerMarker().Matches(html).Count); // the feed and the two panels
        Assert.Contains("Live order feed", html);
        Assert.Contains("Connecting…", html); // prerendered before the circuit starts
    }

    [Fact]
    public async Task Panels_prerender_the_pages_static_tables_inside_the_interactive_component()
    {
        var html = await client.GetStringAsync("/");
        var topCustomers = await Queries.GetTopCustomersAsync(8);

        // The table is the page's own markup (Home.razor), rendered inside CollapsibleCard's body.
        var body = Regex.Match(html, """<div class="body" id="panel-top-customers"[^>]*>(.*?)</table>""", RegexOptions.Singleline);
        Assert.True(body.Success);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(topCustomers[0].Company), body.Groups[1].Value);
        Assert.Contains("id=\"panel-low-stock\"", html);
        Assert.Contains("aria-expanded=\"true\"", html); // always rendered expanded first, so the fragment is captured
    }

    [Fact]
    public async Task Top_customers_are_ordered_by_revenue()
    {
        var customers = await Queries.GetTopCustomersAsync(8);

        Assert.Equal(8, customers.Count);
        Assert.Equal(customers.OrderByDescending(c => c.Revenue).Select(c => c.Company), customers.Select(c => c.Company));
        Assert.All(customers, c => Assert.True(c.Orders > 0));
    }

    [Fact]
    public async Task Low_stock_lists_products_at_or_below_their_reorder_level_worst_first()
    {
        var products = await Queries.GetLowStockAsync(8);

        Assert.NotEmpty(products);
        Assert.All(products, p => Assert.True(p.UnitsInStock <= p.ReorderLevel, $"{p.Name} isn't low on stock"));
        var shortfalls = products.Select(p => p.UnitsInStock - p.ReorderLevel).ToList();
        Assert.Equal(shortfalls.Order(), shortfalls);
    }

    [Fact]
    public async Task Order_feed_starts_with_recent_history()
    {
        var feed = app.Services.GetRequiredService<OrderFeed>();
        for (var i = 0; i < 50 && feed.Count < OrderFeed.Backfill; i++)
        {
            await Task.Delay(100);
        }

        var events = feed.Read(0, feed.Count);
        Assert.True(events.Count >= OrderFeed.Backfill);
        Assert.Equal(events.OrderBy(e => e.Id).Select(e => e.Id), events.Select(e => e.Id));
        Assert.Equal(events.OrderBy(e => e.At).Select(e => e.At), events.Select(e => e.At));
    }

    [Fact]
    public void Order_feed_reads_clamp_to_what_exists()
    {
        var feed = new OrderFeed(null!, TimeProvider.System, NullLogger<OrderFeed>.Instance);
        for (var i = 1; i <= 5; i++)
        {
            feed.Add(new OrderEvent(i, DateTimeOffset.Now, OrderEventKind.NewOrder, $"Order #{i}", ""));
        }

        Assert.Equal([4L, 5L], feed.Read(3, 10).Select(e => e.Id));
        Assert.Empty(feed.Read(9, 3));
        Assert.Equal([1L, 2L], feed.Read(-4, 2).Select(e => e.Id));
    }

    [Fact]
    public void Simulated_orders_are_placed_then_packed_then_shipped()
    {
        var catalog = new FeedCatalog(
            Customers: ["Alfreds Futterkiste, Berlin", "Around the Horn, London"],
            Products: [new("Chai", 18m, 39, 10), new("Chang", 19m, 17, 25), new("Tofu", 23.25m, 35, 0)],
            NextOrderId: 20000,
            Pending: [19990, 19991],
            Packing: [19980]);
        var simulator = new OrderFeedSimulator(catalog, new Random(42));
        var start = DateTimeOffset.Now;

        var events = Enumerable.Range(0, 500).Select(i => simulator.Next(start.AddSeconds(i))).ToList();

        var placed = new HashSet<int>(catalog.Pending);
        var packed = new HashSet<int>(catalog.Packing);
        var shipped = new HashSet<int>();
        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case OrderEventKind.NewOrder:
                    Assert.True(placed.Add(OrderId(e)), $"{e.Subject} was placed twice");
                    Assert.True(e.Amount > 0);
                    break;
                case OrderEventKind.Packed:
                    Assert.True(placed.Contains(OrderId(e)), $"{e.Subject} was packed before it was placed");
                    Assert.True(packed.Add(OrderId(e)), $"{e.Subject} was packed twice");
                    break;
                case OrderEventKind.Shipped:
                    Assert.True(packed.Contains(OrderId(e)), $"{e.Subject} was shipped before it was packed");
                    Assert.True(shipped.Add(OrderId(e)), $"{e.Subject} was shipped twice");
                    break;
                case OrderEventKind.StockAlert:
                    var product = catalog.Products.Single(p => p.Name == e.Subject);
                    Assert.NotEqual(0, product.ReorderLevel); // products with no reorder level never alert
                    var left = StockLeft().Match(e.Detail);
                    Assert.True(e.Detail.StartsWith("Out of stock") || (left.Success && int.Parse(left.Groups[1].Value) <= product.ReorderLevel), e.Detail);
                    break;
            }
        }

        Assert.All(Enum.GetValues<OrderEventKind>(), kind => Assert.Contains(events, e => e.Kind == kind));
        Assert.Equal(Enumerable.Range(1, 500).Select(i => (long)i), events.Select(e => e.Id));
    }

    [Theory]
    [InlineData("cacheview")]
    [InlineData("virtualize-anchor-mode")]
    [InlineData("renderfragment-across-render-modes")]
    public async Task Dashboard_features_have_xray_panels_with_highlighted_source(string id)
    {
        var html = await client.GetStringAsync($"/xray/{id}/panel");

        Assert.StartsWith("<article", html.TrimStart());
        Assert.Matches("""data-line="\d+""", html);
        Assert.Contains("The .NET 10 way", html);
    }

    [Fact]
    public async Task Every_dashboard_feature_is_outlined_on_the_page()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("data-xray=\"cacheview\"", html);
        Assert.Contains("data-xray=\"virtualize-anchor-mode\"", html);
        Assert.Contains("data-xray=\"renderfragment-across-render-modes\"", html);
    }

    static string Stamp(string html, string attribute)
    {
        var match = Regex.Match(html, $"""<time datetime="([^"]+)" {attribute}[ >]""");
        Assert.True(match.Success, $"No {attribute} stamp on the page.");
        return match.Groups[1].Value;
    }

    static int OrderId(OrderEvent e) => int.Parse(e.Subject.TrimStart('O', 'r', 'd', 'e', ' ', '#'));

    [GeneratedRegex("""<!--Blazor:\{"type":"server""")]
    private static partial Regex ServerMarker();

    [GeneratedRegex(@"^(\d+) units left")]
    private static partial Regex StockLeft();
}
