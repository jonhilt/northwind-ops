using System.Text.RegularExpressions;
using NorthwindOps.Showcase;

namespace NorthwindOps.Tests;

/// <summary>The anchor-mode entry runs a plain .NET 10 Virtualize beside the AnchorMode one.</summary>
public partial class VirtualizeAnchorModeDemoTests(AppFactory app) : IClassFixture<AppFactory>
{
    [GeneratedRegex("AnchorMode=\"VirtualizeAnchorMode\\.End\"")]
    private static partial Regex AnchorEnd();

    [Fact]
    public void Entry_shows_the_dotnet10_list_beside_the_anchored_one()
    {
        var entry = new GuideRegistry().Find("virtualize-anchor-mode")!;

        Assert.Equal("demo/anchor-mode/inbox/net10", entry.Before?.Url.TrimStart('/'));
        Assert.Equal(3, entry.Code.Cards.Count);
    }

    [Fact]
    public void Only_the_dotnet11_feed_sets_AnchorMode()
    {
        var sources = new SourceCatalog();
        var after = sources.Read("Components/Guide/Demos/AnchorMode/MessageFeed.razor")!;
        var before = sources.Read("Components/Guide/Demos/AnchorMode/MessageFeedNet10.razor")!;

        Assert.Matches(AnchorEnd(), after);
        Assert.DoesNotContain("AnchorMode=", before);
        Assert.Contains("<Virtualize Items=\"messages\"", before);
    }

    [Theory]
    [InlineData("/demo/anchor-mode/inbox")]
    [InlineData("/demo/anchor-mode/inbox/net10")]
    public async Task Both_inboxes_start_from_the_same_300_messages_with_the_readout(string url)
    {
        var html = await app.CreateClient().GetStringAsync(url);

        Assert.Contains("300 messages", html);
        Assert.Contains("data-newest=\"299\"", html);
        Assert.Contains("data-feed-readout", html);
        Assert.Contains("FeedReadout", html);
    }
}
