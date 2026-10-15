using CacheViewRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddMemoryCache();   // workaround 1
builder.Services.AddOutputCache();   // workaround 2

var app = builder.Build();
app.UseOutputCache();
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.Run();
