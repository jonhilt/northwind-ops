---
id: label-and-input-ids
title: Labels and input ids
group: Forms
order: 13
hook: Labels that name and point at their field, with nothing typed twice.
intro: Render each field's label from the model's [Display] name, tied to an id the input now renders for itself.
mode: Static SSR
demo: demo/labels/suppliers/new
app: suppliers/new
---

## The problem

Clicking a label should put the cursor in its field. Why does every Blazor form need hand-typed ids for that?

In .NET 10, input components rendered no `id` and there was no `Label` component, so apps used one of two workarounds.

### Type the ids and the text

```razor
<label for="supplier-company">Company name</label>
<InputText id="supplier-company"
           @bind-Value="Input.Company" />

<label for="supplier-email">Contact email</label>
<InputText id="supplier-email"
           @bind-Value="Input.Email" />
```

Two strings per field to keep in step, and a typo in an id breaks the link without a warning. {.bad}

### Wrap each input in its label

```razor
<label>
    Company name
    <InputText @bind-Value="Input.Company" />
</label>
```

No ids, but the name is still typed by hand, and drifts from the `[Display]` name your messages use. {.bad}

## See it work

Click a label, then look at the markup.

This is a cut-down copy of the New supplier screen, as a static page. No label text or id is typed into it.

1. Click the words **Company name**. The cursor jumps into its field.
2. Right-click the field and choose Inspect. The label has `for="Input_Company"`, and the input has `id="Input_Company"`.
3. Press **Save supplier** with the form empty. Each message uses the label's name: "The Company name field is required."

## How it's built

Name each field once, with `[Display]`. `<Label For>` renders that name, pointing at the id the input renders for itself.

The id comes from the field's name: `Input.Company` becomes `Input_Company`.

### The model names each field once
code: Components/Guide/Demos/Labels/NewSupplierForm.cs

### The page: a Label for each input
code: Components/Guide/Demos/Labels/NewSupplier.razor

## Good to know

- **Or wrap the input.** Give `Label` child content, like `<Label For="…"><InputText … /></Label>`, and it wraps the input with no `for` at all.
- **An explicit id wins.** Set `id` on an input and the label's `for` no longer matches it. Wrap the input instead.
- **Inputs render an id now.** Check for CSS, scripts or test selectors that could clash. Ids in lists, like `Input_Contacts[0]_Name`, need `[` and `]` escaped in CSS.
- **Your own inputs** derived from `InputBase<T>` should render `id="@IdAttributeValue"`, so labels can find them.
