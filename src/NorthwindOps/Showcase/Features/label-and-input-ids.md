---
id: label-and-input-ids
title: Label component and generated input ids
badge: Label · input ids
release: Preview 1
mode: Any render mode
sources: Components/Pages/Suppliers/SupplierEditor.razor, Features/Suppliers/SupplierForm.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/forms/input-components?view=aspnetcore-11.0#label-component
summary: The new Label component renders a label whose text comes from the property's [Display] name, and the built-in input components now render a matching id. Labels and inputs are tied together with no hand-typed text or ids, so clicking a label focuses its field and screen readers announce the right name.
---

## Try it

1. Click the word **Email** (or any label). The cursor jumps into its input.
2. Inspect the field: `<label for="Supplier_Email">Email</label>` and `<input id="Supplier_Email" name="Supplier.Email">`. Neither the text nor the id is typed into the page.
3. Nested fields work the same way: the city is `Supplier_Address_City`. Press **Add contact** and the new row's name field is `Supplier_Contacts[0]_Name`.
4. Switch the page to **Deutsch**. Every label changes, because each one reads the model's `[Display]` metadata.

## The .NET 10 way

The input components rendered no `id`, so you made one up for every field and typed the label text next to it:

```razor
<label for="supplier-email">Email</label>
<InputText id="supplier-email" @bind-Value="Supplier.Email" />

<label for="supplier-city">City</label>
<InputText id="supplier-city" @bind-Value="Supplier.Address.City" />
```

That's two strings per field to keep in step, and the label text duplicates the `[Display(Name)]` that your validation messages already use. A typo in an id breaks the link without any warning. In a loop you have to build unique ids yourself. Leaving the `for` off is an accessibility bug that nothing points out.

## How it works

`Label<TValue>` takes a `For` expression. Its text comes from `[Display(Name)]` (through `DisplayAttribute.GetName()`, so `ResourceType` localization works), then `[DisplayName]`, then the property name.

- **Two ways to use it.** Without child content it renders `<label for="…">` pointing at the input's generated id, as on this form. With child content it wraps the input, which ties them together without an id: `<Label For="() => Model.Email"><InputText @bind-Value="Model.Email" /></Label>`.
- **Where the id comes from.** `InputBase<TValue>.IdAttributeValue` takes the field's `name` (`Supplier.Address.City`) and replaces dots and whitespace with `_`. `Label` uses the same rule, so the two match, inside collections too. An explicit `id` on the input still wins, but then the label's `for` no longer points at it, so wrap the input instead.
- **Custom inputs** derived from `InputBase<TValue>` should render `id="@IdAttributeValue"` so labels can find them.
- **Names are cached** per property and UI culture for the life of the process.
- **Watch out:** inputs now render an `id` they didn't before. Check for CSS, JavaScript or test selectors that could clash with the new ids. Ids from collections contain `[` and `]`, which need escaping in CSS selectors.
