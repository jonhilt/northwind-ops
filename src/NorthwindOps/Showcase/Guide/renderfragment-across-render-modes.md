---
id: renderfragment-across-render-modes
title: Static content in interactive components
group: Rendering
order: 26
hook: An interactive card around a table the static page rendered.
intro: Put a static page's markup inside an interactive component, like a card you can fold away. New in .NET 11.
mode: Static SSR · Interactive Server card
demo: demo/static-content/low-stock
before: demo/static-content/low-stock/net10
labels: .NET 10 · recorded | .NET 11 · live
app:
---

## The problem

You've built an interactive card that folds away. Why can't a static page put a table inside it?

In .NET 10 it compiled, then failed at run time with a 500: a `RenderFragment` is code, and .NET 10 wouldn't send code to an interactive component. Apps used one of two workarounds.

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

The same page, on .NET 10 and on .NET 11: a static page puts a table inside an interactive card. What does each one show?

.NET 11 fixed this inside the framework, so the old failure can't run here. The left is what our .NET 10 repro returned, recorded.

1. On the left, .NET 10 returned an HTTP 500 for the whole page, before any of it rendered. The code is the page on the right, unchanged.
2. On the right, wait for **Interactive** in the card's corner, then click **▾ Low stock** to fold the card and again to open it. The click runs in C# on the server.
3. The table comes back with the same **Queried at** time: the page's query didn't run again.

Explain: .NET 11 doesn't send the code. The static page runs the fragment while it prerenders, records the markup it produced, and sends that to the card, which replays it. So it's a snapshot: the table only changes if the page renders again.

## How it's built

What changed in your code? Nothing: the same page that failed on .NET 10 works on .NET 11.

Pass markup to an interactive component as usual. It works for `ChildContent` and any other plain `RenderFragment` parameter, on Server and WebAssembly.

### The page: a table inside an interactive card
code: Components/Guide/Demos/StaticContent/LowStock.razor

### The card: it renders ChildContent like any other component
code: Components/Guide/Demos/StaticContent/Collapsible.razor

## Good to know

- **Render it on the first pass.** With `prerender: false`, or content hidden behind an `@if` until later, there's nothing to record, and it throws. That's why the card starts open.
- **Event handlers don't cross.** `@onclick`, `@ref` and `@formname` inside the fragment are dropped, with a warning in the log. A `RenderFragment<T>` still throws.
- **Keep it modest.** The recorded markup travels in the component's marker, several times bigger than the HTML. The dashboard's two tables went past SignalR's 32 KB limit, so `Program.cs` raises `MaximumReceiveMessageSize`.
- **For live data, load it in the card.** The fragment is a snapshot of the static render, so anything that should update belongs on the interactive side.
