using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The env-vars demo: .NET 10's recorded read of the file beside the live page passing the variable.</summary>
public partial class WasmEnvVarsConfigDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Recording_shows_the_variable_arriving_but_configuration_reading_the_file()
    {
        var html = WebUtility.HtmlDecode(await app.CreateClient().GetStringAsync("/demo/env-config/orders/net10"));

        Assert.Contains("data-recording", html);
        var apiUrlRow = ApiUrlRow().Match(html).Value;
        // File, variable, then what Config returned: the file's staging URL.
        Assert.Matches("staging-api.northwind.test/orders</td>\\s*<td[^>]*>demo/env-config/api/orders</td>", apiUrlRow);
        Assert.Matches("class=\"miss\"[^>]*><b[^>]*>https://staging-api.northwind.test/orders</b>", apiUrlRow);
        Assert.Contains("Couldn't reach https://staging-api.northwind.test/orders", html);
    }

    [Fact]
    public async Task Live_page_passes_the_variable_only_until_it_is_switched_off()
    {
        var client = app.CreateClient();

        Assert.Contains("\"Orders__ApiUrl\":\"demo/env-config/api/orders\"", await OptionsOf(client, "/demo/env-config/orders"));
        Assert.DoesNotContain("Orders__ApiUrl", await OptionsOf(client, "/demo/env-config/orders?variable=off"));

        var api = await client.GetStringAsync("/demo/env-config/api/orders?take=5");
        Assert.Equal(5, Regex.Matches(api, "\"id\":").Count);
    }

    static async Task<string> OptionsOf(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var base64 = BrowserConfiguration().Match(html).Groups[1].Value;
        return base64.Length == 0 ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    [GeneratedRegex("<tr[^>]*><th[^>]*>Orders:ApiUrl</th>.*?</tr>", RegexOptions.Singleline)]
    private static partial Regex ApiUrlRow();

    [GeneratedRegex("<!--Blazor-Configuration:([A-Za-z0-9+/=]+)-->")]
    private static partial Regex BrowserConfiguration();
}
