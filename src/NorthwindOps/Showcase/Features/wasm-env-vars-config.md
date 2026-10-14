---
id: wasm-env-vars-config
title: Environment variables flow into WebAssembly configuration
badge: IConfiguration · env vars in WebAssembly
release: Preview 1
mode: Interactive WebAssembly
sources: Components/Guide/Demos/EnvConfig/RecentOrdersPage.razor, Client/EnvConfig/RecentOrders.razor
docs: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/wasm-env-vars-in-configuration.md
summary: WebAssemblyHostBuilder.CreateDefault now adds environment variables to IConfiguration, after wwwroot/appsettings.json, so a variable overrides the file. With BrowserOptions the server can pass those variables in C#, so one WebAssembly build picks up its API address, or any other setting, from the server it's deployed to.
---

## Try it

1. Open the recent orders demo at `/demo/env-config/orders`, or step through it in the Field Guide. The page is static SSR. The list runs in the browser with `@rendermode` `InteractiveWebAssembly`, without prerendering.
2. The badge says **Running on WebAssembly**. The list called `demo/env-config/api/orders?take=5` on this server.
3. Under the table, `Orders:ApiUrl` came from an environment variable: the page passed `Orders__ApiUrl` to the runtime. `Orders:PageSize` came from `wwwroot/appsettings.json`, which the variable doesn't touch.
4. Click **Build's own settings**. The page loads in full, without the variable this time. The same build now reads `Orders:ApiUrl` from its `appsettings.json`, a staging address that doesn't exist, and says it couldn't reach it.
5. View the page source of the first version. Before the list's `<!--Blazor:...-->` marker there's a `<!--Blazor-Configuration:...-->` comment. Its base64 JSON includes `"environmentVariables":{"Orders__ApiUrl":"demo/env-config/api/orders"}` under `webAssembly`. With the variable off, that dictionary is empty.

## The .NET 10 way

WebAssembly read configuration from `wwwroot/appsettings.json` and `wwwroot/appsettings.{Environment}.json`, and nothing else. Settings that differed between deployments went one of three ways:

- **A file per environment.** Every environment's URL shipped inside the build, and a new server or a changed URL meant a rebuild.
- **An endpoint the client called at start-up.** `Program.cs` fetched its settings from the server and added them with `AddInMemoryCollection`, costing a round trip before the app could start.
- **JavaScript.** You turned off `autostart` and called `Blazor.start({ webAssembly: { configureRuntime: dotnet => dotnet.withEnvironmentVariable("ORDERS_API", "...") } })`. The value was a real environment variable, but `IConfiguration` didn't see it. You read it with `Environment.GetEnvironmentVariable` and merged it with the file's settings yourself.

## How it works

Two pieces meet here.

**The client.** `WebAssemblyHostBuilder.CreateDefault` builds the host, which adds `appsettings.json` and `appsettings.{Environment}.json` from `wwwroot`, and then calls `Configuration.AddEnvironmentVariables()`. In configuration the last source added wins, so a variable overrides a file key with the same name. Variable names follow the usual rule: `__` stands for `:`, so `Orders__ApiUrl` is read as `Orders:ApiUrl`. The demo's `SourceOf` helper shows this: it finds the last configuration provider that holds the key.

**The server.** `BrowserOptions.InteractiveWebAssembly.EnvironmentVariables` is a dictionary that the server writes into the page with the rest of the browser options. When `blazor.web.js` starts the WebAssembly runtime, it passes them in with `withEnvironmentVariables`. Set it for every page with `MapRazorComponents<App>().WithBrowserOptions(o => o.InteractiveWebAssembly.EnvironmentVariables["Orders__ApiUrl"] = url)`, or for one page with `<ConfigureBrowser Options="..." />`, which is what the demo does. Variables can also come from anything else that starts the runtime with them, such as the Blazor Gateway host or Aspire.

Gotchas:

- **Read once, at start.** Variables reach the runtime only as it starts. On an enhanced navigation WebAssembly keeps running with the values it started with, which is why the demo's two links use `data-enhance-nav="false"`.
- **Visible to anyone.** The variables are in the page source, and the client's code is downloadable. Only pass values that are safe to publish.
- **Behaviour change.** Apps that already passed variables through `configureRuntime` will now also see them in `IConfiguration`, overriding any `appsettings.json` key with the same name. The official breaking-change note covers this.
- **RC2: don't mix `WithBrowserOptions` and `<ConfigureBrowser>`.** Once `WithBrowserOptions` is set, `<ConfigureBrowser>` writes into the app-wide options object, so one page's values leak into every later request. This app uses `<ConfigureBrowser>` on several demo pages, so it doesn't call `WithBrowserOptions`.
- **Only pages with an interactive component get them.** The options are written just before the first interactive component's marker, which a WebAssembly component always has.
