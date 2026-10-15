using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>
/// The auto-pause entry: the .NET 10 side pauses with its own script and has no AutoPause, the
/// .NET 11 side turns AutoPause on, and both run the same count sheet.
/// </summary>
public partial class CircuitAutoPauseDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task The_net10_side_pauses_with_its_own_script_and_the_net11_side_with_AutoPause()
    {
        var client = app.CreateClient();
        var before = await client.GetStringAsync("/demo/auto-pause/stock-take/net10");
        var after = await client.GetStringAsync("/demo/auto-pause/stock-take");

        Assert.Contains("src=\"js/auto-pause-net10.js\"", before);
        Assert.DoesNotContain("autoPause", ServerOptions(before));
        Assert.DoesNotContain("src=\"js/auto-pause-net10.js\"", after);
        Assert.Contains("\"autoPauseEnabled\":true", ServerOptions(after));

        var script = await client.GetStringAsync("/js/auto-pause-net10.js");
        Assert.Contains("Blazor.pauseCircuit()", script);
        Assert.Contains("10_000", script); // the same 10 seconds as the .NET 11 side
    }

    [Fact]
    public async Task Both_sides_run_the_same_sheet_on_a_circuit()
    {
        var client = app.CreateClient();
        foreach (var url in new[] { "/demo/auto-pause/stock-take/net10", "/demo/auto-pause/stock-take" })
        {
            // prerender: false, so the page holds only the marker the circuit starts the sheet from.
            var html = await client.GetStringAsync(url);
            Assert.Contains("<!--Blazor:{\"type\":\"server\"", html);
            Assert.Contains("<h1>Stock take: Beverages</h1>", html);
        }
    }

    static string ServerOptions(string html)
    {
        var base64 = BrowserConfiguration().Match(html).Groups[1].Value;
        if (string.IsNullOrEmpty(base64)) return "";
        var options = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64))).RootElement;
        return options.TryGetProperty("server", out var server) ? server.GetRawText() : "";
    }

    [GeneratedRegex("<!--Blazor-Configuration:([A-Za-z0-9+/=]+)-->")]
    private static partial Regex BrowserConfiguration();
}
