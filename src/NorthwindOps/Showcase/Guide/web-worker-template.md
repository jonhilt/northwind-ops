---
id: web-worker-template
title: .NET in a Web Worker
group: WebAssembly
order: 63
hook: A heavy report that doesn't freeze the page.
intro: Run CPU-heavy C# in a Web Worker, so a WebAssembly page keeps responding while it works. New in .NET 11. What's new is a project template, blazorwebworker, that writes the worker plumbing into your project as source you own. No framework API changed, and doing it by hand was already possible.
mode: Interactive WebAssembly
demo: demo/web-worker/order-lines
before: demo/web-worker/order-lines/net10
labels: .NET 10: on the UI thread | .NET 11 template: in a Web Worker
app:
---

## The problem

Your WebAssembly page totals a big CSV export in the browser. How do you keep the page responsive while it works?

Blazor WebAssembly runs your C# on the page's one UI thread, in .NET 11 too. In .NET 10 there was no ready-made way off it, so apps used one of three workarounds.

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

It works, but every heavy loop needs its own pauses, and each one adds to the time. {.bad}

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

That's one of five files Microsoft's guide had you write, with request ids, errors and timeouts all up to you. {.bad}

## See it work

Both pages total the same order lines with the same C# method. A clock on each ticks ten times a second on the UI thread. Which clock stops?

On the left, the report runs on the UI thread, the .NET 10 way without a worker. On the right, it runs in a Web Worker from the .NET 11 template. Each run adds a line: how long it took, and how often the clock ticked out of the ~10 a second it should have.

1. When both sides say **Ready**, press **Total on the UI thread** on the left. The clock freezes until the top five products appear, and the run line reads "ticked **0** of ~N times". The right-hand clock stops too: both demos are frames in one browser tab, and share its one UI thread.
2. Still on the left, press **Total, pausing every 1,000 lines**. The clock keeps going, every tick counted, but the run takes longer than the first.
3. On the right, press **Total in a Web Worker**. The same top five arrive, every tick is counted, and the run takes about as long as the first one: the worker isn't faster, the work has just moved.
4. On the right, press **Call a misspelled method**. The status shows "Method not found" for `TotalOrderLine`: the worker's error comes back to that call as an exception, at run time.

Explain: Nothing made the code faster: the worker runs the same method at the same speed, on another thread, so the page's thread is free to draw. That was possible in .NET 10 if you wrote the worker yourself. What `dotnet new blazorwebworker` adds is that code, written for you: a C# `WebWorkerClient` and two scripts that boot .NET in the worker, number each call, send errors back as exceptions (step 4) and time calls out after 60 seconds.

## How it's built

What do you write? A static `[JSExport]` method, and one call that names it.

`dotnet new blazorwebworker -n NorthwindOps.Worker` makes a library with four files you own and can change: `WorkerMethods.cs` for your methods, `WebWorkerClient.cs` (69 lines of C#), and two scripts (157 lines). The scripts are the third .NET 10 workaround done for you: they start the worker and its .NET runtime, match each reply to its request id, reject a call when the worker reports an error, and time it out. The WebAssembly client references the library.

### .NET 10: the same method on the UI thread, or with pauses
code: Client/WebWorker/OrderLineReportNet10.razor

### .NET 11: start the template's worker, then call a method by name
code: Client/WebWorker/OrderLineReport.razor

### The worker runs a static [JSExport] method
code: Worker/WorkerMethods.cs

## Good to know

- **Not faster, just elsewhere.** The worker runs the same code at the same speed. Copying the data across even adds a little.
- **WebAssembly only.** The worker boots the WebAssembly app's runtime, so reference the library from the client project. It does nothing for Interactive Server.
- **Found by name, at run time.** The compiler doesn't check the string, so a typo or a rename only fails when called, as step 4 shows. Keep the name in a constant beside the method. Only simple values cross over: strings, numbers and byte arrays, so send JSON for the rest.
- **A timeout stops the wait, not the work.** After 60 seconds (or the `timeoutMs` you pass), `InvokeAsync` throws "timed out". The worker can't be interrupted, so it finishes the method anyway, and the next call waits behind it. Only disposing of the worker stops it. Each worker is a whole second .NET runtime, so start one, keep it, and dispose of it when the component goes.
