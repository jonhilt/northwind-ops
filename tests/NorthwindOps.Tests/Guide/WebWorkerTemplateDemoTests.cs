using System.Text.RegularExpressions;
using NorthwindOps.Worker;

namespace NorthwindOps.Tests;

/// <summary>The Web Worker entry: the .NET 10 report totals on the UI thread, the .NET 11 one in a worker.</summary>
public partial class WebWorkerTemplateDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [GeneratedRegex(@"<button disabled>([^<]+)</button>")]
    private static partial Regex Button();

    static string[] Buttons(string html) => [.. Button().Matches(html).Select(m => m.Groups[1].Value)];

    [Fact]
    public async Task Only_the_dotnet11_side_totals_in_a_worker()
    {
        var client = app.CreateClient();

        var before = await client.GetStringAsync("/demo/web-worker/order-lines/net10");
        var after = await client.GetStringAsync("/demo/web-worker/order-lines");

        Assert.Equal(["Total on the UI thread", "Total, pausing every 1,000 lines"], Buttons(before));
        Assert.Equal(["Total in a Web Worker", "Call a misspelled method"], Buttons(after));
        // Both sides show the clock that stops while the UI thread is busy.
        Assert.Contains("Clock on the UI thread", before);
        Assert.Contains("Clock on the UI thread", after);
    }

    [Fact]
    public void Pausing_totals_the_same_as_the_worker_method()
    {
        var csv = "OrderId,OrderDate,CustomerId,Product,UnitPrice,Quantity,Discount\n"
            + "1,2025-01-01,ALFKI,Chai,18.00,10,0.10\n"
            + "1,2025-01-01,ALFKI,Tofu,23.25,2,0.00\n"
            + "2,2025-01-02,BONAP,Chai,18.00,5,0.00\n";
        var tally = new OrderLineTally();
        foreach (var line in csv.Split('\n').Skip(1).Where(l => l.Length > 0))
            tally.Add(line);

#pragma warning disable CA1416 // [SupportedOSPlatform("browser")], but plain C# that runs anywhere
        var fromWorkerMethod = WorkerMethods.TotalOrderLines(System.Text.Encoding.UTF8.GetBytes(csv));
#pragma warning restore CA1416

        Assert.Equal(fromWorkerMethod, tally.ToJson());
        Assert.Contains("\"Product\":\"Chai\",\"Units\":15,\"Revenue\":252", fromWorkerMethod);
    }
}
