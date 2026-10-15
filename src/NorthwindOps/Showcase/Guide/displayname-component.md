---
id: displayname-component
title: DisplayName
group: Forms
order: 14
hook: Grid headers that use your form's field names.
intro: Show a property's [Display] name as plain text, so a grid's headers say what the form's labels say. New in .NET 11, and works in any render mode.
mode: Static SSR
demo: demo/displayname/suppliers
before: demo/displayname/suppliers/net10
labels: .NET 10: headers typed as Title | .NET 11: DisplayName
app: suppliers
---

## The problem

Your form's model already names every field with `[Display]`. Why does the grid type them again?

Blazor in .NET 10 had no `DisplayNameFor`, MVC's helper for this. Validation messages read `[Display]`, but nothing rendered it as text, so apps used one of two workarounds.

### Type each column's Title

```razor
<QuickGrid Items="suppliers">
    <PropertyColumn Title="Supplier code"
        Property="s => s.Code" />
    <PropertyColumn Title="Main contact"
        Property="s => s.Contact" />
</QuickGrid>
```

Rename or translate a field on the model and the grid keeps the old English words, unless you look each one up again yourself. Leave `Title` out and QuickGrid prints the raw property name, `Contact`. {.bad}

### Write a reflection helper

```csharp
static string NameOf<T>(Expression<Func<T>> e)
{
    var member = ((MemberExpression)e.Body).Member;
    var display = member
        .GetCustomAttribute<DisplayAttribute>();
    return display?.GetName() ?? member.Name;
}
// <th>@NameOf(() => form.Code)</th>
```

It works, but it's one more helper to write, test and copy between projects. {.bad}

## See it work

Both grids list the same suppliers. Switch both to German: which grid's headers follow the form's model?

On the left, columns have a typed `Title`, or none, the .NET 10 way. On the right, each header is a `<DisplayName For>` pointing at the form's model. The dashed **Guide readout** above each grid isn't part of either technique: it lists the model's `[Display]` names right now, for you to compare.

1. In English, look at the left grid's third header: **Contact**. That column has no `Title`, so QuickGrid printed the property name, while the model calls it Main contact. The right grid reads Main contact.
2. Click **Deutsch** on the left. It adds `?culture=de-DE` to the URL, and the readout turns German (Lieferantencode, Firmenname, Hauptansprechpartner), but the headers stay English.
3. Click **Deutsch** on the right. The headers turn German with the readout. The German comes from `SupplierText.de.resx`, which the model's `[Display(ResourceType = …)]` points at; none is typed in the grid.

Explain: `<DisplayName For>` reads the property's `[Display]` name each time it renders, through `DisplayAttribute.GetName()`, which looks the name up in the request's culture. A typed `Title` is a copy, and copies don't follow; a missing one falls back to the property's name.

## How it's built

What's new? A header template holding `<DisplayName For>`, pointing at the form's model rather than the grid's row type.

`DisplayName` is a component, the text-only sibling of `<Label For>`. .NET 11 adds no method that returns the name as a string, so it can't go in `Title`: it goes in the column's `HeaderTemplate`. `For` is only read for which property it names and never run, so `form` can stay null. A `HeaderTemplate` replaces QuickGrid's own title element, so the demo restores the bold, and the sort button goes with it.

### New in .NET 11: each header points at the form model
code: Components/Guide/Demos/DisplayNames/Suppliers.razor

### Not new: the model, where each name is set once
code: Components/Guide/Demos/DisplayNames/SupplierForm.cs

### .NET 10: names typed again as Title, or left out
code: Components/Guide/Demos/DisplayNames/SuppliersNet10.razor

## Good to know

- **Sortable columns.** The template replaces the sort button too. Keep a plain `Title` on columns that sort, or add your own button that calls `context.Grid.SortByColumnAsync(context)`.
- **It reads attributes, not `IStringLocalizer`.** For names that change with the language, use `[Display(ResourceType = …)]`, as this model does.
- **Any render mode.** This page is static SSR; `DisplayName` renders the same text when interactive.
- **Falls back in order.** `[Display(Name)]`, then `[DisplayName]`, then the property's own name.
