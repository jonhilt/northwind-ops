using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using NorthwindOps.Components.Learn;

namespace NorthwindOps.Tests;

/// <summary>Checks every walkthrough (/learn/{id}) renders whole: its code and its live demos.</summary>
public class LearnTests(AppFactory app) : IClassFixture<AppFactory>
{
    public static TheoryData<string> Walkthroughs => [.. typeof(LearnLayout).Assembly.GetTypes()
        .SelectMany(t => t.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>())
        .Select(r => r.Template)
        .Where(t => t.StartsWith("/learn/"))];

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

        var demos = Regex.Matches(html, "<iframe src=\"([^\"]+)\" data-demo-frame").Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(demos);
        foreach (var demo in demos)
        {
            // A .NET 10 demo may fail on purpose: then it's a 500 that names the exception in the frame (DemoCrash).
            var response = await client.GetAsync("/" + demo);
            var crashed = response.StatusCode == HttpStatusCode.InternalServerError
                && (await response.Content.ReadAsStringAsync()).Contains("data-crash");
            Assert.True(response.StatusCode == HttpStatusCode.OK || crashed, $"{demo} returned {(int)response.StatusCode}.");
        }
    }
}
