using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NorthwindOps.Client.HostedServices;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── webassembly: the guide's WebAssembly demos call back to this app's server ──
var server = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddSingleton(new HttpClient { BaseAddress = server });

// xray:wasm-hosted-services
// One watcher for the whole app. The host starts it when WebAssembly starts.
builder.Services.AddSingleton<OrderWatcher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderWatcher>());
// /xray:wasm-hosted-services
// ── end webassembly ──

await builder.Build().RunAsync();
