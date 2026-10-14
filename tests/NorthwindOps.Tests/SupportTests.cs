using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthwindOps.Data;
using NorthwindOps.Features.Support;

namespace NorthwindOps.Tests;

public partial class SupportTests(AppFactory app) : IClassFixture<AppFactory>
{
    readonly HttpClient client = app.CreateClient();

    SupportDesk Desk => app.Services.GetRequiredService<SupportDesk>();

    async Task<TicketRow> LongestThreadAsync() => (await Desk.ListAsync()).MaxBy(t => t.MessageCount)!;

    // ───────────── Ticket list: QuickGrid OnRowClick ─────────────

    [Fact]
    public async Task Ticket_rows_are_clickable_and_every_subject_is_also_a_link()
    {
        var tickets = await Desk.ListAsync();
        var html = await client.GetStringAsync("/support");

        // OnRowClick gives every row QuickGrid's row-clickable class...
        Assert.Equal(tickets.Count, RowClickable().Matches(html).Count);
        // ...and each subject is a real link, for keyboards, new tabs and before the circuit starts.
        foreach (var ticket in tickets)
        {
            Assert.Contains($"href=\"support/{ticket.Id}\"", html);
        }
    }

    [Fact]
    public async Task Only_the_ticket_desk_is_interactive()
    {
        var html = await client.GetStringAsync("/support");

        Assert.Contains("""<span class="mode-chip server">""", html);
        Assert.Contains("<!--Blazor:{\"type\":\"server\"", html);
    }

    [Fact]
    public async Task Selected_ticket_row_is_highlighted()
    {
        var ticket = (await Desk.ListAsync())[3];
        var html = await client.GetStringAsync($"/support/{ticket.Id}");

        var selected = Assert.Single(SelectedRow().Matches(html));
        Assert.Contains($"href=\"support/{ticket.Id}\"", selected.Value);
    }

