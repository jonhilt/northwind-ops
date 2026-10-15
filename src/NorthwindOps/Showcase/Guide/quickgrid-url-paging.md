---
id: quickgrid-url-paging
title: Grid state in the URL
group: Grids & lists
order: 30
hook: Sort, page, refresh: the grid stays put.
intro: Keep a grid's sort and page in the URL, so a refresh or a shared link shows the same view. New in .NET 11.
mode: Static SSR
demo: demo/grid-url/products?sort=Price&direction=desc&page=2
before: demo/grid-url/products/net10
labels: .NET 10 · recorded | .NET 11 · live
app: orders
---

## The problem

You sort a grid and go to page 2, then refresh or share the link. Why are you back on page 1?

In .NET 10, QuickGrid kept its sort and page in component state, never in the URL. On a static page its headers and pager were buttons with no handler, so they did nothing at all.

### Make the page interactive

```razor
@page "/products"
@rendermode InteractiveServer @* just to sort *@

<QuickGrid Items="products"
           Pagination="pagination">
    <PropertyColumn Property="p => p.UnitPrice"
                    Sortable="true" />
</QuickGrid>
<Paginator State="pagination" />
```

Sorting works, but the view lives in the circuit, so a refresh or a shared link still lands on page 1. {.bad}

### Read the query string yourself

```razor
<th><a href="products?sort=price">Price</a></th>

@code {
    [SupplyParameterFromQuery]
    public string? Sort { get; set; }
    [SupplyParameterFromQuery]
    public int? Page { get; set; }

    IQueryable<Product> Rows => (Sort == "price"
        ? db.Products.OrderBy(p => p.UnitPrice)
        : db.Products.OrderBy(p => p.Name))
      .Skip(((Page ?? 1) - 1) * 10).Take(10);
}
```

You give up QuickGrid's sorting and pager, and write the links and queries for every grid by hand. {.bad}

## See it work

Both grids were asked for `?sort=Price&direction=desc&page=2`, on a static page. Which one shows it?

.NET 11 changed QuickGrid itself, so the old grid can't run here. The left is what our .NET 10 repro returned, replayed as-is: its buttons are exactly as inert as they were.

1. On the left, .NET 10 ignored the URL: page 1, sorted by Product. Click **Price** or **›**: nothing happens, no request, no change.
2. On the right, the same URL opens on page 2, highest price first. The header arrow and the pager say so.
3. Click **Price**, or **‹** under the grid, and watch the frame's address: the URL changes with the view. Press **⟳** to refresh, and it stays.

Explain: In .NET 11, QuickGrid reads `sort`, `direction` and `page` from the URL, and renders its headers and pager as links to the next view. The page is static, with no circuit: every click is a plain request for a new URL.

## How it's built

What did the page have to change? Nothing: an ordinary QuickGrid and Paginator on a static page.

There's nothing to turn on: every QuickGrid in .NET 11 does this, and an interactive grid keeps the URL in step too.

### An ordinary QuickGrid, on a static page
code: Components/Guide/Demos/GridUrl/Products.razor

## Good to know

- **Sorting matches the column title.** `sort=Price` finds the column whose `Title` is Price. Rename it, and old links fall back to the default sort.
- **Two grids on one page?** Give each a prefix with its `QueryParameterNameOptions` parameter. `new QueryParameterNameOptions("cities_")` makes them `cities_sort`, `cities_direction` and `cities_page`.
- **Headers are links now, not buttons.** They render as `<a class="col-title">`, so update any CSS that styled the buttons. The AppContext switch `Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting` set to `false` brings the buttons back.
- **Your own query parameters survive.** A filter read with `[SupplyParameterFromQuery]` stays in the grid's links. Reset `page` when the filter changes, as the Orders screen does with `GetUriWithQueryParameters`.
