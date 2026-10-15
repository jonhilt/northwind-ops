---
id: virtualize-csp
title: Virtualize under a strict CSP
group: Grids & lists
order: 43
hook: Long lists that still work when inline styles are banned.
intro: Use Virtualize on pages whose Content-Security-Policy refuses inline styles. Fixed in .NET 11.
mode: Interactive Server
demo: demo/virtualize-csp/orders
before: demo/virtualize-csp/orders/net10
labels: .NET 10 · recorded | .NET 11 · live
app: support
---

## The problem

Your Content-Security-Policy bans inline styles. Why do your virtualized lists stop working?

`Virtualize` in .NET 10 held the place of unrendered items with `<div style="height: …">`. The policy refuses those, so the spacers collapse to nothing and the list silently ends after its first screenful. Apps used one of two workarounds.

### Allow inline styles after all

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.ContentSecurityPolicy =
        "style-src 'self' 'unsafe-inline'";
    await next();
});
```

Injected markup can now restyle the page, say to hide a warning, and security reviews flag it. {.bad}

### Don't virtualize

```razor
@* Every order on the page at once *@
@foreach (var order in orders)
{
    <OrderRow Order="order" />
}
```

Every row is on the page, so a long list is slow to load and to scroll. {.bad}

## See it work

Both lists sit on a page whose policy refuses inline styles. Scroll each to the end: how far does it go?

.NET 11 fixed this inside `Virtualize`, so the old list can't run here. The left replays the rows and spacers our .NET 10 repro rendered, under the same policy, so your browser refuses them just as it did on .NET 10.

1. Scroll the left list to the end. It stops at **Order #19**, though the list holds 1,000: its bottom spacer asked for `style="height: 39240px"`, and the policy refused it, so there's nothing left to scroll.
2. Scroll the right list to the end. The scrollbar covers every order, and rows render as you go.
3. Open DevTools → Console. The left frame logs "Applying inline style violates the following Content Security Policy directive". The right logs none for its list.

Explain: A .NET 11 spacer carries its height in a `data-blazor-virtualize-reserved-height` attribute instead, and Blazor's script applies it through the CSS Object Model. A CSP restricts styles written into markup, not ones set from script, so the spacers keep their height.

## How it's built

What changed in your code? Nothing: the list is an ordinary `<Virtualize Items="orders" ItemSize="41">`. What changed is the spacers it renders.

.NET 10 wrote `<div style="height: 39240px">`, which the policy refuses. .NET 11 writes `<div data-blazor-virtualize-reserved-height="39240">`, and Blazor's script reads it and sets `element.style.height`. A CSP checks styles written into the page's markup, not ones script sets on an element, so that height stands. It applies in every interactive render mode, and to QuickGrid's virtualized rows too.

### The page sends a policy that refuses inline styles
code: Components/Guide/Demos/VirtualizeCsp/Orders.razor

### Unchanged: the list, and the spacers it now renders
code: Components/Guide/Demos/VirtualizeCsp/OrderList.razor

### Recorded: the .NET 10 spacers the policy refuses
code: Components/Guide/Demos/VirtualizeCsp/OrdersNet10.razor

## Good to know

- **DevTools shows `style="height: …"` on the .NET 11 spacers too.** That isn't markup: it's the height the script set with `element.style.height`, which DevTools displays as an attribute. The policy never sees it.
- **Your own inline styles are refused too.** Any `style="…"` a component renders is blocked, like the dashed line's on the right. Move those styles into stylesheets.
- **A policy belongs to a document.** Enhanced navigation patches the next page into the current one, so its header is ignored. The Support screen also sends its policy in a `<meta>` tag for that reason.
- **Lock down scripts as well.** This demo only restricts styles. The Support screen sets a full policy in middleware, and allows Blazor's inline import map by its hash.
