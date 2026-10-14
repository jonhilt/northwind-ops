---
id: displayname-component
title: DisplayName component
badge: DisplayName
release: Preview 1
mode: Any render mode
sources: Components/Pages/Suppliers/SupplierList.razor
docs: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md
summary: DisplayName renders a property's display name as plain text, read from [Display] or [DisplayName]. It's Blazor's version of MVC's Html.DisplayNameFor, handy for grid headers and read-only views that should use the same names as your forms.
---

## Try it

1. Compare the grid's column headers with the labels on the **New supplier** form: Supplier code, Company name, Main contact. They're the same words because they come from the same `[Display]` attributes.
2. Switch to **Deutsch**. The headers become Lieferantencode, Firmenname and so on, with no German in this page's markup.
3. Inspect a header. `DisplayName` renders just the text, with no wrapping element, so it fits anywhere.

## The .NET 10 way

Blazor had no `DisplayNameFor`, so headers were typed in, and translated, a second time:

```razor
<PropertyColumn Title="Supplier code" Property="s => s.Code" />
<PropertyColumn Title="Company name" Property="s => s.Company" />
```

Rename a field on the form and the grid keeps the old name. The alternative was a reflection helper that dug the `DisplayAttribute` out of an expression, which most teams wrote at some point.

## How it works

```razor
<HeaderTemplate><DisplayName For="() => names.Code" /></HeaderTemplate>
```

`DisplayName<TValue>` takes a `For` expression and renders `[Display(Name)]` (through `DisplayAttribute.GetName()`, so `ResourceType` localization works), then `[DisplayName]`, then the property name. It's the same lookup `Label` uses.

- **The expression is never run.** Only the property it points at matters. This page points at a `SupplierForm` that's never shown, so the grid reuses the form's names instead of repeating them on the row type.
- **It reads attributes, not `IStringLocalizer`.** For names that change with the culture, use `[Display(ResourceType = …)]`, as the Suppliers screen does.
- **Names are cached** per property and UI culture for the life of the process.
- **QuickGrid:** a `HeaderTemplate` replaces the whole header, including the sort link QuickGrid renders for a `Sortable` column, and the template can't rebuild that link because the method that builds its URL is internal. This grid's columns don't sort. A column that needs sorting has to keep a plain-text `Title`.
