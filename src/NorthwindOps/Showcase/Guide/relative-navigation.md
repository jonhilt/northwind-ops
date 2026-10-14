---
id: relative-navigation
title: Relative links
group: Pages & navigation
order: 22
hook: Tabs that link to their siblings, wherever the page lives.
intro: Link from a page to its siblings, like an order's tabs, without spelling out the full path.
mode: Static SSR
demo: demo/relative-nav/orders
app: purchasing
---

## The problem

An order page has tabs: Summary, Lines, Customer. Can each tab link to its neighbours without spelling out `/orders/11077/`?

Blazor pages have `<base href="/">`, so in .NET 10 a relative link pointed at the app's root, not the current page. Apps used one of two workarounds.

### Spell out the full path

```razor
<NavLink href="@($"orders/{OrderId}/summary")">
    Summary
</NavLink>
<NavLink href="@($"orders/{OrderId}/lines")">
    Lines
</NavLink>

@code {
    [Parameter] public int OrderId { get; set; }
}
```

The tabs need the order id and the whole path. Move the page, or reuse the tabs elsewhere, and every link changes. {.bad}

### Work out the folder yourself

```csharp
var path = new Uri(Navigation.Uri).AbsolutePath;
var folder = path[..(path.LastIndexOf('/') + 1)];

Navigation.NavigateTo(folder + "lines");
```

String surgery, copied into every component that links to a sibling page. {.bad}

## See it work

Open an order, then click through its tabs.

This is a cut-down order page, running for real on static SSR. Its tabs aren't told which order they're on.

1. Click an order number. The page sends you on to `…/orders/{id}/summary`.
2. Click **Lines**, then **Customer**. Their code says just `href="lines"` and `href="customer"`, yet the address bar keeps the order's id.
3. Go back to **Orders** and open another. The same links now point under that order, and the current tab is underlined.

## How it's built

Set `RelativeToCurrentUri` on a `NavLink`, or in `NavigationOptions`. The link then resolves against the current page, not the app's root.

It works like a link on a page with no `<base>`: everything after the last `/` is dropped, and your path goes on the end.

### The tabs: three relative links, and no parameters
code: Components/Guide/Demos/RelativeLinks/OrderTabs.razor

### The page: it redirects to a tab, then shows it
code: Components/Guide/Demos/RelativeLinks/OrderPage.razor

## Good to know

- **Mind the last segment.** From `/orders/11077`, `summary` means `/orders/summary`. That's why the page's redirect says `11077/summary`.
- **Relative paths only.** With the option on, a path starting with `/`, or an absolute URL, throws `ArgumentException`.
- **`?` and `#` keep the page.** `?tab=2` replaces just the query, and `#notes` just the fragment.
- **Off by default.** Both options default to `false`, so existing links behave as before.
