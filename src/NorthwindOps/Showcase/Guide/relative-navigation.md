---
id: relative-navigation
title: Relative links
group: Pages & navigation
order: 22
hook: Tabs that link to their siblings, wherever the page lives.
intro: Link from a page to its siblings, like an order's tabs, without spelling out the full path. New in .NET 11.
mode: Any render mode
demo: demo/relative-nav/orders/12047
before: demo/relative-nav/net10/orders/12047/summary
labels: .NET 10: href="lines" | .NET 11: RelativeToCurrentUri
app: purchasing
---

## The problem

An order page has tabs: Summary, Lines, Customer. Can each tab link to its neighbours without spelling out `/orders/12047/`?

Blazor pages have `<base href="/">`, so a relative link like `href="lines"` points at the app's root, `/lines`, not the current page. In .NET 10 `NavLink` and `NavigateTo` had no way round that, so apps used one of two workarounds. .NET 11 adds an option, `RelativeToCurrentUri`, but it's off by default: a plain `href="lines"` still goes to `/lines`.

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

Both pages show order 12047, and both tab strips say just `href="lines"`. Which one takes you to the order's lines?

Both frames run on .NET 11. On the left, the tabs are plain `NavLink`s with the option left off, which is exactly what .NET 10 did (the .NET 10 repro lands on `/lines` too). On the right, each has `RelativeToCurrentUri="true"`. Watch the tabs, and the **You are at** line under them.

1. Before clicking, look at the tabs. On the right, **Summary** is underlined: `NavLink` knows it's the current page. On the left, none is, because Summary points at `/summary`. The right frame was opened at `…/orders/12047`; **You are at** shows `…/orders/12047/summary`, where the page's `NavigateTo` redirected it.
2. Click **Lines** on the left. The address becomes `/lines` and the app answers **Not Found**.
3. Click **Lines** on the right. The order's lines show, **Lines** is underlined, and **You are at** reads `/demo/relative-nav/orders/12047/lines`.
4. On the right, click **← Orders** and open another order. Its link is just `…/orders/{id}`; **You are at** shows `…/orders/{id}/summary`, and the same tabs now point under it.

Explain: A relative link resolves against `<base href="/">`, the app's root, so on the left `lines` means `/lines`. `RelativeToCurrentUri` resolves it against the current URL's folder instead: everything after its last `/` is dropped, and `lines` goes on the end. From `…/12047/summary` that's `…/12047/lines`. The redirect uses the same option on `NavigateTo`, but from `…/orders/12047` the last segment is the id itself, so it says `12047/summary`.

## How it's built

What's new? One parameter on each `NavLink`, and the same option for `NavigateTo`.

Both pages are static SSR and share the tab content, so only the links differ. The option works the same in every render mode.

### .NET 11: three relative links, and no parameters
code: Components/Guide/Demos/RelativeLinks/OrderTabs.razor

### The left frame (.NET 10): the same links, resolved from the app's root
code: Components/Guide/Demos/RelativeLinks/OrderTabsNet10.razor

### The page redirects to a tab with NavigateTo
code: Components/Guide/Demos/RelativeLinks/OrderPage.razor

## Good to know

- **Mind the last segment.** The option saves string surgery for sibling links, like tabs. To go *into* a page's own folder, from `/orders/12047` to its summary, the last segment is dropped, so you name it again (`12047/summary`). With a trailing slash, `/orders/12047/`, it's just `summary`: the page checks which.
- **Not in a .NET 10 app.** There, `RelativeToCurrentUri` isn't a `NavLink` parameter, yet `RelativeToCurrentUri="true"` still compiles: `NavLink` passes any attribute it doesn't know through to the `<a>`. The link still points at the app's root, with no warning. `NavigationOptions.RelativeToCurrentUri` doesn't compile there.
- **Relative paths only.** With the option on, a path starting with `/`, or an absolute URL, throws `ArgumentException`.
- **`?` and `#` keep the page.** `?tab=2` replaces just the query, and `#notes` just the fragment.
