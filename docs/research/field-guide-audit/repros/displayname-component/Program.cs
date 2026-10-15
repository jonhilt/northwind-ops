using DisplayNameRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.Run();
