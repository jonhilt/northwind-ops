---
id: server-initiated-circuit-pause
title: Pause a circuit from the server (Circuit.RequestCircuitPauseAsync)
badge: RequestCircuitPauseAsync · drain before a deploy
release: Preview 7
mode: Interactive Server
sources: Components/Guide/Demos/CircuitPause/CircuitTracker.cs, Components/Guide/Demos/CircuitPause/OrderDraft.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#server-triggered-circuit-pause
summary: Server code can now ask a browser to pause its circuit. The browser saves the [PersistentState] values and lets go of the circuit, just as if it had paused itself, and resumes later on a new circuit, on this server or the next one. Use it to drain a server before a restart without losing anyone's half-filled form.
---

## Try it

1. Open the new order demo at `/demo/server-pause/new-order`, or step through it in the Field Guide.
2. Pick a customer and a product, and change the quantity.
3. Press **Pause this tab from the server**. Server code calls `RequestCircuitPauseAsync` on this tab's circuit. The browser saves the draft and closes the circuit, and the reconnect dialog says the session has been paused by the server. Other visitors' circuits aren't touched.
4. Press **Resume**. The panel shows a new circuit id, and the order is still filled in.

## The .NET 10 way

.NET 10 could pause and resume a circuit, but only the browser could start it, with `Blazor.pauseCircuit()`. The server had no way to ask. So a restart simply dropped every circuit: each tab showed "Rejoining the server…", failed, and reloaded, and whatever was on screen was lost.

Apps that cared saved drafts as they went:

```razor
@inject ProtectedLocalStorage Storage

<InputNumber @bind-Value="Draft.Quantity" @bind-Value:after="SaveAsync" />

@code {
    Task SaveAsync() => Storage.SetAsync("draft", Draft).AsTask();
    // ...and read it back in OnAfterRenderAsync after the reload
}
```

That's a write to the browser on every change, wired by hand into every form, and the page still reloads under the user. The other option was to wait for circuits to close before restarting, but tabs left open all day never close.

## How it works

`Circuit.RequestCircuitPauseAsync(CancellationToken)` returns a `Task<bool>`. It sends the browser a `JS.RequestPause` message and returns `true` once that's sent. It returns `false` without asking if the circuit has been disposed, hasn't finished starting, or its connection is down.

In the browser, Blazor first runs the `onCircuitPausing` callbacks of any JavaScript circuit handlers you passed to `Blazor.start()`, so a page can finish something before it goes. Then it pauses exactly as `Blazor.pauseCircuit()` would: the server collects every `[PersistentState]` value, sends them to the browser encrypted with Data Protection, and disposes the circuit. The reconnect dialog switches to its paused state, with a **Resume** button that calls `Blazor.resumeCircuit()`. That starts a new circuit from the saved state.

There's no public list of circuits, so you keep your own. A `CircuitHandler` registered as scoped is created once per circuit, and `OnCircuitOpenedAsync` hands it the `Circuit` before any component renders. `CircuitTracker` keeps it, and registers itself both as itself and as a `CircuitHandler`, so a component that injects `CircuitTracker` gets the tracker of its own circuit, never another visitor's.

Gotchas:

- **`true` isn't "paused".** It only means the request went out. The browser may still be running `onCircuitPausing` callbacks, or the connection may drop first.
- **Draining a server.** Keep the open circuits in a singleton (add in `OnCircuitOpenedAsync`, remove in `OnCircuitClosedAsync`), and when `IHostApplicationLifetime.ApplicationStopping` fires, call `RequestCircuitPauseAsync` on each one and wait a few seconds for the browsers to save before the host shuts down.
- **The state lives in the browser.** It's encrypted with Data Protection, so the server that resumes it needs the same keys. Persist your key ring (to a shared folder, Redis, Azure Blob Storage and so on) if a restart or another server should be able to read it.
- **Only persisted state survives.** Fields that aren't `[PersistentState]` start again, and `OnInitializedAsync` runs again on the new circuit.
- **The demo pauses only your tab.** Its button calls `RequestCircuitPauseAsync` on the circuit of the tab that pressed it. The panel's "this server" line counts every circuit on the server, as a drain would see them.
