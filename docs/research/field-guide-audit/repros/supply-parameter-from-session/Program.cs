using SessionRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();
app.UseSession();
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.Run();
