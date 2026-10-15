using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NorthwindOps.Client.HostedServices;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── webassembly: the guide's WebAssembly demos call back to this app's server ──
var server = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddSingleton(new HttpClient { BaseAddress = server });

// One instance, so every page's bell can inject the same watcher.
builder.Services.AddSingleton<OrderWatcher>();

// xray:wasm-hosted-services
// New in .NET 11: the WebAssembly host starts this. (.NET 10 ignored it.)
// The lambda hands the host the singleton above. AddHostedService<OrderWatcher>()
// would make a second watcher: the host would start that one, and the bells
// would listen to the one that never runs.
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderWatcher>());
// /xray:wasm-hosted-services
// ── end webassembly ──

await builder.Build().RunAsync();
