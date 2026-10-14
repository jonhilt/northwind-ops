---
id: cacheview
title: Caching part of a page
group: Rendering
order: 25
hook: Cache a slow panel's HTML, and keep the rest of the page live.
intro: Keep the rendered HTML of one slow part of a page, so its query doesn't run on every view.
mode: Static SSR
demo: demo/cacheview/stock
app:
---

## The problem

One panel on a page runs a slow report query on every view. How do you cache just that panel?

Blazor in .NET 10 had nothing for caching part of a page, so apps used one of two workarounds.

### Cache the data yourself

```csharp
// The slow component, with IMemoryCache
protected override async Task OnInitializedAsync()
{
    if (!Cache.TryGetValue("stock", out rows))
    {
        rows = await LoadRowsAsync();
        Cache.Set("stock", rows,
            TimeSpan.FromSeconds(20));
    }
}
```

Caching code in every component, with keys you vary by hand, and the HTML is still rendered on every request. {.bad}

### Cache the whole page

```razor
@page "/stock"
@attribute [OutputCache(Duration = 20)]
@* plus AddOutputCache() and UseOutputCache() *@

<h1>Stock by category</h1>
<StockSummary />
```

All or nothing: one cached response for the whole page, so anything per-visitor on it, like a form, rules it out. {.bad}

## See it work

Refresh the report a few times.

This is a small stock report on a static page, running for real. Its figures take a second and a half to calculate, and are cached for 20 seconds.

1. Press **⟳** above the demo. **Page rendered at** changes, but **Figures calculated at** stays put, and the page comes back at once.
2. Wait 20 seconds and refresh again. This time it takes a moment: the cache expired, so the slow query ran again.
3. Within 20 seconds, press **↗ Open this demo in its own tab**. Same **Figures calculated at**: there's one cache entry, shared by every visitor.

## How it's built

Wrap the slow part in `<CacheView>`. On a hit, Blazor replays the HTML it kept, and the components inside aren't created.

There's nothing to set up in `Program.cs`: CacheView keeps its entries in memory by default.

### The page: the report sits inside a CacheView
code: Components/Guide/Demos/Caching/StockReport.razor

### The report: slow, and only run on a miss
code: Components/Guide/Demos/Caching/StockSummary.razor

## Good to know

- **One entry for everyone.** The key is where the CacheView sits, plus `CacheKey` if you set one. Add `VaryByUser`, `VaryByQuery`, `VaryByRoute` or `VaryByCulture` when the content depends on them.
- **30 seconds by default.** Set `ExpiresAfter`, `ExpiresOn` or `ExpiresSliding`. Sliding on its own still ends after 30 seconds.
- **Live holes.** A component inside marked `[CacheBehavior(CacheBehavior.Rerender)]` runs on every request, like the dashboard's "Served from the cache · 12s old".
- **GET requests only.** Form posts, and CacheViews inside a `[StreamRendering]` component, skip the cache and render as usual.
