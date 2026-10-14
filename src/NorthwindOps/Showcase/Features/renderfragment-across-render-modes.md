---
id: renderfragment-across-render-modes
title: Static content inside interactive components
badge: ChildContent across render modes
release: Preview 5
mode: Interactive Server island in a static page
sources: Components/Pages/Home.razor, Components/Pages/Dashboard/CollapsibleCard.razor, Program.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/components/render-modes?view=aspnetcore-11.0
summary: A static SSR page can now pass ChildContent, or any other plain RenderFragment, to a component with an interactive render mode. Blazor captures the markup while the page prerenders and replays it inside the interactive component, so you can wrap server-rendered content in interactive cards, tabs and dialogs.
---

## Try it

1. Collapse **Top customers** with the arrow in its header. The click is handled in C# over the page's circuit.
2. Reload the page. It appears open, then collapses as soon as the circuit connects: it saved that in your browser's local storage, and it has to render open first (see below).
3. Expand it again. The table comes straight back without querying the database: the circuit re-renders the copy of the markup it was handed when it started.
4. View the page source. The table is plain HTML rendered by the static page, and just before it there's a `<!--Blazor:...-->` marker whose encrypted `descriptor` carries a copy of it for the interactive component.

## The .NET 10 way

The obvious markup threw as soon as the page rendered:

```razor
<CollapsibleCard @rendermode="InteractiveServer" Title="Top customers">
    <table>...</table>
</CollapsibleCard>
```

> InvalidOperationException: Cannot pass the parameter 'ChildContent' to component 'CollapsibleCard' with rendermode 'InteractiveServerRenderMode'. This is because the parameter is of the delegate type 'Microsoft.AspNetCore.Components.RenderFragment', which is arbitrary code and cannot be serialized.

Parameters crossing into an interactive component had to be serializable data, so a general-purpose interactive wrapper was impossible. You picked a workaround:

- **Make the whole page interactive**, so there's no boundary to cross. Now every visitor holds a circuit for the whole page, and every part of it keeps its state in server memory.
- **Move the content into the interactive component** and pass it the data: a `TopCustomersCard` with a `Customers` parameter, a `LowStockCard` with a `Products` parameter, each with its own copy of the collapse logic.
- **Leave it static and write the behaviour in JavaScript**, outside Blazor.

## How it works

When a static page renders an interactive component with prerendering on (the default), Blazor wraps each `RenderFragment` parameter so it records the render tree it produces: elements, attributes, text, markup, and child components with their parameters. That tree is serialized into the component's marker. When the circuit starts, the server turns it back into a `RenderFragment`, and the interactive component renders it like any other `ChildContent`.

- **It's a snapshot.** The fragment's code doesn't run again in the circuit. Values are the ones captured while the page prerendered. Components inside it are different: they're recreated in the circuit from their captured parameters, so their own code runs again there. To show fresh data, it has to come from the interactive side.
- **It must render while prerendering.** With `prerender: false`, or if the component doesn't render the fragment on its first pass (say it starts collapsed behind an `@if`), there's nothing to capture and it throws: *Cannot serialize RenderFragment parameter 'ChildContent' ... because the RenderFragment was not executed.* That's why `CollapsibleCard` always renders expanded first and restores the collapsed state afterwards.
- **What doesn't cross:** event handlers inside the fragment, `@ref` and `@formname` are dropped, with a warning in the server log. Passing a templated `RenderFragment<T>` to an interactive component still throws. Any non-generic `RenderFragment` parameter works, not just `ChildContent`.
- **It isn't small.** The captured tree is verbose JSON, encrypted with Data Protection. Each 8-row table here is about 2.4 KB of HTML but makes an 18 KB marker. The browser sends the markers to the server when the circuit starts, and with both panels on the page that message went over SignalR's 32 KB default: the circuit wouldn't start (*The maximum message size of 32768B was exceeded*). `Program.cs` raises `MaximumReceiveMessageSize` to 64 KB. Keep fragments modest, or raise the limit deliberately.
- **Scoped CSS survives.** The scope attributes are captured with the elements, so `Home.razor.css` still styles the tables after they've crossed.
- **ASP0032:** the feature added one public API, `RenderTreeBuilder.SetAttributeValue`, which the framework uses to wrap nested fragments. It's still marked `[Experimental("ASP0032")]` in RC2. You don't call it to use this feature, so you won't see the diagnostic unless you call it yourself.
