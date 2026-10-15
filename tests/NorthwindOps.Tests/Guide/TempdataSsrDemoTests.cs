using System.Net;
using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>The TempData entry's two sides: the .NET 10 flag in the URL, and TempData.</summary>
public partial class TempdataSsrDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task The_net10_side_redirects_with_a_flag_that_shows_saved_on_every_load()
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        var newest = Net10OrderLink().Match(await client.GetStringAsync("/demo/tempdata/orders/net10")).Groups[1].Value;
        var save = await SaveAsync(client, $"/demo/tempdata/orders/net10/{newest}", "From the .NET 10 side");

        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        Assert.EndsWith($"/demo/tempdata/orders/net10?saved={newest}", save.Headers.Location!.ToString());
        Assert.DoesNotContain(save.Headers.TryGetValues("Set-Cookie", out var set) ? set : [],
            c => c.StartsWith(".AspNetCore.Components.TempData=", StringComparison.Ordinal));

        // A refresh, or anyone the link is shared with, sees it again.
        for (var i = 0; i < 2; i++)
        {
            var list = await client.GetStringAsync($"/demo/tempdata/orders/net10?saved={newest}");
            Assert.Contains($"Order {newest} saved", list);
        }

        // And the flag says whatever the link says: the list offers one to try.
        Assert.Contains("href=\"demo/tempdata/orders/net10?saved=99999\"", await client.GetStringAsync("/demo/tempdata/orders/net10"));
        Assert.Contains("Order 99999 saved", await client.GetStringAsync("/demo/tempdata/orders/net10?saved=99999"));
    }

    [Fact]
    public async Task The_net11_side_shows_the_message_once_from_an_encrypted_cookie()
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        var newest = OrderLink().Match(await client.GetStringAsync("/demo/tempdata/orders")).Groups[1].Value;
        var save = await SaveAsync(client, $"/demo/tempdata/orders/{newest}", "From the .NET 11 side");
        Assert.EndsWith("/demo/tempdata/orders", save.Headers.Location!.ToString());
        Assert.Contains(save.Headers.GetValues("Set-Cookie"), // encrypted, not "Order … saved"
            c => c.StartsWith(".AspNetCore.Components.TempData=CfDJ8", StringComparison.Ordinal));

        var list = await client.GetStringAsync("/demo/tempdata/orders");
        Assert.Contains($"Order {newest} saved", list);

        var refreshed = await client.GetStringAsync("/demo/tempdata/orders");
        Assert.DoesNotContain($"Order {newest} saved", refreshed);
    }

    static async Task<HttpResponseMessage> SaveAsync(HttpClient client, string url, string note)
    {
        var form = await client.GetStringAsync(url);
        var token = WebUtility.HtmlDecode(AntiforgeryToken().Match(form).Groups[1].Value);
        return await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "edit-order",
            ["Input.Notes"] = note,
            ["__RequestVerificationToken"] = token,
        }));
    }

    [GeneratedRegex("""href="demo/tempdata/orders/(\d+)""")]
    private static partial Regex OrderLink();

    [GeneratedRegex("""href="demo/tempdata/orders/net10/(\d+)""")]
    private static partial Regex Net10OrderLink();

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();
}
