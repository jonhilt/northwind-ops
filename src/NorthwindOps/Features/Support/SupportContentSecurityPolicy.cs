using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace NorthwindOps.Features.Support;

/// <summary>
/// Serves the Support pages with a strict Content-Security-Policy: no inline styles and no inline scripts.
/// It's scoped to /support so the other screens can adopt it when they're ready, rather than all at once.
/// </summary>
/// <remarks>
/// A browser applies a policy to a document. Blazor's enhanced navigation patches the next page into the current
/// document, so a header only takes effect on a full page load. Support.razor therefore also renders the policy
/// as a &lt;meta&gt; tag, which enhanced navigation inserts into &lt;head&gt; and the browser then enforces.
/// </remarks>
public sealed class SupportContentSecurityPolicy : IStartupFilter
{
    public static readonly PathString Path = "/support";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Path.StartsWithSegments(Path))
            {
                // xray:virtualize-csp
                // Set when the response starts, by which time routing has found the endpoint and its import map.
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.ContentSecurityPolicy = For(context);
                    return Task.CompletedTask;
                });
                // /xray:virtualize-csp
            }
            await nextMiddleware(context);
        });
        next(app);
    };

    // xray:virtualize-csp
    /// <summary>The policy for a Support page, as a response header.</summary>
    public static string For(HttpContext context) => string.Join("; ",
        ForMetaTag(context),
        // Interactive Server already sends "frame-ancestors 'self'" on its own (ContentSecurityFrameAncestorsPolicy).
        // This header replaces that one, so it carries the directive over. (It's not allowed in a <meta> tag.)
        "frame-ancestors 'self'");

    /// <summary>The same policy without the directives a &lt;meta&gt; tag can't carry.</summary>
    public static string ForMetaTag(HttpContext context) => string.Join("; ",
        "default-src 'self'",
        // No 'unsafe-inline': a style="..." attribute or a <style> block is refused. Stylesheets from this origin are fine.
        "style-src 'self'",
        // <ImportMap /> renders an inline <script type="importmap">, so it's allowed by its hash and nothing else inline is.
        $"script-src 'self'{ImportMapHash(context)}",
        // 'self' covers Blazor's WebSocket back to this origin.
        "connect-src 'self'",
        "img-src 'self' data:",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'");

    public static string ImportMapHash(HttpContext context)
    {
        // The same definition <ImportMap /> renders, and ToString() is exactly the JSON it writes into the script.
        var importMap = context.GetEndpoint()?.Metadata.GetMetadata<ImportMapDefinition>();
        return importMap is null
            ? ""
            : $" 'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(importMap.ToString())))}'";
    }
    // /xray:virtualize-csp
}
