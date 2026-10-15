using Repro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var app = builder.Build();

// Strict policy: no 'unsafe-inline' in style-src. Add ?csp=off to compare without a policy.
app.Use(async (context, next) =>
{
    if (context.Request.Query["csp"] != "off")
        context.Response.Headers.ContentSecurityPolicy = "style-src 'self'";
    await next();
});

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
