using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>
/// Runs on its own, after the other tests: the demo's query tallies are static, so another test class
/// rendering the same pages in parallel would add to them.
/// </summary>
[CollectionDefinition(nameof(CacheviewDemoTests), DisableParallelization = true)]
public class CacheviewDemoCollection;

/// <summary>The cacheview entry: five visitors who miss at once run the query five times the .NET 10 way, once with CacheView.</summary>
[Collection(nameof(CacheviewDemoTests))]
public partial class CacheviewDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Theory]
    [InlineData("/demo/cacheview/stock/net10", 5)]
    [InlineData("/demo/cacheview/stock", 1)]
    public async Task Five_visitors_who_miss_at_once_run_the_query(string url, int expectedQueries)
    {
        var client = app.CreateClient();

        var start = Queries(await client.GetStringAsync(url));
        await Task.Delay(TimeSpan.FromSeconds(10.5)); // both sides cache for 10 seconds
        var pages = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.GetStringAsync(url)));
        var after = Queries(await client.GetStringAsync(url)); // a hit, so it runs nothing

        Assert.Equal(expectedQueries, after - start);
        Assert.All(pages, p => Assert.Matches(FiguresStamp(), p));
    }

    [Fact]
    public async Task Refreshing_within_ten_seconds_serves_the_cached_figures_on_both_sides()
    {
        var client = app.CreateClient();
        foreach (var url in new[] { "/demo/cacheview/stock/net10", "/demo/cacheview/stock" })
        {
            var first = await client.GetStringAsync(url);
            var second = await client.GetStringAsync(url);
            Assert.Equal(FiguresStamp().Match(first).Value, FiguresStamp().Match(second).Value);
            Assert.Equal(Queries(first), Queries(second));
        }
    }

    static int Queries(string html) => int.Parse(QueriesRun().Match(html).Groups[1].Value);

    [GeneratedRegex("data-queries[^>]*>(\\d+)</span>")]
    private static partial Regex QueriesRun();

    [GeneratedRegex("Figures calculated at <b[^>]*>[0-9:]+</b>")]
    private static partial Regex FiguresStamp();
}
