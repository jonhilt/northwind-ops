---
id: quickgrid-url-paging
title: Grid state in the URL
group: Grids & lists
order: 30
hook: Sort, page, refresh: the grid stays put.
intro: Keep a grid's sort and page in the URL, so a refresh or a shared link shows the same view.
mode: Static SSR
demo: demo/grid-url/products
app: orders
---

## The problem

You sort a grid and go to page 3, then refresh. Why are you back on page 1?

In .NET 10, QuickGrid's sort headers and pager were buttons that only worked in an interactive render mode, so apps used one of two workarounds.

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

A circuit per visitor, holding the view in server memory, so a refresh or a link still lands on page 1. {.bad}

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

Sort and page the grid, then refresh.

This is a small product grid on a static page, running for real. There's no circuit: the headers and the pager are plain links.

1. Click **Price** twice to sort highest first. The address bar gains `sort=Price` and `direction=desc`.
2. Press **›** under the grid to go to page 2. The URL gains `page=2`, and the sort stays.
3. Press **⟳** above the demo to refresh. Same sort, same page: the view lives in the URL.

## How it's built

QuickGrid reads its sort and page from the URL. Its headers and pager are links.

The parameters are `sort`, `direction` and `page`. There's nothing to turn on: every QuickGrid in .NET 11 does this.

### An ordinary QuickGrid, on a static page
code: Components/Guide/Demos/GridUrl/Products.razor

## Good to know

- **Sorting matches the column title.** `sort=Price` finds the column whose `Title` is Price. Rename it, and old links fall back to the default sort.
- **Two grids on one page?** Give each a prefix with its `QueryParameterNameOptions` parameter. `new QueryParameterNameOptions("cities_")` makes them `cities_sort`, `cities_direction` and `cities_page`.
- **Your own query parameters survive.** A filter read with `[SupplyParameterFromQuery]` stays in the grid's links. Reset `page` when the filter changes, as the Orders screen does with `GetUriWithQueryParameters`.
- **Headers are links now, not buttons.** They render as `<a class="col-title">`, so update any CSS that styled the buttons.
