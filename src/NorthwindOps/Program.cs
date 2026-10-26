using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NorthwindOps.Components;
using NorthwindOps.Components.Guide.Demos;
using NorthwindOps.Components.Showcase;
using NorthwindOps.Data;
using NorthwindOps.Data.Seed;
using NorthwindOps.Showcase;

var builder = WebApplication.CreateBuilder(args);

// Interactive components render on the circuit's threads, which start out in the server's own culture
// (invariant in the container, whatever the OS uses elsewhere), not the request's. Make US English the default
// everywhere so prices read "$1,380" in an interactive island as well as on a static page.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new("en-US");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDbContextFactory<NorthwindDb>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("Northwind")));
builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DemoData>();
builder.Services.AddHostedService<HourlyDemoDataReset>();

// Dashboard: KPI and panel queries, and the simulated live order feed.
builder.Services.AddSingleton<NorthwindOps.Features.Dashboard.DashboardQueries>();
builder.Services.AddSingleton<NorthwindOps.Features.Dashboard.OrderFeed>();
builder.Services.AddHostedService(services => services.GetRequiredService<NorthwindOps.Features.Dashboard.OrderFeed>());
// xray:renderfragment-across-render-modes
// Static ChildContent passed into an interactive component travels to the server inside the component's
// marker when the circuit starts. The dashboard's two panels need more than SignalR's 32 KB default.
builder.Services.AddServerSideBlazor().AddHubOptions(hub => hub.MaximumReceiveMessageSize = 64 * 1024);
// /xray:renderfragment-across-render-modes

builder.Services.AddSingleton<FeatureRegistry>();
builder.Services.AddSingleton<SourceCatalog>();
builder.Services.AddSingleton<GuideRegistry>();
builder.Services.AddHealthChecks();

// Order detail & edit screen. Its TempData flash needs no registration: AddRazorComponents() sets TempData up.
builder.Services.AddSingleton<IDemoDataSeeder, OrderNotesSeeder>();

// ── Suppliers screen ──
builder.Services.AddSingleton<IDemoDataSeeder, SupplierSeeder>();
builder.Services.AddSingleton<NorthwindOps.Features.Suppliers.SupplierDirectory>();
builder.Services.AddSingleton<NorthwindOps.Features.Suppliers.VatRegister>();
// xray:validation-localization
builder.Services.AddLocalization();
// /xray:validation-localization
// xray:validatable-type-stable
builder.Services.AddValidation(options =>
// /xray:validatable-type-stable
// xray:validation-localization
{
    // Look up the Suppliers screen's validation messages in SupplierText.resx, whichever form type they're on.
    var fallback = options.LocalizerProvider;
    var suppliers = typeof(NorthwindOps.Features.Suppliers.SupplierText);
    options.LocalizerProvider = (type, factory) => type.Namespace == suppliers.Namespace ? factory.Create(suppliers) : fallback(type, factory);
});
// /xray:validation-localization
// ── end Suppliers screen ──

// ── Field Guide, forms entries ──
// The validation-localization demo's .NET 11 form reads its German messages from SupplierMessages, a plain IStringLocalizer.
NorthwindOps.Components.Guide.Demos.ValidationLocalization.ValidationLocalizationSetup.AddSupplierMessages(builder.Services);
// ── end forms entries ──

// Support screen: tickets, their demo data, and a strict Content-Security-Policy on /support.
builder.Services.AddSingleton<IDemoDataSeeder, SupportSeeder>();
builder.Services.AddSingleton<NorthwindOps.Features.Support.SupportDesk>();
// xray:virtualize-csp
builder.Services.AddTransient<IStartupFilter, NorthwindOps.Features.Support.SupportContentSecurityPolicy>();
// /xray:virtualize-csp

