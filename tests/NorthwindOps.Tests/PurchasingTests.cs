using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using NorthwindOps.Data;
using NorthwindOps.Features.Purchasing;

namespace NorthwindOps.Tests;

public partial class PurchasingTests(AppFactory app) : IClassFixture<AppFactory>
{
    // Pavlova, Ltd.: five products, and Outback Lager (70) is below its reorder level.
    const int Pavlova = 7;
    const int OutbackLager = 70;
    const int Vegiespread = 63;

    // Each test gets its own cookie jar, so its own session. Redirects are checked, not followed.
    readonly HttpClient browser = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task List_shows_seeded_purchase_orders_on_a_static_page()
    {
        var html = await browser.GetStringAsync("/purchasing");

        Assert.Contains("PO-2401", html);
        Assert.Contains("""<span class="pill received" """, html);
        Assert.Matches("""href="purchasing/new" class="btn primary"[^>]*>New purchase order""", html);
        Assert.Matches(@"\d+ purchase orders, \d+ awaiting delivery", html);
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [Fact]
    public async Task New_purchase_order_starts_at_the_supplier_step()
    {
        var response = await browser.GetAsync("/purchasing/new");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/purchasing/new/supplier", response.Headers.Location!.AbsolutePath);
    }

    [Theory]
    [InlineData("/purchasing/new/lines")]
    [InlineData("/purchasing/new/review")]
    public async Task Later_steps_need_a_draft(string url)
    {
        var response = await browser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/purchasing/new/supplier", response.Headers.Location!.AbsolutePath);
    }

    [Theory]
    [InlineData("/purchasing/new/payment")]
    [InlineData("/purchasing/new/2")]
    public async Task Unknown_steps_are_not_found(string url)
    {
        var response = await browser.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_wizard_is_static_SSR_and_starts_a_session()
    {
        var response = await browser.GetAsync("/purchasing/new/supplier");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(".AspNetCore.Session=", StringComparison.Ordinal));
        Assert.Contains("""name="SupplierChoice" value="7" """, html);
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [Fact]
    public async Task Choosing_a_supplier_stores_it_in_session_and_redirects_to_the_sibling_step()
    {
        var response = await ChooseSupplier(Pavlova);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/purchasing/new/lines", response.Headers.Location!.AbsolutePath);

        var html = await browser.GetStringAsync("/purchasing/new/lines");
        Assert.Contains("What do we need from Pavlova, Ltd.?", html);
        Assert.Contains($"""name="Quantities[{OutbackLager}]" """, html);
        Assert.Contains($"{DraftKeys.Supplier} = {Pavlova}", html);
    }

    [Fact]
    public async Task Step_links_are_relative_navlinks_resolved_against_the_wizard_folder()
    {
        await ChooseSupplier(Pavlova);

        var html = await browser.GetStringAsync("/purchasing/new/lines");

        Assert.Contains("""<a href="http://localhost/purchasing/new/supplier" class="step">""", html);
        Assert.Contains("""<a href="http://localhost/purchasing/new/lines" class="step active" aria-current="page">""", html);
        Assert.Contains("""<span class="step locked" """, html); // review needs at least one line first
        Assert.Matches("resolve against <b[^>]*>/purchasing/new/</b>", html);
    }

    [Fact]
    public async Task Draft_survives_refresh_and_leaving_the_wizard()
    {
        await ChooseSupplier(Pavlova);
        await SaveLines("next", (OutbackLager, 45), (Vegiespread, 10));

        // Refresh: the quantities come back from session, not from the form.
        var lines = await browser.GetStringAsync("/purchasing/new/lines");
        Assert.Contains($"""name="Quantities[{OutbackLager}]" value="45" """, lines);
        Assert.Contains($"""name="Quantities[{Vegiespread}]" value="10" """, lines);

        // Leave and come back: the list knows about the draft, and /purchasing/new resumes it.
        var list = await browser.GetStringAsync("/purchasing");
        Assert.Contains("Draft in progress", list);
        Assert.Matches("<b[^>]*>Pavlova, Ltd.</b>", list);
        Assert.Contains("2 products, 55 packs", list);

        var resume = await browser.GetAsync("/purchasing/new");
        Assert.Equal("/purchasing/new/lines", resume.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Sessions_are_per_browser()
    {
        await ChooseSupplier(Pavlova);

        var otherBrowser = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await otherBrowser.GetStringAsync("/purchasing");

        Assert.DoesNotContain("Draft in progress", html);
    }

    [Fact]
    public async Task Continuing_with_no_quantities_shows_an_error_and_stays_on_the_step()
    {
        await ChooseSupplier(Pavlova);

        var response = await SaveLines("next");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Add a quantity for at least one product.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Fill_in_suggested_quantities_tops_up_low_stock_lines()
    {
        await ChooseSupplier(Pavlova);

        var response = await SaveLines("suggest", (Vegiespread, 5));
        Assert.Equal("/purchasing/new/lines", response.Headers.Location!.AbsolutePath);

        var html = await browser.GetStringAsync("/purchasing/new/lines");
        Assert.Contains($"""name="Quantities[{OutbackLager}]" value="45" """, html);
        Assert.Contains($"""name="Quantities[{Vegiespread}]" value="5" """, html);
    }

    [Fact]
    public async Task Back_saves_the_lines_and_changing_supplier_clears_them()
    {
        await ChooseSupplier(Pavlova);

        var back = await SaveLines("back", (OutbackLager, 20));
        Assert.Equal("/purchasing/new/supplier", back.Headers.Location!.AbsolutePath);
        Assert.Contains($"{DraftKeys.Lines} = {{ &quot;{OutbackLager}&quot;: 20 }}", await browser.GetStringAsync("/purchasing/new/supplier"));

        await ChooseSupplier(1);
        Assert.Contains($"{DraftKeys.Lines} = null", await browser.GetStringAsync("/purchasing/new/lines"));
    }

    [Fact]
    public async Task Sending_creates_the_purchase_order_clears_the_draft_and_flashes_once()
    {
        await ChooseSupplier(Pavlova);
        await SaveLines("next", (OutbackLager, 45), (Vegiespread, 10));

        var review = await browser.GetStringAsync("/purchasing/new/review");
        Assert.Contains("$1,114.00", review); // 45 × $15.00 + 10 × $43.90

        var deliverBy = DateTime.Today.AddDays(10);
        var sent = await Post("/purchasing/new/review", "po-send", ("DeliverBy", deliverBy.ToString("yyyy-MM-dd")));
        Assert.Equal(HttpStatusCode.Redirect, sent.StatusCode);
        Assert.Equal("/purchasing", sent.Headers.Location!.AbsolutePath);
        // A plain redirect, not a streamed response: TempData's cookie can only be written before the response starts.
        Assert.Contains(sent.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(".AspNetCore.Components.TempData=", StringComparison.Ordinal));
        Assert.DoesNotContain("<blazor-ssr>", await sent.Content.ReadAsStringAsync());

        var list = await browser.GetStringAsync("/purchasing");
        var number = Flash().Match(list).Groups["number"].Value;
        Assert.Matches(@"PO-\d+", number);
        Assert.Contains("sent to Pavlova, Ltd.", list);
        Assert.Contains($"$1,114.00 · due {deliverBy:d MMM}", list);
        Assert.DoesNotContain("Draft in progress", list);
        Assert.Matches($"""<tr class="just-sent"[^>]*><td[^>]*><b[^>]*>{number}<""", list);

        // TempData is read once: a refresh shows the order in the list but no message.
        var refreshed = await browser.GetStringAsync("/purchasing");
        Assert.DoesNotMatch(Flash(), refreshed);
        Assert.Contains(number, refreshed);

        var restart = await browser.GetAsync("/purchasing/new");
        Assert.Equal("/purchasing/new/supplier", restart.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Delivery_date_must_be_after_today()
    {
        await ChooseSupplier(Pavlova);
        await SaveLines("next", (OutbackLager, 45));

        var response = await Post("/purchasing/new/review", "po-send", ("DeliverBy", DateTime.Today.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Choose a delivery date after today.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Discarding_the_draft_from_the_list_clears_the_session()
    {
        await ChooseSupplier(Pavlova);

        var response = await Post("/purchasing", "po-discard-from-list");
        Assert.Equal("/purchasing", response.Headers.Location!.AbsolutePath);

        Assert.DoesNotContain("Draft in progress", await browser.GetStringAsync("/purchasing"));
    }

    [Theory]
    [InlineData(17, 25, 35)]  // 2 × 25 − 17 = 33, rounded up to a whole five
    [InlineData(0, 20, 40)]
    [InlineData(25, 25, 0)]   // at the reorder level: nothing to order
    [InlineData(3, 0, 0)]     // no reorder level set
    public void Suggested_quantity_restocks_to_twice_the_reorder_level_in_fives(int inStock, int reorderLevel, int expected)
    {
        var product = new Product { Name = "Test", UnitsInStock = inStock, ReorderLevel = reorderLevel };

        Assert.Equal(expected, PurchaseOrders.SuggestedQuantity(product));
    }

    [Fact]
    public void Lines_keep_only_the_suppliers_current_products_with_a_quantity()
    {
        Product[] products =
        [
            new() { Id = 1, Name = "Chai", UnitPrice = 18m },
            new() { Id = 2, Name = "Chang", UnitPrice = 19m },
            new() { Id = 3, Name = "Old stock", UnitPrice = 5m, Discontinued = true },
        ];
        var draft = new Dictionary<string, int> { ["1"] = 10, ["2"] = 0, ["3"] = 5, ["99"] = 7 };

        var lines = PurchaseOrders.LinesFor(draft, products);

        var line = Assert.Single(lines);
        Assert.Equal((1, 10, 18m), (line.ProductId, line.Quantity, line.UnitPrice));
        Assert.Equal(180m, PurchaseOrders.TotalOf(lines));
    }

    Task<HttpResponseMessage> ChooseSupplier(int supplierId)
        => Post("/purchasing/new/supplier", "po-supplier", ("SupplierChoice", supplierId.ToString()));

    Task<HttpResponseMessage> SaveLines(string go, params (int ProductId, int Quantity)[] quantities)
        => Post("/purchasing/new/lines", "po-lines",
            [("go", go), .. quantities.Select(q => ($"Quantities[{q.ProductId}]", q.Quantity.ToString()))]);

    /// <summary>Posts a Blazor SSR form the way a browser would: from the page itself, with its antiforgery token if it has one.</summary>
    async Task<HttpResponseMessage> Post(string url, string formName, params (string Name, string Value)[] fields)
    {
        var page = await browser.GetStringAsync(url);
        var values = new List<KeyValuePair<string, string>> { new("_handler", formName) };
        if (AntiforgeryToken().Match(page) is { Success: true } token)
        {
            values.Add(new("__RequestVerificationToken", WebUtility.HtmlDecode(token.Groups[1].Value)));
        }
        values.AddRange(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(values) };
        request.Headers.Add("Origin", "http://localhost");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        return await browser.SendAsync(request);
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();

    [GeneratedRegex("""<div class="flash"[^>]*>\s*<b[^>]*>(?<number>PO-\d+) sent to""")]
    private static partial Regex Flash();
}
