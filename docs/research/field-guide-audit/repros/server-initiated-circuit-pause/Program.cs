using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using PauseRepro;
using PauseRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Same keys across restarts, so a paused tab's encrypted state can be read by the next process.
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("keys"));

// One CircuitTracker per circuit, so a component can get hold of its own Circuit.
builder.Services.AddScoped<CircuitTracker>();
builder.Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<CircuitTracker>());

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

Console.WriteLine($"[repro] process {Environment.ProcessId} started {DateTime.Now:HH:mm:ss}");
app.Run();
