---
id: circuit-auto-pause
title: Auto-pause circuits in hidden tabs
badge: AutoPause · hidden tabs let go
release: RC2
mode: Interactive Server
sources: Components/Guide/Demos/CircuitPause/StockTake.razor, Components/Guide/Demos/CircuitPause/StockSheet.razor, Components/Guide/Demos/CircuitPause/StockPost.razor, wwwroot/js/auto-pause-net10.js
docs: https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#automatic-circuit-pause-on-tab-inactivity
summary: A new opt-in package pauses an Interactive Server circuit once its tab has been hidden for a while, and resumes it when the tab comes back. The circuit's memory is freed on the server, the [PersistentState] values wait in the browser, and it holds off while someone is halfway through typing.
---

## Try it

1. Open the stock take in the Field Guide: it runs the .NET 10 way (`/demo/auto-pause/stock-take/net10`, your own pause script) beside AutoPause (`/demo/auto-pause/stock-take`). Both pause a tab once it's been hidden for 10 seconds.
2. Press **+** on a few products on each side, press **Post count** on each, and at once switch to another browser tab for about 30 seconds. The warehouse takes 20 seconds to take a count.
3. Back again, both say the tab was paused, then resumed, and the counts are still there.
4. The .NET 10 side paused in the middle of the post. The post still finished on the server, so the warehouse has the count, but the tab says **Not posted**. AutoPause waited for the post, then paused: it says **Posted at** a time.

## The .NET 10 way

.NET 10 added `Blazor.pauseCircuit()` and `Blazor.resumeCircuit()`, but nothing called them for you. A hidden tab held its circuit until it was closed, so to free that memory you watched the tab yourself:

```js
let timer, paused = false;
document.addEventListener("visibilitychange", async () => {
  clearTimeout(timer);
  if (document.hidden) {
    timer = setTimeout(async () =>
      paused = await Blazor.pauseCircuit(), 120_000);
  } else if (paused) {
    paused = !await Blazor.resumeCircuit();
  }
});
```

That pauses whatever the user is doing. A click handler still running finishes on a circuit that has already saved its state and gone, so its result never reaches the resumed tab, and any other call between browser and server is cut off. Text half-typed into a bound field isn't sent until the field changes: the template's reconnect dialog takes the cursor when the pause starts, which sends it first, but with the default reconnect UI it's lost. Spotting any of this was up to you.

## How it works

Add the `Microsoft.AspNetCore.Components.Server.AutoPause` package and call `AddAutoPause()` on a `BrowserOptions`:

- **For every page:** `app.MapRazorComponents<App>().WithBrowserOptions(o => o.AddAutoPause())`.
- **For one page:** `<ConfigureBrowser Options="..." />` with options built by `new BrowserOptions().AddAutoPause(...)`, as the demo does.

`AutoPauseBrowserOptions.HiddenDelay` is how long a tab must stay hidden first: 2 minutes by default, and it must be more than zero. `Enabled = false` writes the settings but turns it off. `AddAutoPause` just adds two values to `InteractiveServer.Extensions`, which travel to the browser in the page's `<!--Blazor-Configuration:...-->` comment. No server services are involved.

In the browser, the package's JavaScript initializer reads them when Blazor starts. When the tab is hidden it starts a timer. When the timer fires, it holds off while:

- the focused text box, text area or contenteditable has a value that differs from the one it started with,
- an unmuted `<audio>` or `<video>` is playing,
- Picture-in-Picture is open,
- any Web Lock is held by the site,
- or a JavaScript interop call or stream between browser and server is still running.

It checks again every half second while the tab stays hidden, and gives up as soon as the tab is shown. Then it calls `Blazor.pauseCircuit()`: the server collects the `[PersistentState]` values, sends them to the browser encrypted with Data Protection, and disposes the circuit. When the tab is shown again it calls `Blazor.resumeCircuit()`, which starts a new circuit from that state. Components are created afresh, so `OnInitializedAsync` runs again: `StockSheet` reloads its products, and only `Counted`, `Note` and `StockPost`'s `Sheet` and `Posted` come back.

Gotchas:

- **RC2: `ConfigureBrowser` leaks when `WithBrowserOptions` is set.** `ConfigureBrowser` writes into the options object `WithBrowserOptions` shares between every page, so one visit to a page that turns auto-pause on turns it on everywhere, for everyone. This app doesn't call `WithBrowserOptions`, so auto-pause stays on this demo page.
- **Only persisted state survives.** Anything that isn't `[PersistentState]` (or registered with `PersistentComponentState`) is gone after a resume, as after any circuit pause.
- **The reconnect dialog opens while it's paused.** The app's `ReconnectModal` shows its "paused" state in the hidden tab, and closes when the tab comes back and resumes. If resuming fails, it stays, with its **Resume** button.
- **Not on every device.** A mobile browser may freeze or discard a backgrounded tab before the timer fires.
