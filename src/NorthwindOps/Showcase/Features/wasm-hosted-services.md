---
id: wasm-hosted-services
title: Hosted services in Blazor WebAssembly
badge: IHostedService · in the browser
release: Preview 1
mode: Interactive WebAssembly
sources: Client/Program.cs, Client/HostedServices/OrderWatcher.cs, Client/HostedServices/OrderBell.razor
docs: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md
summary: The WebAssembly host now starts the IHostedService and BackgroundService implementations you register with AddHostedService, just as the server does. Background work, like polling for updates, can live in a service that keeps running while the visitor moves between pages, instead of in a component.
---

## Try it

1. Open the orders demo at `/demo/hosted-services/orders`, or step through it in the Field Guide. The page is static SSR. Only the bell at the top runs in the browser, with `@rendermode` `InteractiveWebAssembly` and no prerendering.
2. Wait a few seconds. The bell's time updates every 3 seconds, and its count goes up as the warehouse's simulated feed places, packs and ships orders.
3. Click **Customers**. It's another static page with its own bell. Enhanced navigation keeps WebAssembly running, so the new bell picks up the same count: the watcher behind it never stopped.
4. Reload the page. WebAssembly starts again, and so does the watcher, from 0.

## The .NET 10 way

`AddHostedService` compiled in a WebAssembly app, if you referenced `Microsoft.Extensions.Hosting.Abstractions`, but nothing ever called `StartAsync`. The registration was silently ignored.

So polling usually lived in a component, with a timer started in `OnInitializedAsync` and stopped in `Dispose`. It worked for one page, but it stopped as soon as the visitor left the page, and every page that wanted the bell ran its own copy. To keep one copy running, you registered a singleton and started it yourself in `Program.cs`:

```csharp
builder.Services.AddSingleton<OrderWatcher>();
var host = builder.Build();
_ = host.Services.GetRequiredService<OrderWatcher>().StartAsync(CancellationToken.None);
await host.RunAsync();
```

That's start-up code every app had to remember. Libraries that rely on hosted services, like some telemetry exporters, didn't start either.

## How it works

`WebAssemblyHostBuilder` registers an internal `HostedServiceExecutor`. When `WebAssemblyHost.RunAsync` runs, it:

1. restores the persisted component state from prerendering,
2. applies the culture (see the culture-from-server entry) and loads its satellite assemblies,
3. awaits `StartAsync` on every registered `IHostedService`, in registration order,
4. and only then creates the renderer and adds the root components.

So a slow `StartAsync` delays the first render. `BackgroundService.StartAsync` returns as soon as `ExecuteAsync` reaches its first incomplete `await`, which makes it the easy choice. When the host is disposed, it calls `StopAsync` on each service and logs any failures. In a browser that rarely happens: the app ends when the page unloads, so don't rely on `StopAsync` to save anything.

The demo registers the watcher twice: once as a singleton, so components can inject it, and once as a hosted service that resolves that same instance. `OrderWatcher` is a `BackgroundService` with a `PeriodicTimer`. Each tick it calls `demo/hosted-services/api/updates?after={id}` on the server and raises `Changed`. The bell subscribes in `OnInitialized`, calls `InvokeAsync(StateHasChanged)` when it fires, and unsubscribes in `Dispose`. The watcher skips the call when no bell is listening, because every WebAssembly demo in this app shares the one client, and the others don't need it.

Gotchas:

- **One thread.** WebAssembly runs .NET on the browser's main thread. A hosted service suits async work that waits most of the time, like polling. CPU-heavy work blocks the UI; look at the Web Worker template for that.
- **One per tab.** Each tab, and each full page load, has its own runtime and its own copy of every hosted service. Three tabs poll three times.
- **It starts with WebAssembly.** In a Blazor Web App, the runtime starts when the first WebAssembly (or Auto) component appears, and stays up across enhanced navigation. A visitor who never meets one never runs your hosted services.
- **Exceptions.** An exception that escapes `ExecuteAsync` ends that service. Catch per iteration, as the demo does, if one failed call shouldn't stop it.
