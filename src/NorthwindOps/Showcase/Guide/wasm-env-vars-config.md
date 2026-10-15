---
id: wasm-env-vars-config
title: Server settings for WebAssembly
group: WebAssembly
order: 61
hook: One build, and the server tells it where the API is.
intro: Pass a setting from the server into WebAssembly as an environment variable, and read it with plain IConfiguration. New in .NET 11, and a breaking change: a variable now beats an appsettings.json key with the same name.
mode: Interactive WebAssembly
demo: demo/env-config/orders
before: demo/env-config/orders/net10
labels: .NET 10 · recorded | .NET 11 · live
app:
---

## The problem

You build your WebAssembly app once, then deploy it to test and to production. How does it find the right API in each?

By default, WebAssembly in .NET 10 read configuration only from files in `wwwroot`. To let the server decide, apps went one of two ways.

### A settings file per environment

```json
// wwwroot/appsettings.Production.json
{
  "Orders": {
    "ApiUrl": "https://api.northwind.com/orders"
  }
}
```

Every URL is baked into the build. A new server, or a changed URL, means building and deploying again. {.bad}

### Pass it in from a start-up script

```razor
<script src="_framework/blazor.web.js"
        autostart="false"></script>
<script>
  Blazor.start({ webAssembly: {
    configureRuntime: dotnet =>
      dotnet.withEnvironmentVariable(
        "Orders__ApiUrl", "@apiUrl") } });
</script>
```

You hand-write JavaScript to start Blazor, and even then `IConfiguration` reads the file. The variable counts only once you add `AddEnvironmentVariables()` and its package to the client. {.bad}

## See it work

Both servers hand the same build their API's address as `Orders__ApiUrl`. Which address does the client's plain `Config["Orders:ApiUrl"]` return: the server's, or the staging one in `wwwroot/appsettings.json`?

.NET 11 changed how WebAssembly builds its configuration, so the old behaviour can't run here. The left is what our .NET 10 repro read, recorded: its server passed the variable from a `Blazor.start` script. The right is live: its server sets it in C#, with no script, and the client has no extra line or package.

1. On the left, the **Variable** column shows the server's `demo/env-config/api/orders` arrived. But **Config[key]** returned the staging URL, so the list called staging and couldn't reach it.
2. Under it, .NET 10's own fix: with `AddEnvironmentVariables()` and its package added, the same key gave the server's address. The start-up script was still needed.
3. On the right, wait for **Running on WebAssembly**. **Config[key]** is the variable's `demo/env-config/api/orders`, and **Calling** shows it resolved against this server, `http://localhost…/demo/env-config/api/orders`. The orders load. `Orders:PageSize` has no variable, so it still comes from the file.
4. Click **Build's own settings**. The server passes nothing now, and the same build falls back to staging, like .NET 10 always did.

Explain: The server's. In .NET 11, `WebAssemblyHostBuilder.CreateDefault` adds environment variables to configuration after the `appsettings` files, so a variable beats the file with no extra line or package. And the server sets it in C# with `BrowserOptions`, instead of a start-up script. .NET 10 could get there too, with the script, the line and the package.

## How it's built

How does a server setting become client configuration?

The page hands the WebAssembly runtime an environment variable, and the client reads it as an ordinary setting.

This demo sets the variable for its own page with `<ConfigureBrowser>`, below. To set it for every page, which is what most apps want, use `WithBrowserOptions` in the server's `Program.cs` instead, reading the address from the server's own configuration:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .WithBrowserOptions(o => o.InteractiveWebAssembly
        .EnvironmentVariables["Orders__ApiUrl"] = app.Configuration["Orders:ApiUrl"]);
```

Use one or the other. In RC2, once `WithBrowserOptions` is set, a page's `<ConfigureBrowser>` writes into the app-wide options, so its values leak into every later page.

### The page passes the API's address as a variable
code: Components/Guide/Demos/EnvConfig/RecentOrdersPage.razor

### The client reads it like any other setting
code: Client/EnvConfig/RecentOrders.razor

## Good to know

- **Double underscore for a colon.** `Orders__ApiUrl` becomes the key `Orders:ApiUrl`, as on the server.
- **A breaking change.** A variable now overrides an `appsettings.json` key with the same name. An app that already passed variables from `Blazor.start`, or gets them from its host, will see them in `IConfiguration` too, so check for stray ones when you upgrade.
- **Read once, at start.** The runtime gets its variables as WebAssembly starts. The demo's links load the page in full, so WebAssembly starts again with the new value.
- **Not a secret keeper.** The value is written into the page, and the browser can read it. Pass addresses and switches, never keys.
