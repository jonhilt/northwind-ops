---
id: browser-options
title: Reconnection settings in C#
group: Interactive Server
order: 52
hook: Tune how the browser rejoins the server, without Blazor.start().
intro: Set how the browser reconnects to the server in C#, instead of a hand-written Blazor.start() script. New in .NET 11. .NET 10 could already set the same options, but only in JavaScript.
mode: Interactive Server
demo: demo/browser-options/pick-list
before: demo/browser-options/pick-list/net10
labels: .NET 10: Blazor.start() in JavaScript | .NET 11: BrowserOptions in C#
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

Settings in hand-written JavaScript, where nothing checks the names: a typo is silently ignored, leaving Blazor's default: 30 tries, the first 10 at once with no wait, then 5 seconds apart, then 30. {.bad}

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

Both pages ask Blazor to retry every 2 seconds, 20 times, when the connection drops. The left page misspells both names. Drop the Wi-Fi on both: does the left page complain, or quietly do something else?

On the left, the settings go to `Blazor.start()` in JavaScript, the .NET 10 way. On the right, they're in C#. Under each pick list, **Sent to the browser** shows what the page sent. Once the Wi-Fi drops, the yellow banner counts the tries and times the waits between them.

1. Press **Pick** on a line on each side, then **Drop my Wi-Fi for 10 seconds** on each. Right: "Rejoin attempt 1 of 20", then "measured waits: 2 s ×1", ×2, ×3: a try every 2 seconds. Left: "11 of 30" straight away, then "measured waits: 0 s ×10, then 5 s ×1": Blazor's defaults (10 tries with no wait, then 5 seconds apart), and no warning anywhere.
2. Wait 10 seconds. Both rejoin, and your picks are still there.
3. On the left, choose **Settings from config**. The value that isn't set renders as `maxRetries: ,`. **Sent to the browser** says Blazor didn't start, and **Pick** does nothing.

Explain: `BrowserOptions` is a typed C# object, so a misspelt name doesn't compile and a missing value is just `null`, which keeps Blazor's default. The server writes the options into the page, and `blazor.web.js` applies them as it starts, with no `Blazor.start()` to write.

## How it's built

How do C# settings reach the browser?

Put them in a `BrowserOptions`. The server writes them into the page as a `<!--Blazor-Configuration:…-->` comment, in JSON: the C# names camelCased, and a `TimeSpan` as milliseconds, so `ReconnectionRetryInterval` arrives as `reconnectionRetryIntervalMilliseconds`. `blazor.web.js` applies them as it starts.

This demo sets them for its own page with `<ConfigureBrowser>`, so the rest of the site keeps Blazor's defaults. For every page, pass them to `WithBrowserOptions` in `Program.cs` instead (this app doesn't; see Good to know). A value bound from configuration as nullable keeps Blazor's default when it's missing:

```csharp
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .WithBrowserOptions(o => o.InteractiveServer
       .ReconnectionMaxRetries =
           config.GetValue<int?>("Reconnect:Tries"));
```

### .NET 11: the page's options, in C#
code: Components/Guide/Demos/Reconnect/PickList.razor

### For comparison, not the feature: .NET 10's Blazor.start() script
code: Components/Guide/Demos/Reconnect/PickListNet10.razor

## Good to know

- **Don't mix the two in RC2.** Once `WithBrowserOptions` is set, `<ConfigureBrowser>` writes into the app-wide options, so one visit to this page would change every page, for everyone. This app doesn't call `WithBrowserOptions`, so each demo page keeps its own settings.
- **Only pages with an interactive component get them.** The settings are written just before the page's first Server, WebAssembly or Auto component. A fully static page gets none.
- **Callbacks still need JavaScript.** A retry-interval function, `configureSignalR` and a custom `reconnectionHandler` can't be serialized, so they stay in `Blazor.start()` or a JS initializer.
- **Hub settings live on the server.** For the hub's own options, like buffer sizes or transports, use `AddInteractiveServerRenderMode(o => o.ConfigureConnection = …)`, also new in .NET 11.
