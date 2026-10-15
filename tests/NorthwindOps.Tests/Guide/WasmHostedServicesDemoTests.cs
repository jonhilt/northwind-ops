using System.Net;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The hosted-services entry: a live .NET 10 polling bell, with the .NET 10 repro recorded, beside AddHostedService.</summary>
public class WasmHostedServicesDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Both_sides_put_a_webassembly_bell_on_orders_and_none_on_customers()
    {
        var client = app.CreateClient();
        var before = await client.GetStringAsync("/demo/hosted-services/orders/net10");
        var after = await client.GetStringAsync("/demo/hosted-services/orders");

        // Each Orders page hosts its bell as a WebAssembly root component (no prerender).
        Assert.Contains("PollingBell", before);
        Assert.Contains("OrderBell", after);
        Assert.Contains("href=\"demo/hosted-services/customers/net10\"", before);

        foreach (var url in new[] { "/demo/hosted-services/customers/net10", "/demo/hosted-services/customers" })
        {
            var customers = WebUtility.HtmlDecode(await client.GetStringAsync(url));
            Assert.DoesNotContain("Bell", customers);
            Assert.Contains("doesn't show the bell", customers);
        }
    }

    [Fact]
    public async Task The_before_side_records_what_AddHostedService_did_on_dotnet10()
    {
        var response = await app.CreateClient().GetAsync("/demo/hosted-services/orders/net10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains("data-recording", html);
        Assert.Contains("repros/wasm-hosted-services", html);
        // Each row names the panel or workaround it stands for.
        Assert.Matches(Row("Timer in the page", "this bell", "5", "gone", "1"), html);
        Assert.Matches(Row("AddHostedService", "the .NET 11 line", "0", "0", "0"), html);
        Assert.Matches(Row("Started by hand", "workaround 2", "5", "8", "10"), html);
    }

    static Regex Row(string name, string role, params string[] cells) =>
        new($@"<th[^>]*>{name}\s*<i[^>]*>{role}</i></th>"
            + string.Concat(cells.Select(c => $@"\s*<td[^>]*>{c}</td>")));
}
