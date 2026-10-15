# wasm-env-vars-config: .NET 10 repro

A .NET 10.0.102 Blazor Web App (WebAssembly interactivity, `Probe` rendered with `InteractiveWebAssemblyRenderMode(prerender: false)`) showing what happened before .NET 11 when the server wanted to tell one WebAssembly build where its API is. It uses the guide demo's values: the server's `appsettings.json` says `Orders:ApiUrl = demo/env-config/api/orders` (an API this repro maps), the client's `wwwroot/appsettings.json` says `https://staging-api.northwind.test/orders` and `Orders:PageSize = 5`.

.NET 10 has no C# API for the server to hand the runtime a variable (`new Microsoft.AspNetCore.Components.BrowserOptions()` fails with CS0234), so `App.razor` uses the hand-written workaround: `autostart="false"` plus `Blazor.start({ webAssembly: { configureRuntime: dotnet => dotnet.withEnvironmentVariable("Orders__ApiUrl", ...) } })`.

Recorded on 10.0.102 (`/`):

- `Environment.GetEnvironmentVariable("Orders__ApiUrl")` = `demo/env-config/api/orders`: the variable arrived.
- `Config["Orders:ApiUrl"]` = `https://staging-api.northwind.test/orders`: plain `IConfiguration` still reads the file. The only provider is `JsonStreamConfigurationProvider`.
- `GET https://staging-api.northwind.test/orders?take=5` = `HttpRequestException: TypeError: Failed to fetch` (net::ERR_NAME_NOT_RESOLVED).
- Layering `AddEnvironmentVariables()` on top gives `demo/env-config/api/orders`. In a .NET 10 client that call doesn't compile (CS1061) until you add the `Microsoft.Extensions.Configuration.EnvironmentVariables` package, which this repro references.

`/?variable=off` drops the variable, and everything falls back to staging.

Run it from this folder (global.json pins SDK 10.0.102): `ASPNETCORE_ENVIRONMENT=Development dotnet run --project EnvVarsRepro --no-launch-profile --urls http://localhost:5278`, then `node tools/guide-shots/probes/wasm-env-vars-config.mjs` from the repo root (BASE overrides the URL).
