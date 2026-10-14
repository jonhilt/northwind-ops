---
id: browser-options
title: Reconnection settings in C#
group: Interactive Server
order: 52
hook: Tune how the browser rejoins the server, without Blazor.start().
intro: Set how the browser reconnects, and other startup settings, in C# instead of a hand-written Blazor.start() script.
mode: Interactive Server
demo: demo/browser-options/pick-list
---

## The problem

Warehouse scanners lose Wi-Fi for a few seconds at a time. How do you make Blazor retry every 2 seconds?

In .NET 10, reconnection settings could only be passed to `Blazor.start()` in JavaScript, so apps did one of two things.

### Start Blazor yourself

```razor
<script src="_framework/blazor.web.js"
        autostart="false"></script>
<script>
  Blazor.start({ circuit: { reconnectionOptions: {
    maxRetries: 20,
    retryIntervalMilliseconds: 2000
  } } });
</script>
```

Settings in hand-written JavaScript, where nothing checks the names: a typo is silently ignored. {.bad}

### Write config values into that script

```razor
@inject IConfiguration Config

<script>
  Blazor.start({ circuit: { reconnectionOptions: {
    maxRetries: @Config["Reconnect:Tries"],
    retryIntervalMilliseconds:
      @Config["Reconnect:IntervalMs"]
  } } });
</script>
```

C# pasted into JavaScript by hand: one missing setting is a syntax error, and Blazor never starts. {.bad}

## See it work

Pick a line, then drop your Wi-Fi.

The button cuts off this browser only, for 10 seconds. The page was told in C# to retry every 2 seconds, 20 times (Blazor's default: 30).

1. Press **Pick** on a line or two, then **Drop my Wi-Fi for 10 seconds**.
2. "Rejoining the server" appears, then "trying again in 2 seconds". Behind it, "Rejoin attempt … of 20" counts up.
3. When the Wi-Fi comes back, the next attempt rejoins. Your picks are still there.

## How it's built

How do C# settings reach the browser?

Put them in a `BrowserOptions`. The server writes them into the page, and `blazor.web.js` applies them as it starts.

This demo sets them for its own page with `<ConfigureBrowser>`, so the rest of the site keeps Blazor's defaults. For every page, pass the same settings to `WithBrowserOptions` on `MapRazorComponents` in `Program.cs`.

### The page sets its reconnection options, and shows the browser using them
code: Components/Guide/Demos/Reconnect/PickList.razor

## Good to know

- **Don't mix the two in RC2.** Once `WithBrowserOptions` is set, `<ConfigureBrowser>` writes into the app-wide options, so one visit to this page would change every page, for everyone. This app doesn't call `WithBrowserOptions`, so each demo page keeps its own settings.
- **Only pages with an interactive component get them.** The settings are written just before the page's first Server, WebAssembly or Auto component. A fully static page gets none.
- **Callbacks still need JavaScript.** A retry-interval function, `configureSignalR` and a custom `reconnectionHandler` can't be serialized, so they stay in `Blazor.start()` or a JS initializer.
- **Hub settings live on the server.** For the hub's own options, like buffer sizes or transports, use `AddInteractiveServerRenderMode(o => o.ConfigureConnection = …)`, also new in .NET 11.
