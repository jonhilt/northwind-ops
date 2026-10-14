---
id: quickgrid-url-paging
title: QuickGrid sorting and paging in the URL
badge: QuickGrid · URL sort & paging
release: Preview 5
mode: Static SSR
sources: Components/Pages/Orders.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid?view=aspnetcore-11.0
summary: QuickGrid now keeps its sort column, direction and page in the query string. Headers and the paginator are plain links, so the grid works on a static page with no circuit, and every view is a URL you can bookmark or share.
---

## Try it

1. Sort by **Total**, then go to page 3. Watch the address bar.
2. Copy the URL into a new tab: you land on exactly the same view.
3. Press Back. You step back through your sorts and pages like any other website.
4. Open DevTools → Network → WS. There's no WebSocket: this page never starts a circuit.

## The .NET 10 way

In .NET 8–10, the sort headers and the paginator were `<button @onclick>` elements. On a static SSR page they render but do nothing, so you had to make the page interactive just to sort a table:

```razor
@page "/orders"
@rendermode InteractiveServer   @* a circuit per visitor, just to sort and page *@

<QuickGrid Items="orders" Pagination="pagination">
    <PropertyColumn Property="o => o.Total" Sortable="true" />
</QuickGrid>
<Paginator State="pagination" />
```

That costs a WebSocket and server memory for every visitor, and the grid's state lives in that memory. Refresh the page and you're back on page one, sorted by default. A link to "page 3, by amount" is impossible, and Back leaves the page entirely.

The alternative was to give up on QuickGrid's built-in sorting and hand-roll it: `[SupplyParameterFromQuery]` properties for page and sort, your own header links, and your own `Skip`/`Take`/`OrderBy` code.

## How it works

QuickGrid reads `page`, `sort` and `direction` from the query string. On a static page it renders sortable headers and the paginator as `<a href>` links. Enhanced navigation fetches the next page and patches the DOM without a full reload. With an interactive render mode, the same parameters are kept in the URL as you click.

- **It plays well with your own parameters.** The status filter on this page is an ordinary `[SupplyParameterFromQuery]`. The grid's links keep it, and the filter links reset `page` with `GetUriWithQueryParameters`.
- **Several grids on one page?** Give each a prefix: `QueryParameterNameOptions="new QueryParameterNameOptions(\"cities_\")"`.
- **Sorting matches on the column's `Title`.** Renaming a column breaks bookmarks that sort by it.
- **This is a breaking markup change.** Headers are now `<a class="col-title">` rather than `<button>`, so update any CSS that targeted the buttons. To get the old behaviour back, set the AppContext switch `Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting` to `false`.
