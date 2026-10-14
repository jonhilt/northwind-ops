---
id: displayname-component
title: DisplayName
group: Forms
order: 14
hook: Grid headers that use your form's field names.
intro: Show a property's [Display] name as plain text, so a grid's headers match the form's labels.
mode: Static SSR
demo: demo/displayname/suppliers
app: suppliers
---

## The problem

Your grid's headers should say what your form's labels say. Why type them twice?

Blazor in .NET 10 had no `DisplayNameFor`, so apps used one of two workarounds.

### Type the headers in

```razor
<tr>
    <th>Supplier code</th>
    <th>Company name</th>
    <th>Main contact</th>
</tr>
```

Rename a field on the form and the grid keeps the old name, in every language you translate it into. {.bad}

### Write a reflection helper

```csharp
static string NameOf<T>(Expression<Func<T>> e)
{
    var member = ((MemberExpression)e.Body).Member;
    var display = member
        .GetCustomAttribute<DisplayAttribute>();
    return display?.GetName() ?? member.Name;
}
// <th>@NameOf(() => names.Code)</th>
```

One more helper to write, test and copy between projects, for something MVC has had built in for years. {.bad}

## See it work

Read the headers, then switch language.

This is a static page listing suppliers. Its headers come from the New supplier form's model.

1. The headers read Supplier code, Company name, Main contact and Country: the same words as the form's labels.
2. Click **Deutsch**. The headers turn German, though the page has no German in it.
3. Right-click a header and choose Inspect. `DisplayName` renders just the text, with no element around it.

## How it's built

`<DisplayName For>` reads a property's `[Display]` name, as `Label` does, and renders it as text.

The names live on the form's model, so the grid and the form can't drift apart.

### The form's model, where each name is set once
code: Components/Guide/Demos/DisplayNames/SupplierForm.cs

### The list's headers point at the model
code: Components/Guide/Demos/DisplayNames/Suppliers.razor

## Good to know

- **The expression is never run.** Only the property it points at matters, so an empty model instance is fine.
- **It reads attributes, not `IStringLocalizer`.** For names that change with the language, use `[Display(ResourceType = …)]`, as this model does.
- **QuickGrid loses its sort link.** A `HeaderTemplate` replaces the whole header, sort link included. Keep a plain `Title` on columns that sort.
- **Falls back in order.** `[Display(Name)]`, then `[DisplayName]`, then the property's own name.
