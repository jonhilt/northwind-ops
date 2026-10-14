---
id: wasm-culture-from-server
title: Prerendered WebAssembly keeps the server's culture
badge: Culture · server to browser
release: Preview 5 (configuration key since Preview 7)
mode: Interactive WebAssembly
sources: Components/Guide/Demos/WasmCulture/Invoice.razor, Client/WasmCulture/InvoiceSummary.razor, Program.cs
docs: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md
summary: When the server prerenders a WebAssembly component, it now saves its culture with the page's state, and WebAssembly applies it as it starts. Numbers and dates keep the format the server rendered them in, instead of switching to the browser's language when the component becomes interactive.
---

## Try it

1. Open the invoice demo at `/demo/wasm-culture/invoice?culture=de-DE`, or step through it in the Field Guide. The page is static SSR, and request localization sets its culture from `?culture=`. The invoice is a WebAssembly component, prerendered on the server.
2. Watch the badge: **Prerendered on the server**, then **Running on WebAssembly** (on a quick connection the first is gone in a blink). The amounts (with decimal commas, like "12,75") and the long date ("Freitag, 13. November 2026") stay German throughout.
3. The component says **Formatting in de-DE** while running in your browser, whatever your browser's language is.
4. Pick **fr-FR** or **en-GB**. The links skip enhanced navigation, so the page loads in full and WebAssembly starts in the new culture.

## The .NET 10 way

WebAssembly chose its own culture as it started: the `applicationCulture` start option if you set one, otherwise the browser's first language (`navigator.languages[0]`). The server's request culture never reached it. So a page prerendered in German would flip to English formatting the moment WebAssembly took over, in an English browser.

To keep the two in step you either turned off `autostart` in `App.razor` and passed `Blazor.start({ webAssembly: { applicationCulture: "@CultureInfo.CurrentCulture.Name" } })`, or stored the culture in a cookie or local storage and set `CultureInfo.DefaultThreadCurrentCulture` yourself in the client's `Program.cs`, before `RunAsync`, with a JavaScript call to read it. Or you passed the culture name to each component as a parameter and handed it to every `ToString` call.

## How it works

`AddInteractiveWebAssemblyComponents()` registers a scoped `CultureStateProvider` on the server, with two `[PersistentState]` properties: the current culture's name and the current UI culture's name. They're saved with the page's other persisted component state for WebAssembly, so the names travel in the page. The client registers the same provider. In `WebAssemblyHost.RunAsync`, after restoring the persisted state and before starting hosted services or rendering anything, the host sets `CultureInfo.CurrentCulture`, `CurrentUICulture` and their `DefaultThread...` counterparts to the saved names, if they differ, and then loads that culture's satellite assemblies.

It's controlled by the `Components:UseCultureFromServer` configuration value: `true` or `1` turns it on, `false` or `0` turns it off. When it's unset, it's on only if an `IStringLocalizerFactory` is registered, which `AddLocalization()` does. This app calls `AddLocalization()` for its Suppliers screen, so it would be on anyway. `Program.cs` sets the key to `true` as well, to say so outright.

The demo's components do nothing special. `InvoiceSummary` formats with `ToString("N2")` and `ToString("D")`, using the current culture, so it's the same code on the server and in the browser. The amounts are formatted as numbers, not with `"C"`, because the invoice is in dollars whatever the reader's culture.

Gotchas:

- **Set once, at start.** The culture is applied when the runtime starts. After an enhanced navigation to a page in another culture, WebAssembly keeps the one it started with, so culture switchers should load the page in full.
- **Globalization data.** By default WebAssembly loads ICU data for the browser's language group only (English, French, Italian, German and Spanish come together). The culture's name is applied, but in, say, a Japanese browser, de-DE then formats without German data. Set `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` in the client project to load data for every culture.
- **The API changed.** Preview 5 had a `WebAssemblyComponentsOptions.UseCultureFromServer` property. It was removed in favour of the configuration key, so older posts show code that no longer compiles.
- **`ApplicationCulture` still exists.** `BrowserOptions.InteractiveWebAssembly.ApplicationCulture` sets a fixed start-up culture from C#, with no prerendering needed.
