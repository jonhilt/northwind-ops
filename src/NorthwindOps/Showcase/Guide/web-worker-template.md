---
id: web-worker-template
title: .NET in a Web Worker
group: WebAssembly
order: 63
hook: A heavy report that doesn't freeze the page.
intro: Run CPU-heavy C# in a Web Worker, so a WebAssembly page keeps responding while it works.
mode: Interactive WebAssembly
demo: demo/web-worker/order-lines
app:
---

## The problem

Your WebAssembly page totals a big CSV export in the browser. How do you keep the page responsive while it works?

Blazor WebAssembly in .NET 10 ran your C# on the page's one UI thread, so apps used one of three workarounds.

### Pause every so often

```csharp
var lines = 0;
foreach (var line in text.Split('\n'))
{
    totals.Add(line);
    // Every 1,000 lines, let the page draw
    if (++lines % 1000 == 0)
        await Task.Delay(1);
}
```

Every heavy loop needs these pauses, and the page still stutters between them. {.bad}

### Send it to the server

```csharp
// Post the export, and let the server total it
using var content = new ByteArrayContent(csv);
var response = await Http.PostAsync(
    "api/reports/order-lines", content);
var totals = await response.Content
    .ReadFromJsonAsync<OrderLineTotals>();
```

Another API to build and secure, and the report stops working whenever the server can't be reached. {.bad}

### Write the worker yourself

```js
// Workers/Report.razor.js: boot .NET by hand
import { dotnet } from '../_framework/dotnet.js';
const { getAssemblyExports, getConfig } =
    await dotnet.create();
const exports = await getAssemblyExports(
    getConfig().mainAssemblyName);
self.onmessage = e => self.postMessage(
    exports.Reports.Total(e.data));
```

That's one of four files Microsoft's guide had you write, with request ids, errors and timeouts all up to you. {.bad}

## See it work

Total the same export twice, and watch the clock.

The report runs on WebAssembly, in your browser. The clock ticks on the page's UI thread, ten times a second.

1. Wait for **Ready**. The order lines have downloaded, and a Web Worker has started its own .NET runtime.
2. Press **Total on the UI thread**. The clock stops for a couple of seconds, until the totals appear.
3. Press **Total in a Web Worker**. The same totals arrive in about the same time, and the clock keeps ticking.

## How it's built

`dotnet new blazorwebworker` makes a library with a `WebWorkerClient` class and the JavaScript glue. You write static `[JSExport]` methods, and call them by name.

The demo page is static SSR, and renders the report with `@rendermode="InteractiveWebAssembly"`. The client project references the worker library.

### The report starts a worker, then calls it by name
code: Client/WebWorker/OrderLineReport.razor

### The worker runs a static [JSExport] method
code: Worker/WorkerMethods.cs

## Good to know

- **Not faster, just elsewhere.** The worker runs the same code at the same speed. Copying the data across even adds a little.
- **Start one worker, and keep it.** Each worker starts a whole .NET runtime of its own. Create it once, and dispose of it when the component goes.
- **Simple values only.** Strings, numbers and byte arrays cross over. Serialize anything else to JSON, as the demo does.
- **No DI, no components.** The worker loads your assemblies, but not your `Program.cs`. Pass each method what it needs.