// Purchasing screen and the session guide entry: drafts kept in session through [SupplyParameterFromSession].
// xray:supply-parameter-from-session
NorthwindOps.Components.Guide.Demos.SessionDraft.SessionSetup.AddDraftSession(builder);
// /xray:supply-parameter-from-session
builder.Services.AddSingleton<IDemoDataSeeder, PurchasingSeeder>();

// ── server-pause: the Field Guide's circuit pause demos ──
// A scoped CircuitHandler gives each circuit's components their own Circuit (and only theirs), and counts this
// server's circuits. Auto-pause needs no services: the demo page turns it on with <ConfigureBrowser>.
builder.Services.AddSingleton<NorthwindOps.Components.Guide.Demos.CircuitPause.CircuitBoard>();
builder.Services.AddScoped<NorthwindOps.Components.Guide.Demos.CircuitPause.CircuitTracker>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>(services =>
    services.GetRequiredService<NorthwindOps.Components.Guide.Demos.CircuitPause.CircuitTracker>());
// ── end server-pause ──

// ── Field Guide, cacheview: the .NET 10 side of the demo caches its report's data in IMemoryCache ──
builder.Services.AddMemoryCache();
// ── end cacheview ──

// ── webassembly: WebAssembly components carry on in the culture the server rendered them in ──
NorthwindOps.Components.Guide.Demos.WasmCulture.WasmCultureSetup.UseCultureFromServer(builder);
// ── end webassembly ──

// ── Security: a year of HSTS, and per-visitor rate limits on what costs the server work ──
builder.Services.AddHsts(hsts => hsts.MaxAge = TimeSpan.FromDays(365));
NorthwindOps.Security.RateLimits.Add(builder);
// ── end security ──

var app = builder.Build();

// The demo database is rebuilt on start and then hourly, so visitors can't leave it in a mess for long.
await app.Services.GetRequiredService<DemoData>().ResetAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// ── Field Guide: a demo that crashes, like the .NET 10 side of a before/after pair, shows why in its frame ──
// Inside the app-wide handler, so it catches a demo's error first.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/demo"), demo => demo.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = async context =>
    {
        // A fresh scope, as the failed render has already used the request's.
        await using var scope = context.RequestServices.CreateAsyncScope();
        context.RequestServices = scope.ServiceProvider;
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()!.Error;
        await new RazorComponentResult<DemoCrash>(new { Error = error }).ExecuteAsync(context);
    },
}));
// ── end Field Guide ──
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
// ── Security: nosniff, Referrer-Policy and a Content-Security-Policy on every response; rate limits before any work ──
NorthwindOps.Security.SecurityHeaders.Use(app);
app.UseRateLimiter();
// ── end security ──
app.UseRequestLocalization("en-US");
// ── Suppliers screen: ?culture=de-DE switches the supplier pages to German ──
// xray:validation-localization
app.UseWhen(context => context.Request.Path.StartsWithSegments("/suppliers"), suppliers => suppliers.UseRequestLocalization(options =>
{
    options.AddSupportedCultures("en-US", "de-DE").AddSupportedUICultures("en-US", "de-DE").SetDefaultCulture("en-US");
    options.RequestCultureProviders = [new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider()];
}));
// /xray:validation-localization
// ── end Suppliers screen ──
// ── webassembly: ?culture=de-DE switches the guide's WebAssembly invoice demo, as in any localized app ──
app.UseWhen(context => context.Request.Path.StartsWithSegments("/demo/wasm-culture"), invoice => invoice.UseRequestLocalization(options =>
{
    options.AddSupportedCultures("en-US", "en-GB", "de-DE", "fr-FR").AddSupportedUICultures("en-US", "en-GB", "de-DE", "fr-FR").SetDefaultCulture("en-US");
    options.RequestCultureProviders = [new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider()];
}));
// ── end webassembly ──

