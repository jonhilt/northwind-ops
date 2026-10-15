using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;

namespace NorthwindOps.Components.Guide.Demos.Reconnect;

/// <summary>Called from Program.cs: what the browser-options guide demos need from the server.</summary>
public static class BrowserOptionsDemo
{
    public static void Use(WebApplication app)
    {
        // The "drop my Wi-Fi" button: this browser's own requests to /_blazor are turned away for 10 seconds.
        WifiDropoutMiddleware.Use(app);

        // The .NET 10 side. It starts Blazor by hand, so it can't go through this app's App.razor, which
        // starts it automatically: the component is the whole page. ?from=config picks the second way.
        app.MapGet("/demo/browser-options/pick-list/net10", (HttpContext context, string? from) =>
        {
            // A strict Content-Security-Policy refuses inline scripts: this one lets the page's own in by nonce.
            // Hex, not base64: Razor would HTML-encode a base64 "+" in the attribute, and it wouldn't match.
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            context.Response.Headers.ContentSecurityPolicy =
                $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; frame-ancestors 'self'";
            return new RazorComponentResult<PickListNet10>(new { From = from, Nonce = nonce });
        });
    }
}
