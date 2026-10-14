---
id: cacheview
title: CacheView: caching part of a server-rendered page
badge: CacheView
release: Preview 7
mode: Static SSR
sources: Components/Pages/Home.razor, Components/Pages/Dashboard/KpiAge.razor, Components/Pages/Dashboard/KpiTiles.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/state-management/cacheview-component?view=aspnetcore-11.0
summary: Wrap part of a server-rendered page in <CacheView> and Blazor keeps its rendered HTML. On a cache hit the components inside aren't created at all, so their slow queries don't run. Small parts that must stay fresh can opt out with [CacheBehavior].
---

## Try it

1. Reload the dashboard. **Page rendered at** changes every time, but **Figures as of** stays put: the KPI tiles are coming from the cache.
2. Watch the green note under the tiles: it counts how old the cached figures are. That note is a live hole in the cache, and it's recalculated on every request. It knows a hit from a miss because the tiles component, which only exists on a miss, leaves a flag for it in `HttpContext.Items`.
3. Wait a minute and reload. The page takes about a second and a half while the slow query runs again, and the note says **Calculated for this request**.
4. Compare in DevTools → Network: a cached load of `/` takes a few milliseconds, and a miss takes over 1.5 seconds.

## The .NET 10 way

Blazor had nothing for caching part of a page. MVC has a `<cache>` tag helper, and output caching works for a whole response, but a page with anything per-request in it (an antiforgery token, an interactive component, the current user's name) can't be cached whole.

So you cached the data instead, inside every component that needed it:

```razor
@inject IMemoryCache Cache
@inject DashboardQueries Queries

@code {
    DashboardKpis? kpis;

    protected override async Task OnInitializedAsync()
    {
        kpis = await Cache.GetOrCreateAsync("dashboard-kpis", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return Queries.GetKpisAsync();
        });
    }
}
```

That's still a reasonable thing to do, but it has costs. The caching is buried in each component's code, so you can't see from the page what's cached. You choose the key yourself and have to remember to add the user, the culture or the query string to it when the output depends on them. Every request still creates and renders the whole component tree. And `GetOrCreateAsync` doesn't stop several requests that miss at the same moment from all running the slow query.

## How it works

```razor
<CacheView CacheKey="dashboard-kpis" ExpiresAfter="TimeSpan.FromMinutes(1)">
    <KpiTiles />
</CacheView>
```

On a miss, Blazor renders the children as usual and records what they produce. On a hit, it replays the recording and the child components are never created. If two requests miss at the same time, the second waits for the first one's render instead of running the query again.

- **The key** is where the CacheView is rendered (the type of the component whose output contains it, and its place in that output), plus `CacheKey` if you set one. The URL isn't part of it, so a component with a CacheView inside shares one entry on every page that uses it. Nothing else goes in unless you ask: `VaryByQuery` (comma-separated names, or `"*"`), `VaryByRoute`, `VaryByHeader` and `VaryByCookie` take names, `VaryByUser` and `VaryByCulture` are booleans, and `VaryBy` takes any string. Forget `VaryByUser` on per-user content and one user's output is served to everyone.
- **Expiry:** `ExpiresAfter`, `ExpiresOn` (a `DateTimeOffset`) or `ExpiresSliding`. With none of them set, an entry lasts 30 seconds. `ExpiresSliding` on its own still gets that 30-second absolute limit, so pair it with `ExpiresAfter` or `ExpiresOn`.
- **Only plain GET requests are cached.** Other methods, `Enabled="false"`, and a CacheView inside a component with `[StreamRendering]` all skip the cache and render normally.
- **Live holes:** a component marked `[CacheBehavior(CacheBehavior.Rerender)]` isn't baked into the cached HTML. It runs its lifecycle on every request, but its parameters are the ones captured when the entry was created. That's how the age note here works: it gets the original `AsOf` time and compares it with now. `AntiforgeryToken` and `HeadOutlet` are marked this way, and so are interactive components, so their markers are never cached.
- **Components that refuse:** `[CacheBehavior(CacheBehavior.Throw)]` makes a component throw inside a CacheView. `Virtualize` does. `QuickGrid` throws unless the CacheView varies by query, and `AuthorizeView` unless it varies by user, which they declare with `[CacheCondition(CacheVaryBy.Query)]` and `[CacheCondition(CacheVaryBy.User)]`. You can use the same attributes on your own components.
- **CacheViews can't be nested**, and two CacheViews that end up with the same key in one request (a reusable component used twice, or a CacheView in a loop) throw. Give each one its own `CacheKey`.
- **Storage** is an in-memory cache capped by `RazorComponentsServiceOptions.CacheViewSizeLimit` (100 MB by default; when it's full, nothing new is cached until entries expire). If you register a `HybridCache` (from the Microsoft.Extensions.Caching.Hybrid package), CacheView uses that instead (or set `CacheViewHybridCache` to pick one), so entries can be shared between servers through its distributed cache. `ExpiresSliding` isn't supported with HybridCache and throws.
