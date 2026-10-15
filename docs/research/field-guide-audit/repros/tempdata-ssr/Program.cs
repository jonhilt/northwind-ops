using TempDataRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();   // .NET 10: no TempData for components

var app = builder.Build();
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.Run();
