---
id: renderfragment-across-render-modes
title: Static content in interactive components
group: Rendering
order: 26
hook: An interactive card around a table the static page rendered.
intro: Put a static page's markup inside an interactive component, like a card you can fold away.
mode: Static SSR · Interactive Server card
demo: demo/static-content/low-stock
app:
---

## The problem

You've built an interactive card that folds away. Why can't a static page put a table inside it?

In .NET 10 that threw: `ChildContent` is code, and can't be serialized to an interactive component. Apps used one of two workarounds.

### Make the whole page interactive

```razor
@page "/warehouse"
@rendermode InteractiveServer

<Collapsible Title="Low stock">
    <table>...</table>
</Collapsible>
```

Every visitor holds a circuit for the whole page, just so one card can fold up. {.bad}

### Move the table into the component

```razor
<LowStockCard @rendermode="InteractiveServer"
              Products="products" />

@* LowStockCard.razor: the table, plus its
   own copy of the folding logic *@
```

A new card component for every table, each with its own copy of the folding logic. {.bad}

## See it work

Fold the card away, then open it again.

This is a static page with one interactive card on it, running for real. The page renders the table, and the card shows it.

1. Wait for **Interactive** in the card's corner. The card is now running on a circuit.
2. Click **▾ Low stock** to fold the card, and again to open it. The click runs in C# on the server.
3. The table comes back with the same **Queried at** time. The card replays a copy it was handed, without querying again.

## How it's built

Pass markup to an interactive component as usual. Blazor captures it while the page prerenders, and replays it inside the component.

Nothing to switch on: it works for `ChildContent` and any other plain `RenderFragment` parameter.

### The page: a table inside an interactive card
code: Components/Guide/Demos/StaticContent/LowStock.razor

### The card: it renders ChildContent like any other component
code: Components/Guide/Demos/StaticContent/Collapsible.razor

## Good to know

- **It's a snapshot.** The fragment's code doesn't run again in the circuit. For live data, load it on the interactive side.
- **Render it on the first pass.** With `prerender: false`, or content hidden behind an `@if` until later, there's nothing to capture, and it throws. That's why the card starts open.
- **Event handlers don't cross.** `@onclick`, `@ref` and `@formname` inside the fragment are dropped, with a warning in the log. A `RenderFragment<T>` still throws.
- **Keep it modest.** The captured markup travels in the component's marker, several times bigger than the HTML. The dashboard's two tables went past SignalR's 32 KB limit, so `Program.cs` raises `MaximumReceiveMessageSize`.
