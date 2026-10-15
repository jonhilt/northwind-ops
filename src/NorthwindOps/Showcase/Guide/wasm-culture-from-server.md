---
id: wasm-culture-from-server
title: The same culture in the browser
group: WebAssembly
order: 62
hook: German prices stay German when WebAssembly takes over.
intro: Keep a WebAssembly component in the culture the server rendered it in, so numbers and dates don't change format as it starts. New in .NET 11.
mode: Interactive WebAssembly
demo: demo/wasm-culture/invoice?culture=de-DE
before: demo/wasm-culture/invoice/net10
labels: .NET 10 · recorded | .NET 11 · live
app:
---

## The problem

A German customer opens an invoice. The server renders "1.234,56", then WebAssembly takes over. How do you stop it switching to "1,234.56"?

WebAssembly in .NET 10 took its culture from the browser's language, not the server's, so the page switched format the moment WebAssembly started. Apps used one of two workarounds.

### Tell Blazor.start the culture

```razor
<script src="_framework/blazor.web.js"
        autostart="false"></script>
<script>
  Blazor.start({ webAssembly: {
    applicationCulture:
      "@CultureInfo.CurrentCulture.Name"
  } });
</script>
```

A hand-written start-up script in App.razor, just to repeat what the server already knew. {.bad}

### Pass the culture to each component

```razor
<td>@line.Amount.ToString("N2", culture)</td>

@code {
    // The server page passes its culture's name
    [Parameter] public string? Culture { get; set; }

    CultureInfo culture =>
        CultureInfo.GetCultureInfo(Culture ?? "");
}
```

Every format call needs it. Miss one, and that number switches when WebAssembly starts. {.bad}

## See it work

Both invoices are rendered by the server in German, from `?culture=de-DE`, then run in an English browser on WebAssembly. Do their numbers and dates stay German?

.NET 11 changed how WebAssembly starts, so the old behaviour can't run here. The left is two moments recorded from our .NET 10 repro. The right is live, and keeps what the server rendered so you can compare.

1. On the left, the server sent **1.234,56** and **Freitag**. Once WebAssembly started, the same component showed **1,234.56** and **Friday**: the browser's language won.
2. The third row is workaround 2, the culture passed as a parameter. The total stayed German, but one forgotten format call, the date, still flipped.
3. On the right, wait for **Running on WebAssembly**. **Server rendered** and **WebAssembly now** match: de-DE both times, whatever your browser's language.

Explain: As the server prerenders, it saves its culture in the page's persisted state, and WebAssembly applies it as it starts, before any component renders. So plain `ToString("N2")` gives the same answer on both sides.

## How it's built

What turns it on? Registering localization, which a localized app already does.

In most apps you write nothing: the server project's `Program.cs` already calls `AddLocalization()` and `UseRequestLocalization()`, and that turns it on. `Components:UseCultureFromServer` is the explicit switch, shown in the first card; you only need it to turn the feature off, or on without localization. The component only gets the culture if it's prerendered.

### The switch, called from Program.cs
code: Components/Guide/Demos/WasmCulture/WasmCultureSetup.cs

### The page renders the invoice in the request's culture
code: Components/Guide/Demos/WasmCulture/Invoice.razor

### What you no longer write: plain formatting, no culture passed in
code: Client/WasmCulture/InvoiceSummary.razor

## Good to know

- **Prerendering carries it.** With `prerender: false` there's no server render to take the culture from, so WebAssembly falls back to the browser's language.
- **Set once, at start.** Changing culture needs a full page load, which is why the demo's culture links skip enhanced navigation.
- **Globalization data.** WebAssembly loads data for the browser's own language group. For a culture outside it, like German in a Japanese browser, set `BlazorWebAssemblyLoadAllGlobalizationData` to `true` in the client project.
- **Turn it off.** Set `Components:UseCultureFromServer` to `false`, and WebAssembly uses the browser's language again.
