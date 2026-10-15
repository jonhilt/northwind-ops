---
id: circuit-auto-pause
title: Pause hidden tabs
group: Interactive Server
order: 50
hook: A hidden tab gives back its circuit, then picks up where it left off.
intro: Free the server memory a hidden tab's circuit holds, without cutting off work still under way. New in .NET 11.
mode: Interactive Server
demo: demo/auto-pause/stock-take
before: demo/auto-pause/stock-take/net10
labels: .NET 10: your own pause script | .NET 11: AutoPause
---

## The problem

Staff leave tabs open all day. Why does every hidden one still hold memory on your server?

An Interactive Server tab keeps its circuit while it's open, even hidden. .NET 10 could already pause a circuit and resume it later, but only when your own script asked.

### Do nothing

```csharp
// A hidden tab keeps its circuit, and the
// server keeps its memory, until the tab
// is closed.
```

Simple, and a server full of forgotten tabs. {.bad}

### Pause it yourself when the tab hides

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

It pauses whatever is going on. A save still running finishes on a circuit that's gone, so the tab comes back without its result. {.bad}

## See it work

Each side posts its count to a slow warehouse system, and you hide this tab while the post is still running. When you come back, which side knows its count was posted?

On the left, a script of your own pauses the tab, the .NET 10 way. On the right, AutoPause does. Both pause once the tab has been hidden for 10 seconds. The warehouse takes 20 seconds to take a count.

1. Press **+** on a few products on each side. Then press **Post count** on each side, and at once switch to another browser tab for about 30 seconds.
2. Back here, both panels say the tab was paused, then resumed on a new circuit, and the counts are all there. Pausing worked in .NET 10 too.
3. On the left, the post says **Not posted**, yet the line under it says the warehouse has this count once. The script paused the tab mid-post, so its state was saved before `Posted` was set. Press **Post count** again: 20 seconds later the warehouse has it twice.
4. On the right, it says **Posted at** a time, and the warehouse has it once. Both tabs were hidden for the same 30 seconds, but the right panel says it was paused for about 10 s, the left for about 20 s: AutoPause waited the 20 seconds the post took, so it paused later.

Explain: The script paused 10 seconds after the tab hid, in the middle of the post. The post still finished on the server, but its circuit had already saved its state and gone, so "Posted" was never saved, and nothing tells the user the count went through. AutoPause holds off while the browser is still waiting on the server, as it is until a click handler returns, then pauses with the result saved.

## How it's built

What does .NET 11 add? A package that watches the tab for you, and waits until it's safe.

Install it with `dotnet add package Microsoft.AspNetCore.Components.Server.AutoPause`. There's nothing to register in `Program.cs`: `AddAutoPause()` only writes settings into the page, and the package's own script reads them in the browser. Here one page turns it on with `<ConfigureBrowser>`, also new in .NET 11. Pausing itself, and the `[PersistentState]` values it keeps, work as in .NET 10.

### .NET 10: your own script pauses the tab
code: wwwroot/js/auto-pause-net10.js

### .NET 11: the page turns auto-pause on
code: Components/Guide/Demos/CircuitPause/StockTake.razor

### Not new: the post, a click handler that takes 20 seconds
code: Components/Guide/Demos/CircuitPause/StockPost.razor

## Good to know

- **It waits for typing too (not shown here).** It holds off while the focused text box holds new typing, sound is playing, or Picture-in-Picture or a Web Lock is active, and checks again every half second. A script's pause usually keeps half-typed text only because the template's reconnect dialog takes the cursor, which sends it first.
- **Only `[PersistentState]` survives.** A resumed tab gets a new circuit, so components start again and reload their data. The saved state waits in the browser, encrypted.
- **For every page, in `Program.cs`.** `app.MapRazorComponents<App>().WithBrowserOptions(o => o.AddAutoPause())`, with a 2-minute wait by default.
- **RC2: a page's setting can leak to every page.** If `Program.cs` calls `WithBrowserOptions`, a page's `<ConfigureBrowser>` writes into those shared options, so one visit changes every page for everyone. This app doesn't call it, so auto-pause stays on this page.
