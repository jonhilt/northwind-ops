namespace NorthwindOps.Components.Guide.Demos.Reconnect;

/// <summary>
/// Demo only: the browser-options demo's "drop my Wi-Fi" button. While a browser holds the
/// short-lived <c>northwind-dropout</c> cookie (set by WifiDropout.razor.js), the server turns away
/// that browser's requests to Blazor's hub, so its reconnection attempts fail until the cookie
/// expires. Nothing is stored on the server, and other visitors are unaffected.
/// </summary>
public static class WifiDropoutMiddleware
{
    public const string Cookie = "northwind-dropout";

    public static void Use(IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/_blazor") && context.Request.Cookies.ContainsKey(Cookie))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Demo: this browser's Wi-Fi is off for a few seconds.");
            return;
        }
        await next(context);
    });
}
