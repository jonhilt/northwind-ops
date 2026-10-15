using AutoPauseRepro;
using AutoPauseRepro.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
// Counts the circuits this server is holding in memory right now.
builder.Services.AddSingleton<CircuitCounter>();
builder.Services.AddScoped<CountingHandler>();
builder.Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<CountingHandler>());

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/circuits", (CircuitCounter c) => new { open = c.Open, opened = c.Opened, closed = c.Closed });
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
