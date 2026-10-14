---
id: wasm-env-vars-config
title: Server settings for WebAssembly
group: WebAssembly
order: 61
hook: One build, and the server tells it where the API is.
intro: Pass a setting from the server into WebAssembly as an environment variable, and read it with plain IConfiguration.
mode: Interactive WebAssembly
demo: demo/env-config/orders
app:
---

## The problem

You build your WebAssembly app once, then deploy it to test and to production. How does it find the right API in each?

WebAssembly in .NET 10 read its configuration only from files in `wwwroot`, so apps used one of two workarounds.

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

### Pass it in from JavaScript

```razor
<script>
  Blazor.start({ webAssembly: {
    configureRuntime: dotnet =>
      dotnet.withEnvironmentVariable(
        "ORDERS_API", "@Config["Orders:ApiUrl"]")
  } });
</script>
@* client reads Environment.GetEnvironmentVariable *@
```

A hand-written start-up script, and the value never reaches configuration, so you merge it with appsettings yourself. {.bad}

## See it work

Look where the settings came from, then switch the variable off.

This is a list of recent orders. It runs in your browser on WebAssembly, and calls the API it finds in configuration.

1. The badge says **Running on WebAssembly**, and the orders loaded from this server's own API.
2. Under the table: `Orders:ApiUrl` came from **an environment variable**, while `Orders:PageSize` still came from **wwwroot/appsettings.json**.
3. Click **Build's own settings**. Now the server passes nothing, so the same build falls back to the staging URL in its `appsettings.json`, and can't reach it.

## How it's built

How does a server setting become client configuration?

The page hands the WebAssembly runtime an environment variable. In .NET 11, `WebAssemblyHostBuilder.CreateDefault` adds environment variables to `IConfiguration`, after `appsettings.json`, so they win.

This demo sets the variable for its own page with `<ConfigureBrowser>`. For every page, pass the same options to `WithBrowserOptions` on `MapRazorComponents` in `Program.cs`.

### The page passes the API's address as a variable
code: Components/Guide/Demos/EnvConfig/RecentOrdersPage.razor

### The client reads it like any other setting
code: Client/EnvConfig/RecentOrders.razor

## Good to know

- **Double underscore for a colon.** `Orders__ApiUrl` becomes the key `Orders:ApiUrl`, as on the server.
- **Read once, at start.** The runtime gets its variables as WebAssembly starts. The demo's links load the page in full, so WebAssembly starts again with the new value.
- **Not a secret keeper.** The value is written into the page, and the browser can read it. Pass addresses and switches, never keys.
- **A breaking change.** A variable now overrides an `appsettings.json` key with the same name, which it didn't in .NET 10.
