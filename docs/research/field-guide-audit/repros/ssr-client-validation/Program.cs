using Repro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents(); // static SSR only: no interactive render modes, no circuit

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.Run();
