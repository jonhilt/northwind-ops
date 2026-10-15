---
id: cacheview
title: Caching part of a page
group: Rendering
order: 25
hook: Cache a slow panel's HTML, and keep the rest of the page live.
intro: Keep the rendered HTML of one slow part of a page, so its query doesn't run on every view, and visitors who arrive together run it once. New in .NET 11. It works where the server renders the page, so this demo is static server rendering.
mode: Static SSR
demo: demo/cacheview/stock
before: demo/cacheview/stock/net10
labels: .NET 10: IMemoryCache in the component | .NET 11: CacheView
app:
---

## The problem

One panel on a page runs a slow report query on every view. How do you cache just that panel?

Blazor in .NET 10 had nothing for caching part of a page, so apps used one of two workarounds.

### Cache the data in the component

```csharp
// @inject IMemoryCache Cache, in every component
protected override async Task OnInitializedAsync()
{
    rows = (await Cache.GetOrCreateAsync("stock",
        entry =>
        {
            entry.SetAbsoluteExpiration(ttl);
            return LoadRowsAsync();
        }))!;
}
```

Caching code in every component, keys you vary by hand, and visitors who miss at the same moment each run the query. {.bad}

### Cache the whole page

```razor
@page "/stock"
@attribute [OutputCache(Duration = 10)]
@* plus AddOutputCache() and UseOutputCache() *@

<h1>Stock by category</h1>
<StockSummary />
<ReorderForm />
```

All or nothing: the whole page freezes. And a form's antiforgery token sends `Set-Cookie` and `no-store`, so nothing is cached: in the .NET 10 repro every view still took 1.5 s, with no warning. {.bad}

## See it work

Both pages cache the same 1.5-second report for 10 seconds. When the cache has expired and five visitors arrive at once, how many times does each side run the query?

On the left, the report caches its own data in `IMemoryCache`, the first .NET 10 way (the second caches nothing once a form is on the page). On the right, the page wraps it in `<CacheView>`. Under each, a line counts visitors and report queries since the app started, so the totals don't start at zero: watch how much they rise.

1. Press **⟳** above each demo a few times. Both come back at once: **Page rendered at** moves, **Figures calculated at** stays, **visitors** rises and **report queries run** doesn't. Caching the data worked in .NET 10 too.
2. Wait until **Figures calculated at** is more than 10 seconds old, then press **Five visitors at once** on the left. It reads **Last burst: 5 visitors, 5 queries ran**: each visitor missed, and each ran the query.
3. Do the same on the right. It reads **5 visitors, 1 query ran**: the other four waited for the first visitor's render, and got its HTML.

Explain: `GetOrCreateAsync` doesn't lock, so every visitor who misses runs the factory. `CacheView` keeps the summary's rendered HTML, makes visitors who miss together wait for one render, and on a hit replays the HTML without creating `StockSummary` at all.

## How it's built

What's new? One component, wrapped around the slow part of the page. `StockSummary` inside it is an ordinary component with no caching code: it runs the query in `OnInitializedAsync`.

There's nothing to set up in `Program.cs`: CacheView keeps its entries in memory by default. The .NET 10 side needs `AddMemoryCache()`, and picks its own key, `"stock-report"`.

### .NET 11: the page wraps the report in a CacheView
code: Components/Guide/Demos/Caching/StockReport.razor

### .NET 10: the report caches its own data, and nothing waits
code: Components/Guide/Demos/Caching/StockSummaryNet10.razor

## Good to know

- **How entries are keyed: one for every visitor.** The key is where the CacheView sits, plus `CacheKey` if you set one. The user, query string and route aren't in it unless you add `VaryByUser`, `VaryByQuery`, `VaryByRoute` or `VaryByCulture`.
- **Entries last 30 seconds by default.** Set `ExpiresAfter`, `ExpiresOn` or `ExpiresSliding`. Sliding on its own still ends after 30 seconds.
- **Keep one small part fresh inside a cached one.** Mark a component `[CacheBehavior(CacheBehavior.Rerender)]` and it runs on every request, even on a hit. The app's dashboard uses one to say "Served from the cache · 12s old".
- **Only server-rendered GETs are cached.** Form posts render as usual, and so do CacheViews inside a `[StreamRendering]` component. Interactive components inside a CacheView aren't baked into its HTML.
