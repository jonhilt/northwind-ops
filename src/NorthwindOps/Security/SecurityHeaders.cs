using NorthwindOps.Features.Support;

namespace NorthwindOps.Security;

/// <summary>
/// Headers every response carries. Visitors share one database, so the Content-Security-Policy is the backstop if their
/// text ever reached the page as markup: only this origin's scripts run.
/// </summary>
public static class SecurityHeaders
{
    public static void Use(WebApplication app) => app.Use(async (context, next) =>
    {
        // Set when the response starts, by which time routing has found the endpoint and its import map.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // A page that sets a stricter policy of its own keeps it (Support, the CSP guide demo). Interactive
            // Server's lone "frame-ancestors 'self'" is replaced: this policy carries that directive over.
            var own = headers.ContentSecurityPolicy.ToString();
            if (own is "" or "frame-ancestors 'self'")
            {
                headers.ContentSecurityPolicy = ContentSecurityPolicy(context);
            }
            return Task.CompletedTask;
        });
        await next(context);
    });

    public static string ContentSecurityPolicy(HttpContext context) => string.Join("; ",
        "default-src 'self'",
        // WebAssembly pages compile .NET's runtime, which needs 'wasm-unsafe-eval' (not 'unsafe-eval').
        // <ImportMap /> is the one inline script, allowed by its hash.
        $"script-src 'self' 'wasm-unsafe-eval'{SupportContentSecurityPolicy.ImportMapHash(context)}",
        // Components across the app (Virtualize's spacers among them) render style="..." attributes. Support refuses them.
        "style-src 'self' 'unsafe-inline'",
        "img-src 'self' data:",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        // Interactive Server pages send this on their own; this header replaces theirs, so it carries it over.
        "frame-ancestors 'self'");
}
