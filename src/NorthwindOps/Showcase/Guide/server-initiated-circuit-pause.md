---
id: server-initiated-circuit-pause
title: Pause circuits from the server
group: Interactive Server
order: 51
hook: Drain a server before a deploy, and keep everyone's half-filled forms.
intro: Ask a browser to save its state and let go of its circuit, from server code.
mode: Interactive Server
demo: demo/server-pause/new-order
---

## The problem

You need to restart a server to deploy. How do you stop everyone on it losing the order they're halfway through?

In .NET 10, only the browser could pause a circuit. A restart dropped every one, so apps used one of two workarounds.

### Save every draft in the browser

```razor
@inject ProtectedLocalStorage Storage

<InputNumber @bind-Value="Draft.Quantity"
             @bind-Value:after="SaveAsync" />
@code {
    Task SaveAsync() =>
        Storage.SetAsync("draft", Draft).AsTask();
    // ...and read it back after the reload
}
```

Every form wired by hand, a write on every change, and the restart still reloads the page under them. {.bad}

### Wait for everyone to leave

```csharp
// Before a deploy: wait for circuits to close
while (circuits.Count > 0)
{
    await Task.Delay(TimeSpan.FromMinutes(1));
}
```

Tabs left open all day never close, so the deploy waits, or gives up and drops them. {.bad}

## See it work

Fill in an order, then have the server pause this tab.

In a deploy, the server would ask every tab. Here, the button asks only yours, so other visitors carry on.

1. Pick a customer and a product, and change the quantity.
2. Press **Pause this tab from the server**. The browser saves the draft and lets go of its circuit. The dialog says the session has been paused by the server.
3. Press **Resume**. The panel shows a new circuit, and the order is still filled in.

## How it's built

How does server code reach a tab? Through its `Circuit`, which a circuit handler can keep.

`RequestCircuitPauseAsync` asks the browser to pause. The browser then pauses just as if it had decided to itself: it saves the `[PersistentState]` values and closes the circuit.

### A circuit handler keeps each tab's circuit
code: Components/Guide/Demos/CircuitPause/CircuitTracker.cs

It's registered scoped in `Program.cs`, as itself and as a `CircuitHandler`, so each circuit gets its own.

### The draft asks for a pause, and marks what to keep
code: Components/Guide/Demos/CircuitPause/OrderDraft.razor

## Good to know

- **Draining a whole server?** Keep every open `Circuit` in a singleton, and ask each one when `IHostApplicationLifetime.ApplicationStopping` fires.
- **`false` means it didn't ask.** The circuit had closed, wasn't connected yet, or its browser was offline. `true` means the request went out, not that the pause has finished.
- **Restarting? Keep your Data Protection keys.** The paused state waits in the browser, encrypted. The new server can only read it with the same keys.
- **The browser can hold it back.** A JavaScript circuit handler's `onCircuitPausing` callback runs first, so a page can finish a save before it pauses.
