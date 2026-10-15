namespace NorthwindOps.Components.Guide.Demos.SessionDraft;

/// <summary>Called from Program.cs. Session is ASP.NET Core's, as in .NET 10.</summary>
public static class SessionSetup
{
    // Program.cs calls this with the other services, before builder.Build().
    public static void AddDraftSession(this WebApplicationBuilder builder)
    {
        // xray:supply-parameter-from-session
        // Not new: where session data lives. This server's memory: lost on a restart, and
        // behind a load balancer you'd want a shared cache such as Redis instead.
        builder.Services.AddDistributedMemoryCache(cache => cache.SizeLimit = 16 << 20);
        builder.Services.AddSession(options =>
        {
            // Not new: the browser only holds a cookie, .AspNetCore.Session, naming its
            // session. Idle for 30 minutes (the default is 20) and the session is gone.
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.IsEssential = true;
        });
        // /xray:supply-parameter-from-session
    }

    // Program.cs calls this after builder.Build(), before app.MapRazorComponents<App>().
    public static void UseDraftSession(this WebApplication app)
    {
        // xray:supply-parameter-from-session
        // Not new: leave this out and the page throws "Session has not been configured".
        app.UseSession();
        // /xray:supply-parameter-from-session
    }
}
