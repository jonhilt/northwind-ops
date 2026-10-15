---
id: environment-view
title: Content for some environments only
group: Rendering
order: 27
hook: A test-data banner that never reaches Production.
intro: Render a banner or a debug panel only in the environments you name, on the server or in the browser. New in .NET 11.
mode: Any render mode
demo: demo/environment-view/orders
before: demo/environment-view/orders/net10
labels: .NET 10: IWebAssemblyHostEnvironment | .NET 11: EnvironmentView
app:
---

## The problem

A debug panel runs in the browser on WebAssembly, but the server prerenders it first. How does it know the environment?

In .NET 10, no one service answered in both places, so every choice broke one of them.

### Ask WebAssembly's own service

```razor
@inject IWebAssemblyHostEnvironment Env

@if (!Env.IsProduction())
{
    <aside class="debug">...</aside>
}
```

The server prerenders the panel first, and it has no such service: the whole page fails with a 500. {.bad}

### Ask IHostEnvironment instead

```razor
@* needs Microsoft.Extensions.Hosting
   .Abstractions in the client project *@
@inject IHostEnvironment Env
```

It prerenders, then WebAssembly starts and has no such service: the panel crashes in the browser. {.bad}

### Don't prerender the panel

```razor
<DebugPanel @rendermode="new
    InteractiveWebAssemblyRenderMode(
        prerender: false)" />
```

No crash, but the panel only appears once WebAssembly has downloaded. {.bad}

## See it work

Each panel is prerendered on the server, then runs in your browser. Which page survives that, and what will its panel say?

On the left, the panel asks `IWebAssemblyHostEnvironment`, the .NET 10 way. On the right, it's wrapped in `<EnvironmentView>`.

1. The left page is an HTTP 500. The server prerendered the panel, and the server has no `IWebAssemblyHostEnvironment`.
2. The right page loads. The banner and the panel both name this server's environment: **Demo** on the live site, **Development** if you run the app yourself.
3. The panel's **Renderer** says WebAssembly: it's running in your browser. The server rendered it first, as Static, and asked the same service without failing.

Explain: .NET 11 registers `IHostEnvironment` in WebAssembly too, carrying the server's environment name, with no extra package. That's what fixes the crash: one service, on both sides. `<EnvironmentView>` is the shorthand that asks it for you.

## How it's built

What's different about the right-hand panel? Two .NET 11 changes, working together.

First, `IHostEnvironment` is now registered in WebAssembly: that line removes the crash. Then `<EnvironmentView>` is the shorthand on top: one tag, with `Include` or `Exclude`, no `@inject` and no `if`.

### The panel, the .NET 11 way: works on the server and in the browser
code: Client/Environments/DebugPanel.razor

### The same panel, the .NET 10 way: the highlighted line is the 500
code: Client/Environments/DebugPanelNet10.razor

### The page: a banner, and a warning for Production only
code: Components/Guide/Demos/Environments/RecentOrders.razor

## Good to know

- **Not a secret keeper.** A WebAssembly component's markup ships to the browser in its DLL, whatever the environment. Keep secrets on the server.
- **Comma-separated names.** `Include="Development,Staging"`. Case doesn't matter, and if a name is in both lists, `Exclude` wins.
- **Was EnvironmentBoundary.** It was renamed in Preview 6, so older posts use the old name.
