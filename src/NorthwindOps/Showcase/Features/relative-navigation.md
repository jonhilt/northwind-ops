---
id: relative-navigation
title: Relative navigation with RelativeToCurrentUri
badge: RelativeToCurrentUri
release: Preview 1
mode: Any render mode
sources: Components/Pages/Purchasing/PurchaseOrderWizard.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-11.0
summary: NavLink and NavigationManager.NavigateTo can resolve a link against the current page instead of the app's base URI. The wizard's steps link to "supplier", "lines" and "review", so none of its links spell out /purchasing/new/.
---

## Try it

1. Start a new purchase order and pick a supplier. The step links at the top are written as `href="supplier"` and `href="lines"`. Inspect one: it renders as `http://…/purchasing/new/lines`, and the current step gets NavLink's `active` class.
2. Open DevTools → Network and click **Choose products**. The form post is redirected to `/purchasing/new/lines` by `NavigateTo("lines", new NavigationOptions { RelativeToCurrentUri = true })`.
3. Go to `/purchasing/new` (the **Resume draft** link). It sends you to the step your draft was on. From `/purchasing/new` the relative path has to be `new/lines`, because a relative link resolves against the current page's folder, which there is `/purchasing/`.

## The .NET 10 way

Blazor pages have `<base href="/">`, so a relative link resolves against the app's root, not the page you're on. `NavLink` and `NavigateTo` did the same. Every step link had to spell out the full path:

```razor
<NavLink href="purchasing/new/supplier">Supplier</NavLink>
<NavLink href="purchasing/new/lines">Products</NavLink>

@code {
    void SaveLines() => Navigation.NavigateTo("purchasing/new/review");
}
```

Move the wizard, or reuse a step under `/suppliers/7/reorder/`, and you hunt down every string. The alternative was working out the current folder yourself:

```csharp
var path = new Uri(Navigation.Uri).AbsolutePath;
var folder = path[..(path.LastIndexOf('/') + 1)];
Navigation.NavigateTo(folder + "review");
```

## How it works

```razor
<NavLink href="lines" RelativeToCurrentUri="true">Products</NavLink>

@code {
    void Next() => Navigation.NavigateTo("review", new NavigationOptions { RelativeToCurrentUri = true });
}
```

`NavigationOptions.RelativeToCurrentUri` and `NavLink.RelativeToCurrentUri` are both `bool`, and both default to `false`, so existing links behave as before.

- **It resolves against the current URL's folder.** Everything after the last `/` of the current path is dropped, and your path is added as written (NavLink renders `./` and `../` as they are, for the browser to resolve). From `/purchasing/new/lines`, `review` becomes `/purchasing/new/review`. From `/purchasing/new` (no trailing slash) it would be `/purchasing/review`, which is why the resume redirect uses `new/lines`.
- **Query and fragment links keep the page.** A value that starts with `?` replaces the current URL's query (and drops any fragment); one that starts with `#` replaces just the fragment.
- **Relative only.** With the option on, an absolute URL or a path starting with `/` throws `ArgumentException`.
- **NavLink resolves it while rendering**, so it works on static SSR pages like this one. It renders the resolved, absolute URL as the `href`, and matches that for its `active` class.
- **On a static page, `NavigateTo` in a form handler becomes a redirect.** This app also sets `<BlazorDisableThrowNavigationException>`, so `NavigateTo` returns instead of throwing, and the wizard's handlers stop by checking a flag.
- **Leaving the wizard is an ordinary link.** After sending, the wizard calls `NavigateTo("purchasing")`, relative to the app's base as before: relative paths are for moving between sibling pages.
