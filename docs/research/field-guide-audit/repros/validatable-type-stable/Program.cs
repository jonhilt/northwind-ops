using NestedRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();

// Set NO_ADDVALIDATION=1 to see DataAnnotationsValidator fall back to top-level only.
if (Environment.GetEnvironmentVariable("NO_ADDVALIDATION") is null)
    builder.Services.AddValidation();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.Run();