    [Fact]
    public async Task Unknown_ticket_is_not_found()
    {
        var response = await client.GetAsync("/support/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ───────────── Thread: Virtualize InitialItemIndex ─────────────

    [Fact]
    public async Task Long_thread_opens_at_the_first_unread_message()
    {
        var ticket = await LongestThreadAsync();
        var html = await client.GetStringAsync($"/support/{ticket.Id}");

        // Prerendering already renders the window around InitialItemIndex, not the start of the thread.
        var numbers = MessageNumbers(html);
        Assert.Contains(ticket.ReadUpTo + 1, numbers);
        Assert.DoesNotContain(1, numbers);
        Assert.Contains("New since you last read", html);
        Assert.Contains($"First unread (#{ticket.ReadUpTo + 1})", html);
    }

    [Fact]
    public async Task Message_link_opens_the_thread_at_that_message()
    {
        var ticket = await LongestThreadAsync();
        var html = await client.GetStringAsync($"/support/{ticket.Id}?message=40");

        var numbers = MessageNumbers(html);
        Assert.Contains(40, numbers);
        Assert.DoesNotContain(ticket.ReadUpTo + 1, numbers);
    }

    [Theory]
    [InlineData(10, 4, null, 4)]   // first unread
    [InlineData(10, 10, null, 9)]  // all read: the latest
    [InlineData(10, 0, null, 0)]   // nothing read: the start
    [InlineData(10, 4, 7, 6)]      // a link to message #7
    [InlineData(10, 4, 0, 4)]      // out-of-range links are ignored
    [InlineData(10, 4, 11, 4)]
    [InlineData(0, 0, null, 0)]
    public void Opening_index_prefers_the_linked_message_then_the_first_unread_then_the_latest(int count, int readUpTo, int? requested, int expected)
        => Assert.Equal(expected, SupportDesk.OpeningIndex(count, readUpTo, requested));

    // ───────────── Variable heights ─────────────

    [Fact]
    public async Task Threads_mix_one_liners_with_long_emails_and_hundreds_of_messages()
    {
        await using var db = await app.Services.GetRequiredService<IDbContextFactory<NorthwindDb>>().CreateDbContextAsync();
        var tickets = await db.Set<Ticket>().Include(t => t.Messages).ToListAsync();

        Assert.InRange(tickets.Count, 35, 45);
        Assert.Contains(tickets, t => t.Messages.Count >= 300);
        Assert.Contains(tickets, t => t.Messages.Count <= 4);
        var lengths = tickets.SelectMany(t => t.Messages).Select(m => m.Body.Length).ToList();
        Assert.True(lengths.Min() < 40, "Some messages should be one-liners.");
        Assert.True(lengths.Max() > 700, "Some messages should run to several paragraphs.");
        foreach (var ticket in tickets)
        {
            Assert.Equal(Enumerable.Range(1, ticket.Messages.Count), ticket.Messages.Select(m => m.Number).Order());
            Assert.InRange(ticket.ReadUpTo, 0, ticket.Messages.Count);
            Assert.Equal(ticket.Messages.Max(m => m.SentOn), ticket.LastActivityOn);
        }
    }

    [Fact]
    public async Task Long_messages_start_clamped_with_a_show_more_button()
    {
        var ticket = await LongestThreadAsync();
        var html = await client.GetStringAsync($"/support/{ticket.Id}");

        Assert.Contains("class=\"body clamped\"", html);
        Assert.Contains(">Show more</button>", html);
    }

    // ───────────── Replies ─────────────

    [Fact]
    public async Task Reply_is_added_to_the_end_of_the_thread_and_marks_it_read()
    {
        var ticket = (await Desk.ListAsync()).First(t => t.Status == TicketStatus.Open && t.MessageCount < 20);

        var reply = await Desk.ReplyAsync(ticket.Id, "  We've sent a replacement today.  ");
        var after = (await Desk.GetAsync(ticket.Id))!;

        Assert.Equal(ticket.MessageCount + 1, reply.Number);
        Assert.Equal("We've sent a replacement today.", after.Messages[^1].Body);
        Assert.Equal(MessageAuthor.Agent, after.Messages[^1].AuthorKind);
        Assert.Equal(reply.Number, after.ReadUpTo);
        Assert.Equal(TicketStatus.Waiting, after.Status);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Empty_or_overlong_replies_are_rejected(string? body)
    {
        var ticket = (await Desk.ListAsync())[0];
        body ??= new string('x', SupportDesk.MaxReplyLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() => Desk.ReplyAsync(ticket.Id, body));
    }

    [Fact]
    public async Task Mark_as_read_reads_every_message()
    {
        var ticket = (await Desk.ListAsync()).First(t => t.Unread > 0 && t.MessageCount < 100);

        var readUpTo = await Desk.MarkReadAsync(ticket.Id);

        Assert.Equal(ticket.MessageCount, readUpTo);
        Assert.Equal(0, (await Desk.ListAsync()).Single(t => t.Id == ticket.Id).Unread);
    }

    // ───────────── Content-Security-Policy ─────────────

    [Theory]
    [InlineData("/support")]
    [InlineData("/support/2401")]
    public async Task Support_pages_send_a_strict_policy(string url)
    {
        var response = await client.GetAsync(url);
        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));

        Assert.Contains("style-src 'self';", policy);
        Assert.Contains("script-src 'self' 'sha256-", policy);
        Assert.DoesNotContain("unsafe-inline", policy);
        Assert.DoesNotContain("unsafe-eval", policy);
    }

    [Fact]
    public async Task Script_policy_allows_exactly_the_import_map()
    {
        var response = await client.GetAsync("/support");
        var html = await response.Content.ReadAsStringAsync();
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();

        // The only inline script on the page is <ImportMap />, and the policy carries its hash.
        var inlineScripts = InlineScript().Matches(html);
        var importMap = Assert.Single(inlineScripts);
        Assert.Contains("type=\"importmap\"", importMap.Value);
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(importMap.Groups["body"].Value)));
        Assert.Contains($"'sha256-{hash}'", policy);
    }

    [Fact]
    public async Task Virtualized_thread_renders_without_inline_styles()
    {
        var ticket = await LongestThreadAsync();
        var html = await client.GetStringAsync($"/support/{ticket.Id}");

        Assert.DoesNotMatch(@"\sstyle=""", html);
        Assert.DoesNotContain("<style", html);
        // The spacers carry their heights as data for Virtualize's script to apply.
        Assert.Equal(2, ReservedHeight().Matches(html).Count);
    }

    [Fact]
    public async Task Other_screens_keep_their_own_policy()
    {
        var response = await client.GetAsync("/orders");

        // They get the site-wide policy, which allows the style="..." attributes Support refuses.
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("style-src 'self' 'unsafe-inline'", policy);
    }

    [Fact]
    public async Task Policy_also_comes_as_a_meta_tag_for_enhanced_navigation()
    {
        // Enhanced navigation into /support keeps the current document, so the header alone wouldn't apply.
        var request = new HttpRequestMessage(HttpMethod.Get, "/support/2401");
        request.Headers.TryAddWithoutValidation("Accept", "text/html; blazor-enhanced-nav=on");

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var meta = MetaPolicy().Match(html);
        Assert.True(meta.Success, "No Content-Security-Policy <meta> tag in <head>.");
        var header = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.StartsWith(WebUtility.HtmlDecode(meta.Groups["policy"].Value), header);
        // frame-ancestors is ignored (with a console error) in a <meta> tag, so it's only in the header.
        Assert.DoesNotContain("frame-ancestors", meta.Groups["policy"].Value);
        Assert.Contains("frame-ancestors 'self'", header);
    }

    // ───────────── X-ray ─────────────

    [Theory]
    [InlineData("quickgrid-onrowclick")]
    [InlineData("virtualize-variable-height")]
    [InlineData("virtualize-scroll-to-item")]
    [InlineData("virtualize-csp")]
    public async Task Every_support_feature_has_an_xray_panel_with_highlighted_code(string id)
    {
        var html = await client.GetStringAsync($"/xray/{id}/panel");

        Assert.Matches("""data-line="\d+""", html);
    }

    static List<int> MessageNumbers(string html)
        => MessageNumber().Matches(html).Select(m => int.Parse(m.Groups[1].Value)).ToList();

    [GeneratedRegex("""<tr[^>]*class="row-clickable[^"]*""")]
    private static partial Regex RowClickable();

    [GeneratedRegex("""<head>.*<meta http-equiv="Content-Security-Policy" content="(?<policy>[^"]+)".*</head>""", RegexOptions.Singleline)]
    private static partial Regex MetaPolicy();

    [GeneratedRegex("""<tr[^>]*class="row-clickable selected"[^>]*>.*?</tr>""", RegexOptions.Singleline)]
    private static partial Regex SelectedRow();

    [GeneratedRegex("""<span class="num"[^>]*>#(\d+)</span>""")]
    private static partial Regex MessageNumber();

    [GeneratedRegex("""<script(?![^>]*\ssrc=)[^>]*>(?<body>.*?)</script>""", RegexOptions.Singleline)]
    private static partial Regex InlineScript();

    [GeneratedRegex(@"data-blazor-virtualize-reserved-height=""\d+(\.\d+)?""")]
    private static partial Regex ReservedHeight();
}
