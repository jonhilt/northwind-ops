---
id: web-worker-template
title: Run .NET in a Web Worker (the blazorwebworker template)
badge: dotnet new blazorwebworker · [JSExport]
release: Preview 2 (as webworker); renamed blazorwebworker in Preview 4
mode: Interactive WebAssembly
sources: Client/WebWorker/OrderLineReport.razor, Worker/WorkerMethods.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/blazor-with-dotnet-on-web-workers?view=aspnetcore-11.0
summary: A new project template, dotnet new blazorwebworker, scaffolds a WebWorkerClient class plus the JavaScript glue that starts a second .NET runtime in a Web Worker. Static [JSExport] methods run there, off the browser's UI thread, so a WebAssembly page keeps responding while they crunch.
---

## Try it

1. Open the order-lines demo at `/demo/web-worker/order-lines`, or step through it in the Field Guide. It downloads a year of made-up order lines as a CSV export (a few megabytes) and starts a Web Worker. The status says **Ready** when both are done.
2. Press **Total on the UI thread**. The clock above the buttons, which a .NET timer updates ten times a second, stops for a couple of seconds. Nothing on the page responds until the top five products appear.
3. Press **Total in a Web Worker**. The same method totals the same bytes, in about the same time, but the clock keeps ticking throughout.

## The .NET 10 way

Blazor WebAssembly ran all your C# on the page's single UI thread (WebAssembly multithreading was never supported for Blazor). Anything CPU-heavy, like parsing an import or aggregating a report, froze the page until it finished. The usual workarounds were to break the loop up with `await Task.Delay(1)` every so often, which still stutters, or to post the data to a server API.

Running .NET in a Web Worker was possible, but you built it from Microsoft's long guide: a worker script that imports `_framework/dotnet.js` and boots the runtime, a client script that creates the worker and matches replies to requests, a `[JSImport]` class to call that client from C#, and two stub `.razor` files so the build served the scripts:

```js
// Workers/Report.razor.js
import { dotnet } from '../_framework/dotnet.js';

const { getAssemblyExports, getConfig } = await dotnet.create();
const exports = await getAssemblyExports(getConfig().mainAssemblyName);

self.addEventListener('message', e => {
    const result = exports.Reports.Total(e.data.csv);
    self.postMessage({ requestId: e.data.requestId, result });
});
```

Errors, timeouts and start-up failures were yours to handle too. In a published Blazor Web App, `dotnet.js` is fingerprinted and a worker doesn't see the page's import map, so a hard-coded import path could break. The alternative was the community BlazorWorker library.

## How it works

```bash
cd src
dotnet new blazorwebworker -n NorthwindOps.Worker
dotnet add NorthwindOps.Client/NorthwindOps.Client.csproj \
    reference NorthwindOps.Worker/NorthwindOps.Worker.csproj
```

The template makes a Razor class library (`Microsoft.NET.Sdk.Razor`, with `AllowUnsafeBlocks` for the `[JSExport]` source generator) holding four files:

- **`WorkerMethods.cs`**: a `static partial class` marked `[SupportedOSPlatform("browser")]`, where you add `static` methods marked `[JSExport]`. The demo's `TotalOrderLines(byte[] csv)` parses the CSV and returns the top five products as JSON.
- **`WebWorkerClient.cs`**: the C# side. `CreateAsync(IJSRuntime, timeoutMs = 60000, assemblyName = null, CancellationToken)` starts the worker and waits until .NET is running in it. `InvokeAsync<TResult>(method, object[] args, timeoutMs = 60000, CancellationToken)` and `InvokeVoidAsync` call a method by its full name as a string, like `"NorthwindOps.Worker.WorkerMethods.TotalOrderLines"`. `DisposeAsync` terminates the worker.
- **`wwwroot/dotnet-web-worker-client.js`**: creates the module worker, posts each call with a request id, resolves the matching reply and applies the timeouts. It resolves `_framework/dotnet.js` through the page's import map on the main thread and passes the URL in, so fingerprinted, published apps work.
- **`wwwroot/dotnet-web-worker.js`**: the worker itself. It imports `dotnet.js`, calls `dotnet.create()` and `getAssemblyExports()` for the app's main assembly and the worker library, then looks each requested method up by walking the dotted name.

It's source in your solution, not a framework API, so you can read and change all of it.

Things to know:

- **Reference it from the WebAssembly project.** The worker boots the WebAssembly app's own runtime and assemblies, so the library must be part of that app. Here `NorthwindOps.Client` references it. A Blazor Web App's server project alone isn't enough.
- **Not faster, just off the UI thread.** The worker runs the same interpreted code at the same speed. Copying the arguments and result across adds a little, which is why the demo's worker run is usually a touch slower.
- **Create one worker and keep it.** Each worker starts a whole second .NET runtime, which takes a moment (the files come from the browser cache after the first time). The demo creates it once, after its first render, and disposes of it in `DisposeAsync`.
- **Only simple values cross over.** Arguments go through Blazor's JS interop, then `postMessage`, then `[JSExport]` marshalling. Strings, numbers, bools and `byte[]` work. Serialize anything richer to JSON, as `TotalOrderLines` does with its result.
- **Methods are found by string.** A rename or typo fails at run time with "Method not found", not at compile time. `nameof` can't build the full name for you, so keep the string next to the method or in a constant.
- **No DI, no DOM, no components.** `dotnet.create()` loads your assemblies but never runs `Program.cs`. Worker methods are plain static methods: give them everything they need as arguments.
- **A timeout stops the wait, not the work.** When `timeoutMs` passes or the token is cancelled, `InvokeAsync` throws, but the worker carries on with the method until it finishes.
- **Calling the method directly works too.** `TotalOrderLines` is an ordinary static method, so the demo's UI-thread button calls it with no worker at all. That's what makes the comparison fair.
