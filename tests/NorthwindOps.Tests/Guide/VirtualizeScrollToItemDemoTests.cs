using System.Text.RegularExpressions;

namespace NorthwindOps.Tests;

/// <summary>Checks the claims "Open a list at any item" makes about its two demos.</summary>
public partial class VirtualizeScrollToItemDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task Dotnet10_side_prerenders_no_messages_and_scrolls_by_offset()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/scroll-to-item/incident/net10?message=300");

        // No InitialItemIndex: the first HTML holds none of the thread, so the list can only jump once the circuit runs.
        Assert.Empty(MessageNumber().Matches(html));
        Assert.Contains("data-asked=\"300\"", html);
        Assert.Contains("IncidentNet10.razor", html); // the scrollToOffset script
        Assert.Contains("data-thread-readout", html);
    }

    [Fact]
    public async Task Dotnet11_side_prerenders_the_messages_around_the_link()
    {
        var html = await app.CreateClient().GetStringAsync("/demo/scroll-to-item/incident?message=300");

        var numbers = MessageNumber().Matches(html).Select(m => int.Parse(m.Groups[1].Value)).ToList();
        Assert.Contains(300, numbers);
        Assert.DoesNotContain(1, numbers);
        Assert.InRange(numbers.Count, 1, 60); // still virtualized: a window, not the whole thread
        Assert.Contains("data-thread-readout", html);
    }

    [Fact]
    public async Task Both_sides_open_the_same_420_message_thread()
    {
        var client = app.CreateClient();

        foreach (var path in new[] { "/demo/scroll-to-item/incident/net10", "/demo/scroll-to-item/incident" })
        {
            var html = await client.GetStringAsync(path + "?message=300");
            Assert.Contains("data-count=\"420\"", html);
        }
    }

    [GeneratedRegex("""#(\d+)</header>""")]
    private static partial Regex MessageNumber();
}
