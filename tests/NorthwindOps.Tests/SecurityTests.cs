using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthwindOps.Data;
using NorthwindOps.Features.Suppliers;

namespace NorthwindOps.Tests;

/// <summary>
/// Anonymous visitors write to one shared database, and SQLite ignores column lengths. So every write path has to cap
/// what it stores on the server, or one visitor could fill memory for everyone who loads a list.
/// </summary>
public partial class SecurityTests(AppFactory app) : IClassFixture<AppFactory>
{
    readonly HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task A_cross_site_form_post_is_refused()
    {
        var order = await FirstOrderAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/orders/{order.Id}")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["_handler"] = "edit-order", ["Input.Status"] = "Pending", ["Input.ShippedOn"] = "", ["Input.Notes"] = "From elsewhere",
            }),
        };
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEqual("From elsewhere", (await LoadOrderAsync(order.Id)).Notes);
    }

    [Fact]
    public async Task No_posted_field_can_be_larger_than_16_KB()
    {
        var order = await FirstOrderAsync();

        var response = await client.PostAsync($"/demo/tempdata/orders/{order.Id}", Form(new()
        {
            ["_handler"] = "edit-order", ["Input.Notes"] = new string('x', 20_000),
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEqual(20_000, (await LoadOrderAsync(order.Id)).Notes?.Length ?? 0);
    }

    [Fact]
    public async Task TempData_demo_does_not_save_a_note_over_500_characters()
    {
        var order = await FirstOrderAsync();

        var response = await client.PostAsync($"/demo/tempdata/orders/{order.Id}", Form(new()
        {
            ["_handler"] = "edit-order", ["Input.Notes"] = new string('y', 501),
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // the form again, with its message, rather than a redirect
        Assert.Contains("validation-message", await response.Content.ReadAsStringAsync());
        Assert.NotEqual(501, (await LoadOrderAsync(order.Id)).Notes?.Length ?? 0);
    }

    [Fact]
    public async Task An_order_status_that_does_not_exist_is_not_saved()
    {
        var order = await FirstOrderAsync();

        var response = await client.PostAsync($"/orders/{order.Id}", Form(new()
        {
            ["_handler"] = "edit-order", ["Input.Status"] = "42", ["Input.ShippedOn"] = "", ["Input.Notes"] = "",
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(Enum.IsDefined((await LoadOrderAsync(order.Id)).Status));
    }

    [Fact]
    public async Task A_supplier_draft_still_has_to_fit_its_fields()
    {
        var response = await PostSupplierAsync(new()
        {
            ["Intent"] = "draft",
            ["Supplier.Company"] = "Long Draft Ltd",
            ["Supplier.Address.Country"] = new string('z', 5_000),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("can't be longer than 40 characters", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.False(await SupplierExistsAsync("Long Draft Ltd"));
    }

    [Fact]
    public async Task A_supplier_draft_can_have_at_most_ten_other_contacts()
    {
        var fields = new Dictionary<string, string> { ["Intent"] = "draft", ["Supplier.Company"] = "Many Contacts Ltd" };
        for (var i = 0; i <= SupplierForm.MaxContacts; i++)
        {
            fields[$"Supplier.Contacts[{i}].Name"] = $"Person {i}";
        }

        var response = await PostSupplierAsync(fields);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("at most 10 other contacts", await response.Content.ReadAsStringAsync());
        Assert.False(await SupplierExistsAsync("Many Contacts Ltd"));
    }

    [Fact]
    public async Task A_supplier_draft_within_its_limits_is_still_saved()
    {
        var response = await PostSupplierAsync(new() { ["Intent"] = "draft", ["Supplier.Company"] = "Tidy Draft Ltd", ["Supplier.Address.Country"] = "UK" });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(await SupplierExistsAsync("Tidy Draft Ltd"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/orders")]
    [InlineData("/suppliers/new")]
    [InlineData("/guide")]
    public async Task Every_page_sends_the_security_headers(string url)
    {
        var response = await client.GetAsync(url);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Matches("script-src 'self' 'wasm-unsafe-eval' 'sha256-[^']+'", policy);
        Assert.Contains("object-src 'none'", policy);
        Assert.Contains("frame-ancestors 'self'", policy);
    }

    [Fact]
    public async Task Support_keeps_its_stricter_policy()
    {
        var response = await client.GetAsync("/support");

        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("style-src 'self';", policy);
        Assert.DoesNotContain("unsafe-inline", policy);
    }

    [Fact]
    public async Task One_visitor_posting_too_often_is_told_to_slow_down_and_others_are_not()
    {
        using var limited = new AppFactory { FormPostsPerMinute = 2 };
        var visitor = limited.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpRequestMessage Post(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/demo/tempdata/orders/1") { Content = Form(new() { ["_handler"] = "edit-order" }) };
            request.Headers.Add("CF-Connecting-IP", ip);
            return request;
        }

        await visitor.SendAsync(Post("203.0.113.7"));
        await visitor.SendAsync(Post("203.0.113.7"));
        var third = await visitor.SendAsync(Post("203.0.113.7"));
        var someoneElse = await visitor.SendAsync(Post("198.51.100.20"));

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, someoneElse.StatusCode);
    }

    [Fact]
    public async Task A_post_that_is_not_a_form_is_a_bad_request()
    {
        var response = await client.PostAsync("/orders", new StringContent("hello"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_post_to_a_page_that_does_not_exist_counts_once()
    {
        using var limited = new AppFactory { FormPostsPerMinute = 2 };
        var visitor = limited.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Each is re-run to render the not-found page, which mustn't use up another of the visitor's posts.
        var first = await visitor.PostAsync("/no-such-page", Form(new() { ["x"] = "1" }));
        var second = await visitor.PostAsync("/no-such-page", Form(new() { ["x"] = "1" }));
        var third = await visitor.PostAsync("/no-such-page", Form(new() { ["x"] = "1" }));

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    // ── Helpers ──

    static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);

    async Task<HttpResponseMessage> PostSupplierAsync(Dictionary<string, string> fields)
    {
        var page = await client.GetStringAsync("/suppliers/new");
        fields["__RequestVerificationToken"] = AntiforgeryToken().Match(page).Groups[1].Value;
        fields["_handler"] = "new-supplier";
        return await client.PostAsync("/suppliers/new", Form(fields));
    }

    async Task<NorthwindDb> DbAsync() => await app.Services.GetRequiredService<IDbContextFactory<NorthwindDb>>().CreateDbContextAsync();

    async Task<Order> FirstOrderAsync()
    {
        await client.GetAsync("/healthz"); // starts the app, which seeds its database
        await using var db = await DbAsync();
        return await db.Orders.AsNoTracking().OrderByDescending(o => o.Id).FirstAsync();
    }

    async Task<Order> LoadOrderAsync(int id)
    {
        await using var db = await DbAsync();
        return await db.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
    }

    async Task<bool> SupplierExistsAsync(string company)
    {
        await using var db = await DbAsync();
        return await db.Set<Supplier>().AnyAsync(s => s.Company == company);
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();
}
