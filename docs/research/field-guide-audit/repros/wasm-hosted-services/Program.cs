using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WasmHostedRepro;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// The guide's .NET 11 registration. Compiles on .NET 10 ... and nothing ever starts it.
builder.Services.AddSingleton<HostedWatcher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HostedWatcher>());

// The .NET 10 workaround: register a singleton and start it yourself.
builder.Services.AddSingleton<ManualWatcher>();

var host = builder.Build();
_ = host.Services.GetRequiredService<ManualWatcher>().StartAsync(CancellationToken.None);
await host.RunAsync();
