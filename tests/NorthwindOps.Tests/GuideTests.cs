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
    public void Entry_has_every_step(string id)
    {
        var entry = guide.Find(id)!;

        Assert.True(features.Find(id) is not null, $"{id} needs a full write-up in Showcase/Features/{id}.md.");
        Assert.NotEmpty(entry.Problem.Question);
        Assert.NotEmpty(entry.Problem.Options);
        Assert.All(entry.Problem.Options, o => Assert.Contains("class=\"bad\"", o.Html));
        Assert.NotEmpty(entry.Try.Question);
        Assert.InRange(entry.Try.WatchHtml.Count, 2, 4);
        Assert.NotEmpty(entry.Code.Question);
        Assert.NotEmpty(entry.Code.Cards);
        Assert.InRange(entry.GoodToKnow.Count, 1, 4);
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Code_cards_show_demo_source_that_marks_the_feature(string id)
    {
        foreach (var card in guide.Find(id)!.Code.Cards)
        {
            var view = sources.View(card.Path, id);
            Assert.True(view is not null, $"{id} shows {card.Path}, which isn't embedded source.");
            Assert.True(view.HighlightedLines.Length > 0, $"{card.Path} has no xray:{id} markers to highlight.");
        }
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public async Task Every_step_and_the_demo_render(string id)
    {
        var client = app.CreateClient();
        var entry = guide.Find(id)!;

        foreach (var url in new[] { $"/guide/{id}", $"/guide/{id}/try", $"/guide/{id}/code", "/" + entry.DemoUrl })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} returned {(int)response.StatusCode}.");
        }
    }

    [Fact]
    public async Task Index_links_every_entry()
    {
        var html = await app.CreateClient().GetStringAsync("/guide");

        Assert.All(guide.All, e => Assert.Contains($"href=\"guide/{e.Id}\"", html));
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
            intro: An intro.
            mode: Static SSR
            demo: demo/x
            ---

            ## The problem

            Why is this *hard*?

            The lead.

            ### Workaround one

            ```csharp
            // ### not a heading inside a fence
            var x = 1;
            ```

            It hurts. {.bad}

            ## See it work

            Try this.

            A lead.

            1. Do **this**.
            2. Then that.

            ## How it's built

            Two parts.

            ### The first part
            code: Components/Guide/Demos/TempData/EditOrder.razor

            A note.

            ## Good to know

            - **First thing.** Its detail with `code`.
            """);

        Assert.Equal("Why is this <em>hard</em>?", entry.Problem.Question);
        var option = Assert.Single(entry.Problem.Options);
        Assert.Equal("Workaround one", option.Title);
        Assert.Contains("### not a heading", option.Html);
        Assert.Matches("<p class=\"bad\">It hurts.\\s*</p>", option.Html);
        Assert.Equal(["Do <strong>this</strong>.", "Then that."], entry.Try.WatchHtml);
        Assert.Contains("A lead.", entry.Try.LeadHtml);
        var card = Assert.Single(entry.Code.Cards);
        Assert.Equal(("The first part", "Components/Guide/Demos/TempData/EditOrder.razor"), (card.Caption, card.Path));
        Assert.Contains("A note.", card.NoteHtml);
        var note = Assert.Single(entry.GoodToKnow);
        Assert.Equal("First thing", note.Title);
        Assert.Equal("Its detail with <code>code</code>.", note.Html);
    }

    [GeneratedRegex("""href="demo/tempdata/orders/(\d+)""")]
    private static partial Regex OrderLink();

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();
}
