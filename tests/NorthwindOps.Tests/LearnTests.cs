using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using NorthwindOps.Components.Learn;

namespace NorthwindOps.Tests;

/// <summary>Checks every walkthrough (/learn/{id}) renders whole: its code, and its live demos or animated figure.</summary>
public partial class LearnTests(AppFactory app) : IClassFixture<AppFactory>
{
    /// <summary>Every page routed at /learn/{id}.</summary>
    public static readonly string[] Urls = [.. typeof(LearnLayout).Assembly.GetTypes()
        .SelectMany(t => t.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>())
        .Select(r => r.Template)
        .Where(t => t.StartsWith("/learn/"))];

    public static TheoryData<string> Walkthroughs => [.. Urls];

    [Fact]
    public void There_are_walkthroughs()
        => Assert.NotEmpty(Walkthroughs);

    [Theory]
    [MemberData(nameof(Walkthroughs))]
    public async Task Walkthrough_renders_its_code_and_demos(string url)
    {
        var client = app.CreateClient();
        var html = await client.GetStringAsync(url);

        // Its id is a Field Guide feature, so the full write-up it links to exists.
        Assert.NotNull(new Showcase.FeatureRegistry().Find(url["/learn/".Length..]));

        // Code figures show real source with the feature's lines highlighted.
        Assert.All(Regex.Matches(html, "<pre class=\"line-numbers\" data-line=\"([^\"]*)\""),
            m => Assert.NotEmpty(m.Groups[1].Value));

        // Live demos come in pairs: the .NET 10 way and the .NET 11 way. Where the difference happens out of
        // sight (on the server, say), an animated figure shows it instead.
        var demos = DemoFrame().Matches(html).Select(m => (Side: m.Groups["side"].Value, Url: m.Groups["url"].Value)).ToList();
        if (demos.Count == 0)
        {
            Assert.Contains("data-anim", html);
        }
        else
        {
            Assert.Contains(demos, d => d.Side == "before");
            Assert.Contains(demos, d => d.Side == "after");
        }
        foreach (var (side, demo) in demos)
        {
            // Only a .NET 10 demo may fail on purpose: a 500 that names the exception in the frame (DemoCrash).
            var response = await client.GetAsync("/" + demo);
            var crashed = side == "before" && response.StatusCode == HttpStatusCode.InternalServerError
                && (await response.Content.ReadAsStringAsync()).Contains("data-crash");
            Assert.True(response.StatusCode == HttpStatusCode.OK || crashed, $"The {side} demo {demo} returned {(int)response.StatusCode}.");
        }
    }

    [GeneratedRegex("data-side=\"(?<side>[a-z]+)\".*?<iframe src=\"(?<url>[^\"]+)\" data-demo-frame", RegexOptions.Singleline)]
    private static partial Regex DemoFrame();
}
