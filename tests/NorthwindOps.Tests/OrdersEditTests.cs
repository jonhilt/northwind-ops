using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthwindOps.Data;
using NorthwindOps.Features.OrdersEdit;

namespace NorthwindOps.Tests;

public class OrdersEditTests(AppFactory app) : IClassFixture<AppFactory>
{
    const string TempDataCookie = ".AspNetCore.Components.TempData";
    const string ListView = "orders?sort=Total&direction=desc&page=2";
    static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    // Follows nothing, so each test sees the redirect itself; keeps cookies like a browser.
    readonly HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Orders_grid_links_each_order_to_its_page_carrying_the_list_view()
    {
        var html = await client.GetStringAsync("/" + ListView);

        Assert.Matches("""<a href="orders/\d+\?returnUrl=orders%3Fsort%3DTotal%26direction%3Ddesc%26page%3D2">\d+</a>""", html);
        // Sorting by the Order column still works now it's a link.
        Assert.Contains("sort=Order", html);
    }

    [Fact]
    public async Task Order_page_shows_customer_lines_and_totals_on_a_static_page()
    {
        var order = await FindOrderAsync(q => q.Where(o => o.Lines.Count >= 3 && o.Lines.Any(l => l.Discount > 0)));

        var html = await client.GetStringAsync($"/orders/{order.Id}");

        Assert.Contains($"Order {order.Id}", html);
        Assert.Contains(Encode(order.Customer.Company), html);
        foreach (var line in order.Lines)
        {
            Assert.Contains(Encode(line.Product.Name), html);
            Assert.Contains(Encode((line.UnitPrice * line.Quantity * (1 - line.Discount)).ToString("C", Us)), html);
        }
        Assert.Contains(Encode(order.Total.ToString("C", Us)), html);
        Assert.Contains("Discounts", html);

        Assert.Contains("""<input type="hidden" name="_handler" value="edit-order" />""", html);
        Assert.Contains("data-enhance", html);
        Assert.Contains("Static SSR", html);
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [Fact]
    public async Task Unknown_order_is_not_found()
    {
        var response = await client.GetAsync("/orders/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Saving_redirects_to_the_list_view_with_a_flash_that_shows_once()
    {
        var order = await FindOrderAsync(q => q.Where(o => o.Status == OrderStatus.Packing));
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        var response = await PostAsync(order.Id, ListView, OrderStatus.Shipped, today, "Sent with the afternoon van.");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/" + ListView, response.Headers.Location!.PathAndQuery);

        var saved = await LoadAsync(order.Id);
        Assert.Equal(OrderStatus.Shipped, saved.Status);
        Assert.Equal(DateTime.Today, saved.ShippedOn);
        Assert.Equal("Sent with the afternoon van.", saved.Notes);

        var list = await client.GetStringAsync(response.Headers.Location);
        Assert.Contains($"Order {order.Id} saved", list);
        Assert.Contains("class=\"flash\" role=\"status\"", list);

        var refreshed = await client.GetStringAsync(response.Headers.Location);
        Assert.DoesNotContain($"Order {order.Id} saved", refreshed);
        Assert.DoesNotContain("class=\"flash\"", refreshed);
    }

    [Fact]
    public async Task Flash_travels_in_an_encrypted_cookie_that_the_list_deletes_once_read()
    {
        // No cookie handling here, so we can see the Set-Cookie headers and pass the cookie on by hand.
        var raw = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var order = await FindOrderAsync(q => q.Where(o => o.Status == OrderStatus.Delivered).OrderByDescending(o => o.Id));

        var save = await raw.PostAsync($"/orders/{order.Id}", Form(OrderStatus.Delivered, order.ShippedOn!.Value.ToString("yyyy-MM-dd"), "Signed for by J. Smith"));

        var cookie = Assert.Single(save.Headers.GetValues("Set-Cookie"), c => c.StartsWith(TempDataCookie + "=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=lax", cookie);
        var value = cookie[(TempDataCookie.Length + 1)..cookie.IndexOf(';')];
        Assert.DoesNotContain("saved", value); // Data Protection, not plain text
        Assert.StartsWith("CfDJ8", value);

        using var read = new HttpRequestMessage(HttpMethod.Get, "/orders");
        read.Headers.Add("Cookie", $"{TempDataCookie}={value}");
        var list = await raw.SendAsync(read);

        Assert.Contains($"Order {order.Id} saved", await list.Content.ReadAsStringAsync());
        Assert.Contains(list.Headers.GetValues("Set-Cookie"), c => c.StartsWith(TempDataCookie + "=;", StringComparison.Ordinal) && c.Contains("expires=Thu, 01 Jan 1970"));
    }

    [Fact]
    public async Task Invalid_change_shows_the_rule_and_saves_nothing()
    {
        var order = await FindOrderAsync(q => q.Where(o => o.Status == OrderStatus.Pending));

        var response = await PostAsync(order.Id, ListView, OrderStatus.Pending, DateTime.Today.ToString("yyyy-MM-dd"), "Should not be saved");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Only shipped or delivered orders have a shipped date.", html);
        // The form keeps what was typed, and the header still shows what's saved.
        Assert.Contains("Should not be saved</textarea>", html);
        Assert.Contains("""<span class="pill pending""", html);

        var unchanged = await LoadAsync(order.Id);
        Assert.Null(unchanged.ShippedOn);
        Assert.Equal(order.Notes, unchanged.Notes);

        var list = await client.GetStringAsync("/" + ListView);
        Assert.DoesNotContain("""class="flash""", list);
    }

    [Fact]
    public async Task Delivered_order_needs_a_shipped_date()
    {
        var order = await FindOrderAsync(q => q.Where(o => o.Status == OrderStatus.Delivered));

        var response = await PostAsync(order.Id, null, OrderStatus.Delivered, shippedOn: "", notes: "");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A delivered order needs a shipped date.", await response.Content.ReadAsStringAsync());
        Assert.NotNull((await LoadAsync(order.Id)).ShippedOn);
    }

    [Theory]
    [InlineData("https://evil.example/orders")]
    [InlineData("//evil.example")]
    [InlineData("suppliers")]
    public async Task Return_url_only_ever_leads_back_to_the_orders_list(string returnUrl)
    {
        var order = await FindOrderAsync(q => q.Where(o => o.Status == OrderStatus.Cancelled));

        var response = await PostAsync(order.Id, returnUrl, OrderStatus.Cancelled, shippedOn: "", notes: "");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("localhost", response.Headers.Location!.Host);
        Assert.Equal("/orders", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task Cross_site_form_post_is_rejected()
    {
        var order = await FindOrderAsync(q => q.Where(o => o.Status == OrderStatus.Packing).OrderByDescending(o => o.Id));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/orders/{order.Id}") { Content = Form(OrderStatus.Cancelled, "", "") };
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(OrderStatus.Packing, (await LoadAsync(order.Id)).Status);
    }

    [Fact]
    public async Task Some_recent_orders_are_seeded_with_notes()
    {
        await using var db = await CreateDbAsync();

        Assert.True(await db.Orders.CountAsync(o => o.Notes != null) >= 3);
    }

    // ---- Rules, without HTTP ----

    static readonly DateTime PlacedOn = new(2026, 10, 1, 9, 30, 0);
    static readonly DateOnly Today = new(2026, 10, 13);

    [Theory]
    [InlineData(OrderStatus.Shipped, "2026-10-02", null)]
    [InlineData(OrderStatus.Delivered, "2026-10-01", null)]
    [InlineData(OrderStatus.Pending, null, null)]
    [InlineData(OrderStatus.Cancelled, null, null)]
    [InlineData(OrderStatus.Shipped, null, "A shipped order needs a shipped date.")]
    [InlineData(OrderStatus.Packing, "2026-10-02", "Only shipped or delivered orders have a shipped date. Clear the date or change the status.")]
    [InlineData(OrderStatus.Shipped, "2026-09-30", "An order can't ship before it was placed (1 Oct 2026).")]
    [InlineData(OrderStatus.Delivered, "2026-10-14", "The shipped date can't be in the future.")]
    public void Shipped_date_follows_the_status(OrderStatus status, string? shippedOn, string? error)
    {
        var form = new OrderEditForm { Status = status, ShippedOn = shippedOn is null ? null : DateOnly.Parse(shippedOn, CultureInfo.InvariantCulture) };

        var results = form.Check(PlacedOn, Today).ToList();

        if (error is null)
        {
            Assert.Empty(results);
        }
        else
        {
            var result = Assert.Single(results);
            Assert.Equal(error, result.ErrorMessage);
            Assert.Equal([nameof(OrderEditForm.ShippedOn)], result.MemberNames);
        }
    }

    [Fact]
    public void Note_is_limited_to_500_characters()
    {
        var form = new OrderEditForm { Notes = new string('x', 501) };

        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), null, validateAllProperties: true));
    }

    [Fact]
    public void Unchanged_shipped_date_keeps_its_time_and_blank_notes_are_cleared()
    {
        var order = new Order { CustomerId = "ALFKI", PlacedOn = PlacedOn, ShippedOn = new DateTime(2026, 10, 2, 14, 5, 0), Notes = "old" };

        var form = OrderEditForm.From(order);
        form.Notes = "   ";
        form.ApplyTo(order);
        Assert.Equal(new DateTime(2026, 10, 2, 14, 5, 0), order.ShippedOn);
        Assert.Null(order.Notes);

        form.ShippedOn = new DateOnly(2026, 10, 3);
        form.ApplyTo(order);
        Assert.Equal(new DateTime(2026, 10, 3), order.ShippedOn);
    }

    [Theory]
    [InlineData(null, "orders")]
    [InlineData("", "orders")]
    [InlineData("orders", "orders")]
    [InlineData("orders?sort=Total&page=3", "orders?sort=Total&page=3")]
    [InlineData("https://evil.example", "orders")]
    [InlineData("//evil.example/orders", "orders")]
    [InlineData("/\\evil.example", "orders")]
    [InlineData("orders-archive", "orders")]
    [InlineData("support", "orders")]
    public void Return_url_is_limited_to_the_orders_list(string? returnUrl, string expected)
        => Assert.Equal(expected, OrderLinks.SafeReturnUrl(returnUrl));

    [Fact]
    public void Detail_link_only_carries_a_return_url_when_there_is_a_view_to_return_to()
    {
        Assert.Equal("orders/11078", OrderLinks.Detail(11078, "orders"));
        Assert.Equal("orders/11078?returnUrl=orders%3Fstatus%3DShipped%26page%3D2", OrderLinks.Detail(11078, "orders?status=Shipped&page=2"));
    }

    // ---- Helpers ----

    Task<HttpResponseMessage> PostAsync(int orderId, string? returnUrl, OrderStatus status, string shippedOn, string notes)
    {
        var url = returnUrl is null ? $"/orders/{orderId}" : $"/orders/{orderId}?returnUrl={Uri.EscapeDataString(returnUrl)}";
        return client.PostAsync(url, Form(status, shippedOn, notes));
    }

    static FormUrlEncodedContent Form(OrderStatus status, string shippedOn, string notes) => new(new Dictionary<string, string>
    {
        ["_handler"] = "edit-order",
        ["Input.Status"] = status.ToString(),
        ["Input.ShippedOn"] = shippedOn,
        ["Input.Notes"] = notes,
    });

    // The client is created first, so the app has started and seeded its database by now.
    Task<NorthwindDb> CreateDbAsync() => app.Services.GetRequiredService<IDbContextFactory<NorthwindDb>>().CreateDbContextAsync();

    async Task<Order> FindOrderAsync(Func<IQueryable<Order>, IQueryable<Order>> filter)
    {
        await using var db = await CreateDbAsync();
        var orders = db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Lines).ThenInclude(l => l.Product).OrderBy(o => o.Id);
        return await filter(orders).FirstAsync();
    }

    async Task<Order> LoadAsync(int id)
    {
        await using var db = await CreateDbAsync();
        return await db.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
    }

    static string Encode(string text) => HtmlEncoder.Default.Encode(text);
}
