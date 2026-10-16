using System.Net;
using System.Text.RegularExpressions;
using NorthwindOps.Showcase;

namespace NorthwindOps.Tests;

/// <summary>Keeps every Field Guide entry complete, and checks its pages and demo work.</summary>
public partial class GuideTests(AppFactory app) : IClassFixture<AppFactory>
{
    readonly GuideRegistry guide = new();
    readonly FeatureRegistry features = new();
    readonly SourceCatalog sources = new();

    public static TheoryData<string> Entries => [.. new GuideRegistry().All.Select(e => e.Id)];

    [Fact]
    public void There_are_entries()
        => Assert.NotEmpty(guide.All);

    [Theory]
    [MemberData(nameof(Entries))]
    public void Entry_has_a_write_up_and_a_walkthrough(string id)
    {
        Assert.True(features.Find(id) is not null, $"{id} needs a full write-up in Showcase/Features/{id}.md.");
        Assert.Contains($"/learn/{id}", LearnTests.Urls);
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public async Task Old_guide_steps_redirect_to_the_walkthrough(string id)
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false });

        foreach (var url in new[] { $"/guide/{id}", $"/guide/{id}/try", $"/guide/{id}/code" })
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal($"/learn/{id}", response.Headers.Location!.OriginalString);
        }
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public async Task Both_demos_run(string id)
    {
        var client = app.CreateClient();
        var entry = guide.Find(id)!;

        var response = await client.GetAsync("/" + entry.DemoUrl);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"/{entry.DemoUrl} returned {(int)response.StatusCode}.");

        // The old way may fail outright, as long as it says why in its frame.
        if (entry.Before is { } before)
        {
            response = await client.GetAsync("/" + before.Url);
            var failed = response.StatusCode == HttpStatusCode.InternalServerError
                && (await response.Content.ReadAsStringAsync()).Contains("data-crash");
            Assert.True(response.StatusCode == HttpStatusCode.OK || failed, $"/{before.Url} returned {(int)response.StatusCode}.");
        }
    }

    public static TheoryData<string> EntriesWithBefore => [.. new GuideRegistry().All.Where(e => e.Before is not null).Select(e => e.Id)];

    [Fact]
    public async Task Nested_validation_demo_shows_the_address_and_a_list_of_contacts()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/nested-validation/with");

        Assert.Matches("<legend[^>]*>Address</legend>", html);
        Assert.Matches("<legend[^>]*>Contacts</legend>", html);
        Assert.True(ContactRow().Matches(html).Count >= 2, "The demo should start with at least two contacts, so the list is visible.");
        Assert.Contains("name=\"Input.Contacts[1].Email\"", html);
        Assert.Contains("value=\"add\"", html);
        Assert.Contains("value=\"remove:1\"", html);
    }

    [Theory]
    [InlineData("without", true)]
    [InlineData("with", false)]
    public async Task Nested_validation_demo_saves_a_bad_contact_only_without_the_attribute(string side, bool saves)
    {
        var html = await PostNewSupplier(side, new()
        {
            ["Input.Company"] = "Acme",
            ["Input.Address.City"] = "",
            ["Input.Contacts[0].Name"] = "Ann",
            ["Input.Contacts[0].Email"] = "sales@",
            ["Input.Contacts[1].Name"] = "Bo",
            ["Input.Contacts[1].Email"] = "",
        });

        Assert.Equal(saves, html.Contains("data-saved"));
        Assert.Equal(!saves, html.Contains(">The City field is required.<"));
        Assert.Equal(!saves, html.Contains(">The Email field is not a valid e-mail address.<"));
    }

    [Fact]
    public async Task Nested_validation_demo_adds_a_contact_without_validating()
    {
        var html = await PostNewSupplier("with", new()
        {
            ["Input.Company"] = "",
            ["Input.Contacts[0].Name"] = "Ann",
            ["Input.Contacts[1].Name"] = "Bo",
            ["Intent"] = "add",
        });

        Assert.Equal(3, ContactRow().Matches(html).Count);
        Assert.DoesNotContain("field is required", html);
    }

    [Fact]
    public async Task Environment_view_demo_fails_the_dotnet10_way_and_works_the_dotnet11_way()
    {
        var client = app.CreateClient();

        var before = await client.GetAsync("/demo/environment-view/orders/net10");
        var crash = WebUtility.HtmlDecode(await before.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.InternalServerError, before.StatusCode);
        Assert.Contains("There is no registered service of type 'Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment'", crash);

        var after = await client.GetStringAsync("/demo/environment-view/orders");
        Assert.Contains("test data, not real customers", after);
        Assert.Contains("class=\"debug\"", after);
        Assert.DoesNotContain("These are real orders", after);
    }

    [Fact]
    public void Server_pause_board_shows_a_request_that_never_got_an_answer()
    {
        var board = new NorthwindOps.Components.Guide.Demos.CircuitPause.CircuitBoard(TimeProvider.System);

        board.Asked("js-interop");    // .NET 10: the awaited JS call never returns, so nothing answers
        board.Asked("server-api");
        board.Answered("server-api", "true");

        Assert.Null(board.Request("js-interop")!.Answer);
        Assert.Equal("true", board.Request("server-api")!.Answer);
        Assert.NotNull(board.Request("server-api")!.After);
        Assert.Null(board.Request("never-asked"));
    }

        [Theory]
    [InlineData("/net10", "The E-Mail field is not a valid e-mail address.")]
    [InlineData("", "E-Mail ist keine gültige E-Mail-Adresse.")]
    public async Task Validation_localization_demo_misses_a_message_the_dotnet10_way(string side, string emailMessage)
    {
        var client = app.CreateClient(new() { HandleCookies = true });
        var url = $"/demo/validation-localization/suppliers/new{side}?culture=de-DE";
        var form = await client.GetStringAsync(url);
        var response = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "new-supplier",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(AntiforgeryToken().Match(form).Groups[1].Value),
            ["Input.Company"] = "",
            ["Input.Email"] = "sales@",
        }));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains(">Firma ist ein Pflichtfeld.<", html);
        Assert.Contains($">{emailMessage}<", html);
    }

    [Theory]
    [MemberData(nameof(EntriesWithBefore))]
    public async Task A_recorded_before_demo_names_a_repro_that_exists(string id)
    {
        var before = guide.Find(id)!.Before!;
        var html = await (await app.CreateClient().GetAsync("/" + before.Url)).Content.ReadAsStringAsync();
        if (!html.Contains("data-recording"))
        {
            return; // runs live, so there's nothing to trace
        }

        var repro = Regex.Match(html, @"repros/([a-z0-9-]+)").Groups[1].Value;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "NorthwindOps.slnx")))
        {
            root = root.Parent;
        }
        Assert.True(Directory.Exists(Path.Combine(root!.FullName, "docs/research/field-guide-audit/repros", repro)),
            $"/{before.Url} is recorded from repros/{repro}, which isn't in docs/research/field-guide-audit.");
    }

    [Fact]
    public async Task Static_content_demo_recorded_the_dotnet10_failure_and_works_live()
    {
        var client = app.CreateClient();

        var before = WebUtility.HtmlDecode(await client.GetStringAsync("/demo/static-content/low-stock/net10"));
        Assert.Contains("Cannot pass the parameter 'ChildContent' to component 'Collapsible'", before);

        var after = await client.GetStringAsync("/demo/static-content/low-stock");
        Assert.Contains("Queried at", after);
        Assert.Contains("<table", after);
    }

    [Fact]
    public async Task Grid_url_demo_replays_dead_dotnet10_buttons_beside_dotnet11_links()
    {
        var client = app.CreateClient();

        var before = await client.GetStringAsync("/demo/grid-url/products/net10");
        Assert.Matches("<button class=\"col-title\" type=\"button\"[^>]*>", before);
        Assert.DoesNotContain("<a class=\"col-title\"", before);
        Assert.Matches("Page <strong[^>]*>1</strong>", before); // the URL asked for page 2

        var after = WebUtility.HtmlDecode(await client.GetStringAsync("/demo/grid-url/products?sort=Price&direction=desc&page=2"));
        Assert.Matches("<a class=\"col-title\" href=\"[^\"]*sort=Price&direction=asc&page=2\"", after);
        Assert.Matches("Page <strong[^>]*>2", after);
        Assert.Contains("aria-sort=\"descending\"", after);
    }

    [Fact]
    public async Task Virtualize_csp_demo_replays_dotnet10_inline_spacers_under_the_same_policy()
    {
        var client = app.CreateClient();

        var before = await client.GetAsync("/demo/virtualize-csp/orders/net10");
        var after = await client.GetAsync("/demo/virtualize-csp/orders");
        var replay = await before.Content.ReadAsStringAsync();

        // Same policy on both, so the browser refuses the replayed spacers' inline heights.
        Assert.Equal("style-src 'self'; frame-ancestors 'self'", before.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal(before.Headers.GetValues("Content-Security-Policy").Single(), after.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("style=\"height: 39240px; flex-shrink: 0;\"", replay);
        Assert.Equal(19, Regex.Matches(replay, "class=\"row\"").Count);
    }

    [Fact]
    public async Task Variable_height_demo_charts_the_dotnet10_heights_beside_a_live_readout()
    {
        var client = app.CreateClient();

        // The .NET 10 side charts the measured heights: one point a scroll step, swinging 12,550px.
        var before = await client.GetStringAsync("/demo/variable-height/incident/net10");
        var points = Regex.Match(before, "<polyline points=\"([^\"]+)\"").Groups[1].Value.Split(' ');
        Assert.Equal(41, points.Length);
        Assert.Contains("33,057px", before);
        Assert.Contains("45,607px", before);

        var after = await client.GetStringAsync("/demo/variable-height/incident");
        Assert.Contains("data-scroll-readout", after);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/js/scroll-readout.js")).StatusCode);
    }

    [Fact]
    public async Task Wasm_culture_demo_records_the_dotnet10_flip_and_keeps_what_the_server_rendered()
    {
        var client = app.CreateClient();

        var before = await client.GetStringAsync("/demo/wasm-culture/invoice/net10");
        Assert.Contains("1.234,56", before);
        Assert.Contains("1,234.56", before);

        // The live invoice prerenders in the request's culture, for WebAssembly to carry on in.
        var after = WebUtility.HtmlDecode(await client.GetStringAsync("/demo/wasm-culture/invoice?culture=de-DE"));
        Assert.Matches("Formatting in <b[^>]*>de-DE</b>", after);
    }

    [Fact]
    public async Task Row_click_demo_puts_a_button_in_every_row_only_the_dotnet10_way()
    {
        var client = app.CreateClient();

        var before = await client.GetStringAsync("/demo/row-click/tickets/net10");
        var after = await client.GetStringAsync("/demo/row-click/tickets");

        Assert.Equal(8, Regex.Matches(before, "<button type=\"button\" class=\"open\"").Count);
        Assert.DoesNotContain("class=\"open\"", after);
    }

    [Fact]
    public async Task Async_validation_demo_checks_the_code_in_a_handler_only_the_dotnet10_way()
    {
        var client = app.CreateClient();

        var before = await client.GetStringAsync("/demo/async-validation/suppliers/new/net10");
        var after = await client.GetStringAsync("/demo/async-validation/suppliers/new");

        // Both are the same New supplier form; only the .NET 11 one can say "Checking…" on its Save button.
        Assert.Contains("Supplier code", before);
        Assert.Contains("Supplier code", after);
        Assert.Contains(">Save supplier</button>", before);
        Assert.Matches(@"<button type=""submit""[^>]*>\s*Save supplier", after);
    }

    async Task<string> PostNewSupplier(string side, Dictionary<string, string> fields)
    {
        var client = app.CreateClient(new() { HandleCookies = true });
        var url = $"/demo/nested-validation/{side}";
        var form = await client.GetStringAsync(url);
        fields["_handler"] = "new-supplier";
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(AntiforgeryToken().Match(form).Groups[1].Value);
        var response = await client.PostAsync(url, new FormUrlEncodedContent(fields));
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task The_guide_is_the_home_page_and_links_to_the_app()
    {
        var html = await app.CreateClient().GetStringAsync("/");

        Assert.Contains("<h1 class=\"l-title\">Field Guide</h1>", html);
        Assert.Contains("href=\"app\"", html);
    }

    [Fact]
    public async Task Index_links_every_entry()
    {
        var html = await app.CreateClient().GetStringAsync("/guide");

        Assert.All(guide.All, e => Assert.Contains($"href=\"learn/{e.Id}\"", html));
    }

    [Theory]
    [InlineData("/guide/no-such-feature")]
    [InlineData("/guide/tempdata-ssr/no-such-step")]
    public async Task Unknown_entries_and_steps_are_not_found(string url)
    {
        var response = await app.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TempData_demo_shows_the_message_once()
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        var newest = OrderLink().Match(await client.GetStringAsync("/demo/tempdata/orders")).Groups[1].Value;
        var form = await client.GetStringAsync($"/demo/tempdata/orders/{newest}");
        var token = WebUtility.HtmlDecode(AntiforgeryToken().Match(form).Groups[1].Value);
        var save = await client.PostAsync($"/demo/tempdata/orders/{newest}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "edit-order",
            ["Input.Notes"] = "Leave with reception",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        Assert.EndsWith("/demo/tempdata/orders", save.Headers.Location!.ToString());

        var list = await client.GetStringAsync("/demo/tempdata/orders");
        Assert.Contains($"Order {newest} saved", list);
        Assert.Contains("Leave with reception", list);

        var refreshed = await client.GetStringAsync("/demo/tempdata/orders");
        Assert.DoesNotContain($"Order {newest} saved", refreshed);
    }

    [Fact]
    public void Parses_the_guide_format()
    {
        var entry = GuideRegistry.Parse("test.md", """
            ---
            id: demo-feature
            title: Demo
            group: Forms
            order: 5
            hook: A hook.
            mode: Static SSR
            demo: demo/x
            before: demo/x/old
            labels: The old way | The new way
            ---
            """);

        Assert.Equal(new GuideEntry("demo-feature", "Demo", "Forms", 5, "A hook.", "Static SSR", "demo/x",
            new GuideBefore("demo/x/old", "The old way", "The new way")), entry);
    }

    [GeneratedRegex(@"data-contact[\s>]")]
    private static partial Regex ContactRow();

    [GeneratedRegex("""href="demo/tempdata/orders/(\d+)""")]
    private static partial Regex OrderLink();

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();
}
