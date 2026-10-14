---
id: wasm-culture-from-server
title: The same culture in the browser
group: WebAssembly
order: 62
hook: German prices stay German when WebAssembly takes over.
intro: Keep a WebAssembly component in the culture the server rendered it in, so numbers and dates don't change format as it starts.
mode: Interactive WebAssembly
demo: demo/wasm-culture/invoice?culture=de-DE
app:
---

## The problem

A German customer opens an invoice. The server renders "1.234,56", then WebAssembly takes over. How do you stop it switching to "1,234.56"?

WebAssembly in .NET 10 took its culture from the browser's language, not the server's, so apps used one of two workarounds.

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

Look at the invoice, then pick another culture.

The invoice is rendered on the server in German, from `?culture=de-DE`, then runs in your browser on WebAssembly.

1. The badge says **Running on WebAssembly**: the server rendered the invoice first, then WebAssembly took over. The amounts and the date stayed German.
2. That's not your browser's language at work: it still says **Formatting in de-DE**, even if your browser is set to English.
3. Pick **fr-FR** or **en-GB**. The page loads again, and WebAssembly starts in that culture instead.

## How it's built

Where does the browser get the culture from?

As the server prerenders, it saves its culture with the page's state. WebAssembly applies it as it starts, before any component renders.

It's on whenever `AddLocalization()` is registered. The page sets its culture with request localization, as any localized app does.

### The page renders the invoice in the request's culture
code: Components/Guide/Demos/WasmCulture/Invoice.razor

### The invoice formats with no culture in sight
code: Client/WasmCulture/InvoiceSummary.razor

## Good to know

- **Turn it on or off.** Set `Components:UseCultureFromServer` to `true` or `false` in configuration. Unset, it's on if `AddLocalization()` was called.
- **Set once, at start.** Changing culture needs a full page load, which is why the demo's culture links skip enhanced navigation.
- **Globalization data.** WebAssembly loads data for the browser's own language group. For a culture outside it, like German in a Japanese browser, set `BlazorWebAssemblyLoadAllGlobalizationData` to `true` in the client project.