// ── Field Guide, forms entries: ?culture=de-DE switches these demos to German ──
app.UseWhen(context => context.Request.Path.StartsWithSegments("/demo/validation-localization")
    || context.Request.Path.StartsWithSegments("/demo/displayname"), demo => demo.UseRequestLocalization(options =>
{
    options.AddSupportedCultures("en-US", "de-DE").AddSupportedUICultures("en-US", "de-DE").SetDefaultCulture("en-US");
    options.RequestCultureProviders = [new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider()];
}));
// ── end forms entries ──

// Purchasing screen: session middleware has to run before the request reaches the Razor Components endpoint.
// xray:supply-parameter-from-session
NorthwindOps.Components.Guide.Demos.SessionDraft.SessionSetup.UseDraftSession(app);
// /xray:supply-parameter-from-session

// ── server-options: the browser-options guide demos: the "drop my Wi-Fi" button, and the .NET 10 page ──
// While a browser holds the demo's 10-second cookie, its own requests to /_blazor are turned away. Nobody else's are.
NorthwindOps.Components.Guide.Demos.Reconnect.BrowserOptionsDemo.Use(app);
// ── end server-options ──

// ── Security: a post the pages can't read is the visitor's mistake, so 400 rather than 500 ──
// That's a POST that isn't a form at all (Blazor's own /_blazor traffic aside), or a form past the limits on
// MapRazorComponents below.
app.Use(async (context, next) =>
{
    // The raw header: Request.HasFormContentType throws once a cross-site post has failed its antiforgery check.
    var contentType = context.Request.ContentType ?? "";
    var isForm = contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
    if (HttpMethods.IsPost(context.Request.Method) && !isForm && !context.Request.Path.StartsWithSegments("/_blazor"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    try { await next(context); }
    catch (InvalidDataException) when (!context.Response.HasStarted) { context.Response.StatusCode = StatusCodes.Status400BadRequest; }
});

app.MapStaticAssets();
app.MapHealthChecks("/healthz");
// The commit this image was built from (deploy.sh passes it in as GIT_SHA). The Deploy workflow polls this to confirm
// the live site is serving a merge. no-store keeps Cloudflare and browsers from answering with an old one.
app.MapGet("/version", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Environment.GetEnvironmentVariable("GIT_SHA") ?? "unknown";
});

// X-ray drawer content: a Razor component rendered as an HTML fragment, fetched by wwwroot/js/xray.js.
app.MapGet("/xray/{id}/panel", IResult (string id, FeatureRegistry registry) =>
    registry.Find(id) is null
        ? TypedResults.NotFound()
        : new RazorComponentResult<XRayPanel>(new { Id = id, Tabbed = true }));

// ── webassembly: the APIs the guide's WebAssembly demos call from the browser ──
NorthwindOps.Components.Guide.Demos.HostedServices.OrderUpdatesApi.MapOrderUpdatesApi(app);
NorthwindOps.Components.Guide.Demos.EnvConfig.OrdersApi.MapOrdersApi(app);
// ── end webassembly ──

// ── Field Guide, web-worker: the order-line CSV that the Web Worker demo downloads and totals ──
// Field Guide: the old three-step pages (/guide/{id}, /try, /code) now live on as one walkthrough at /learn/{id}.
app.MapGet("/guide/{id}/{step:regex(^(try|code)$)?}", IResult (string id, GuideRegistry guide) =>
    guide.Find(id) is { } entry ? Results.LocalRedirect($"~/learn/{entry.Id}", permanent: true) : Results.NotFound());

app.MapGet("/demo/web-worker/order-lines.csv", NorthwindOps.Components.Guide.Demos.WebWorker.OrderLinesCsv.GetAsync);
// ── end web-worker ──

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(NorthwindOps.Client._Imports).Assembly)
    // ── Security: the demo database is shared and SQLite ignores column lengths, so no posted field may be large ──
    // Each form checks its own lengths too; this backs them up, from the default 4 MB a field down to 16 KB.
    .WithFormOptions(valueLengthLimit: 16 * 1024, valueCountLimit: 256);

app.Run();
