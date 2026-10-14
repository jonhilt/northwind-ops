---
id: environment-view
title: Content for some environments only
group: Rendering
order: 27
hook: A test-data banner that never reaches Production.
intro: Render a banner or a debug panel only in the environments you name, on the server or in the browser.
mode: Any render mode
demo: demo/environment-view/orders
app:
---

## The problem

A "test data" banner must never show in Production. How do you keep it out?

Blazor in .NET 10 had no component for this, so apps asked the host environment themselves, with a different service in each render mode.

### On the server

```razor
@inject IWebHostEnvironment Env

@if (!Env.IsProduction())
{
    <div class="env-banner">
        @Env.EnvironmentName · test data
    </div>
}
```

Fine on the server, but that service doesn't exist in the browser, so this can't run on WebAssembly. {.bad}

### And again in WebAssembly

```razor
@inject IWebAssemblyHostEnvironment Env

@if (!Env.IsProduction())
{
    <aside class="debug">...</aside>
}
```

A different service, so a component used in both render modes needs two code paths, or an abstraction of your own. {.bad}

## See it work

Look at the banner, then the debug panel.

This is a small orders page, running for real. It's static, with a debug panel that runs in your browser on WebAssembly.

1. The striped banner names this server's environment: **Demo** on the live site, **Development** if you run the app yourself. Neither is Production, so `Exclude="Production"` lets it render.
2. The debug panel's **Renderer** says WebAssembly, so it's running in your browser. It names the same environment as the banner.
3. The page also has a warning for Production only. Open the demo in its own tab and view its source: the warning isn't hidden, it was never rendered.

## How it's built

Wrap the content in `<EnvironmentView>`, and name the environments to `Include` or `Exclude`.

It reads `IHostEnvironment`, which .NET 11 registers in WebAssembly too, so it works in every render mode.

### The page: a banner, and a warning for Production only
code: Components/Guide/Demos/Environments/RecentOrders.razor

### The debug panel, running on WebAssembly
code: Client/Environments/DebugPanel.razor

## Good to know

- **Comma-separated names.** `Include="Development,Staging"`. Case doesn't matter, and if a name is in both lists, `Exclude` wins.
- **The browser gets the server's environment.** A Blazor Web App passes its environment name to WebAssembly, so both sides agree.
- **Not a secret keeper.** A WebAssembly component's markup ships to the browser in its DLL, whatever the environment. Keep secrets on the server.
- **Was EnvironmentBoundary.** It was renamed in Preview 6, so older posts use the old name.
