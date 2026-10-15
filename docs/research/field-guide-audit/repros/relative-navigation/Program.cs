using RelNavRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();

var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.Run();
