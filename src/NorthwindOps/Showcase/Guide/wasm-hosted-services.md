---
id: wasm-hosted-services
title: Background services in the browser
group: WebAssembly
order: 60
hook: A watcher that keeps checking for updates as you change pages.
intro: Register a hosted service in a WebAssembly app and the host starts it, like a watcher that checks for order updates while you change pages. New in .NET 11.
mode: Interactive WebAssembly
demo: demo/hosted-services/orders
before: demo/hosted-services/orders/net10
labels: .NET 10: the bell polls by itself | .NET 11: AddHostedService
app:
---

## The problem

Every page shows a bell with the latest order updates. Where does the checking live, so it carries on as you move between pages?

WebAssembly in .NET 10 never started a hosted service, so apps used one of two workarounds.

### Poll from the component

```razor
@code {
    PeriodicTimer timer = new(TimeSpan.FromSeconds(3));

    protected override async Task OnInitializedAsync()
    {
        while (await timer.WaitForNextTickAsync())
            await CheckForUpdatesAsync();
    }
}
```

The checking belongs to the component. Open a page without it and it stops; come back and it starts again from nothing. {.bad}

### Start the service yourself

```csharp
builder.Services.AddSingleton<OrderWatcher>();
var host = builder.Build();

// Nothing else will start it in WebAssembly
var watcher = host.Services
    .GetRequiredService<OrderWatcher>();
_ = watcher.StartAsync(CancellationToken.None);

await host.RunAsync();
```

This works, but only for services you start yourself: `AddHostedService` compiled once you added the Hosting.Abstractions package, then silently did nothing, so hosted services that libraries such as OpenTelemetry register never started. {.bad}

## See it work

Both bells check the server for order updates every 3 seconds. The Customers page doesn't show the bell. Go there and come back: which bell remembers its count?

On the left, the bell runs its own timer, the .NET 10 way. On the right, it listens to a watcher registered with `AddHostedService`. Each bell shows how many times it has checked the server, since when, and how many new updates those checks found.

1. Wait about ten seconds on **Orders**. Both bells reach **checked the server 3×**. Checks and updates are separate counts: the first check only finds where the feed is up to, and a later check may find no update, or several.
2. Click **Customers** on both sides, wait a few seconds, then click **Orders**. The left bell starts again from **0×** (or **1×** if a tick has passed), with a new **since** time: its timer went with the Orders page. The right bell kept checking while you were away: same **since** time, a higher count, and any updates it found meanwhile.
3. Under the left bell, our .NET 10 repro runs three watchers in one app, recorded: this bell's timer, the .NET 11 `AddHostedService` line, and workaround 2. The `AddHostedService` row stays at **0** ticks, with no error. Started by hand, the same watcher kept counting: **5**, **8**, **10**.

Explain: The right-hand watcher is a service, not part of a page. The WebAssembly host starts it as the runtime starts, and the runtime keeps running as you move between pages, so the watcher does too, bell or no bell. .NET 10 could do this if you started each service yourself after `Build()`, but a hosted service registered inside a library never started. New in .NET 11, `AddHostedService` does it for you, with no extra package to add.

## How it's built

What starts the watcher, and how does the bell hear from it?

Register the watcher as a singleton, so bells can inject it, then hand that same instance to `AddHostedService`, so the host starts it. `AddHostedService<OrderWatcher>()` on its own would build a second watcher: the host would run one, and the bells would listen to the other.

### Register it once, and the host starts it
code: Client/Program.cs

### The watcher checks every 3 seconds
code: Client/HostedServices/OrderWatcher.cs

### The bell listens, and carries on wherever it's shown
code: Client/HostedServices/OrderBell.razor

### The .NET 10 bell: the timer lives in the component
code: Client/HostedServices/PollingBell.razor

## Good to know

- **Starts before the first render.** The host awaits each service's `StartAsync` before it renders any component, so keep that quick. A `BackgroundService` returns at its first `await`.
- **One thread.** WebAssembly runs .NET on the browser's only thread, the one that draws the page. A service that waits, like this poll, is fine. A long calculation freezes the page until it finishes.
- **One copy per tab, once WebAssembly starts.** It starts with the first WebAssembly component a visitor meets, then keeps running between pages. A reload or a second tab starts a fresh copy from 0. Closing the tab just ends it, without `StopAsync`, so don't save anything there.
- **A component can outlive its page.** Enhanced navigation keeps a WebAssembly component that sits in the same place on the next page, timer and all. The left bell only stops because Customers leaves it out.
