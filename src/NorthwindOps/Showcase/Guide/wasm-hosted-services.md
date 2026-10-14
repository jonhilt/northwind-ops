---
id: wasm-hosted-services
title: Background services in the browser
group: WebAssembly
order: 60
hook: A watcher that keeps checking for updates as you change pages.
intro: Run a hosted service in WebAssembly, like a watcher that checks for order updates while you work.
mode: Interactive WebAssembly
demo: demo/hosted-services/orders
app:
---

## The problem

Every page shows a bell with the latest order updates. Where does the checking live, so it carries on as you move between pages?

Blazor WebAssembly in .NET 10 ignored `AddHostedService`, so apps used one of two workarounds.

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

The checking belongs to the page. Leave it and it stops; come back and it starts again from nothing. {.bad}

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

Start-up code to remember by hand, while AddHostedService compiled fine and silently did nothing. {.bad}

## See it work

Watch the bell, then change pages.

This is a small orders screen. The page is static, and the bell runs in your browser on WebAssembly.

1. Wait a few seconds. The bell's count goes up as orders are placed, packed and shipped, and its time shows the last check.
2. Click **Customers**. The bell there carries on from the same count: the watcher kept running between pages.
3. Press **⟳** above the demo. The page loads afresh, so WebAssembly and its watcher start again from 0.

## How it's built

What starts the watcher, and how does the bell hear from it?

Register a `BackgroundService` with `AddHostedService`. The WebAssembly host starts it, and the bell just listens to it.

### Register it once, as a hosted service
code: Client/Program.cs

### The watcher checks every 3 seconds
code: Client/HostedServices/OrderWatcher.cs

### The bell shows what the watcher found
code: Client/HostedServices/OrderBell.razor

## Good to know

- **Starts before the first render.** The host awaits each service's `StartAsync` before it renders any component, so keep that quick. A `BackgroundService` returns at its first `await`.
- **One thread.** WebAssembly runs on the browser's single thread. Use hosted services for async work like polling, not long calculations.
- **Lives as long as the page.** Enhanced navigation keeps it running. A reload or a new tab starts a fresh copy, and closing the tab just ends it, so don't count on `StopAsync`.
- **Only once WebAssembly starts.** In a Blazor Web App that's when the first WebAssembly component appears. A visitor who never meets one never runs it.
