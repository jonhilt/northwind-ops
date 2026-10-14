---
id: virtualize-csp
title: Virtualize under a strict CSP
group: Grids & lists
order: 43
hook: Long lists that still work when inline styles are banned.
intro: Use Virtualize on pages whose Content-Security-Policy refuses inline styles.
mode: Interactive Server
demo: demo/virtualize-csp/orders
app: support
---

## The problem

Your Content-Security-Policy bans inline styles. Why do your virtualized lists stop working?

`Virtualize` in .NET 10 held the place of unrendered items with `<div style="height: …">`. The policy refuses those, so the list loses its scroll range. Apps used one of two workarounds.

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

Scroll the orders, under a policy with no `'unsafe-inline'`.

This page is served with `Content-Security-Policy: style-src 'self'`. The list holds every order in the database, virtualized.

1. Read the dashed line at the top. It asks for red text in a `style="…"` attribute, but it's grey: the policy is on.
2. Scroll the orders to the very end. The scrollbar covers every order, and rows render as you go.
3. Open DevTools → Console. The only CSP error is the dashed line's. Virtualize causes none.

## How it's built

Set the policy, then use `Virtualize` as usual. The list needs no changes.

Spacers now carry their height in a `data-blazor-virtualize-reserved-height` attribute. Blazor's script applies it through the CSS Object Model, which a CSP doesn't restrict.

### The page sends a policy that refuses inline styles
code: Components/Guide/Demos/VirtualizeCsp/Orders.razor

### The list is an ordinary Virtualize
code: Components/Guide/Demos/VirtualizeCsp/OrderList.razor

## Good to know

- **Your own inline styles are refused too.** Any `style="…"` a component renders is blocked, like the dashed line's. Move those styles into stylesheets.
- **DevTools still shows `style="height: …"` on the spacers.** That's the script's CSSOM writes, reflected in the attribute. The policy only refuses styles in the markup, or set with `setAttribute`.
- **A policy belongs to a document.** Enhanced navigation patches the next page into the current one, so its header is ignored. The Support screen also sends its policy in a `<meta>` tag for that reason.
- **Lock down scripts as well.** This demo only restricts styles, set from a static page. The Support screen sets a full policy in middleware, and allows Blazor's inline import map by its hash.
