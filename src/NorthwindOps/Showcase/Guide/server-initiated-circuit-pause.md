---
id: server-initiated-circuit-pause
title: Pause circuits from the server
group: Interactive Server
order: 51
hook: Drain a server before a deploy, and keep everyone's half-filled forms.
intro: Ask a browser to save its state and let go of its circuit, from server code, and know at once whether the request went out. New in .NET 11.
mode: Interactive Server
demo: demo/server-pause/new-order
before: demo/server-pause/new-order/net10
labels: .NET 10: JS interop | .NET 11: RequestCircuitPauseAsync
---

## The problem

You need to restart a server to deploy. How does server code ask each open tab to pause first, so nobody loses a half-filled order?

Pausing wasn't new: in .NET 10 a browser could pause its circuit and resume it later, state and all. What was missing was a server API to ask for it.

### Call the browser's pause function

```csharp
// From server code holding the tab's IJSRuntime
await js.InvokeVoidAsync("Blazor.pauseCircuit");
// The circuit closes before the browser
// can reply, so this never returns
```

It pauses the tab, but the await never comes back: shutdown code stops there, or fires and forgets and never knows. {.bad}

### Restart anyway

```csharp
// No pause: the deploy drops every circuit.
// Each tab shows "Rejoin failed", reloads,
// and starts again with an empty form.
```

Simple, and everyone loses whatever they'd typed. {.bad}

## See it work

Both tabs get asked to pause by server code. When each one comes back, what will the server have heard?

On the left, server code calls the browser's pause function through JS interop, the .NET 10 way. On the right, it calls `RequestCircuitPauseAsync`.

1. In each tab, change the **Quantity**, press **Pause this tab from the server**, then **Resume**. Both come back on a new circuit with the order still filled in: pausing itself worked in .NET 10 too.
2. On the left, **The server's request** is still waiting for an answer. The awaited JS call never returned, so the code after it never ran.
3. On the right, it answered `true` within a few milliseconds: the request went out, so server code can move on to the next tab. It isn't a promise the tab has finished pausing.

Explain: A pause closes the circuit, and with it the connection a JS call's reply would come back on. `RequestCircuitPauseAsync` doesn't wait for the browser: it sends the request and says at once whether it could. So a drain asks every tab, then gives the browsers a few seconds to save before the process exits.

## How it's built

What does .NET 11 add? A way to ask from the server that answers.

A circuit handler keeps each tab's `Circuit`, and `RequestCircuitPauseAsync` asks that tab to pause. The browser then pauses as if it had decided to itself: it saves the `[PersistentState]` values and closes the circuit.

### The draft: both ways of asking, and what to keep
code: Components/Guide/Demos/CircuitPause/OrderDraft.razor

### A circuit handler keeps each tab's circuit
code: Components/Guide/Demos/CircuitPause/CircuitTracker.cs

It's registered scoped in `Program.cs`, as itself and as a `CircuitHandler`, so each circuit gets its own.

## Good to know

- **Draining a whole server?** Keep every open `Circuit` in a singleton, ask each one when `IHostApplicationLifetime.ApplicationStopping` fires, and wait a few seconds (with a timeout) before returning, or the process may exit before the browsers have saved.
- **`false` means it didn't ask.** The circuit had closed, wasn't connected yet, or its browser was offline. `true` means the request went out, not that the pause has finished.
- **Restarting? Keep your Data Protection keys.** The paused state waits in the browser, encrypted. The new server can only read it with the same keys.
- **The browser can hold it back.** A JavaScript circuit handler's `onCircuitPausing` callback runs first, so a page can finish a save before it pauses.
