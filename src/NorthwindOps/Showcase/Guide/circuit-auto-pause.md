---
id: circuit-auto-pause
title: Pause hidden tabs
group: Interactive Server
order: 50
hook: A hidden tab gives back its circuit, then picks up where it left off.
intro: Free the server memory a hidden tab's circuit holds, without losing what's on its screen.
mode: Interactive Server
demo: demo/auto-pause/stock-take
---

## The problem

Staff leave tabs open all day. Why does every hidden one still hold memory on your server?

An Interactive Server tab keeps its circuit while it's open, even hidden. .NET 10 could pause a circuit, but only when your own script asked.

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

It pauses mid-edit too. Spotting a half-typed field or work still in flight was up to you. {.bad}

## See it work

Count some stock, then hide the tab for a while.

This demo pauses a tab once it's been hidden for 10 seconds. The panel at the top shows this tab's circuit.

1. Type a note for the next shift, then press **+** on a few products.
2. Switch to another browser tab, wait 15 seconds, then come back.
3. The panel says this tab was paused, then resumed on a new circuit. Your counts and note are still there.
4. Now type in the note, leave the cursor in it, and switch away for 15 seconds again. Come back: same circuit, because it doesn't pause mid-edit.

## How it's built

How does a tab know to pause? A script from the AutoPause package watches it, once you turn it on.

Add the `Microsoft.AspNetCore.Components.Server.AutoPause` package. Then turn it on for one page with `<ConfigureBrowser>`, as here, or for every page in `Program.cs`.

### The page turns auto-pause on
code: Components/Guide/Demos/CircuitPause/StockTake.razor

### The sheet marks what to keep across a pause
code: Components/Guide/Demos/CircuitPause/StockSheet.razor

## Good to know

- **For every page.** Add `.WithBrowserOptions(o => o.AddAutoPause())` to `MapRazorComponents` in `Program.cs`. The default wait is 2 minutes.
- **It waits for the user.** It won't pause while the focused text box has unsaved typing, sound is playing, Picture-in-Picture or a Web Lock is active, or a JavaScript interop call is running.
- **Only `[PersistentState]` survives.** A resumed tab gets a new circuit, so components start again. The saved state waits in the browser, encrypted.
- **RC2: don't mix the two.** If `Program.cs` calls `WithBrowserOptions`, a page's `<ConfigureBrowser>` writes into the app-wide options, so one visit turns auto-pause on for everyone. This app mixes them, so `Program.cs` takes the shared options off this page.
