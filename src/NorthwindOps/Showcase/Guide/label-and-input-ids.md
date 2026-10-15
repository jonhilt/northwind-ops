---
id: label-and-input-ids
title: Labels and input ids
group: Forms
order: 13
hook: Labels that name and point at their field, with nothing typed twice.
intro: Render each field's label from the model's [Display] name, tied to an id the input now renders for itself. New in .NET 11.
mode: Any render mode
demo: demo/labels/suppliers/new
before: demo/labels/suppliers/new/net10
labels: .NET 10: ids and names typed by hand | .NET 11: Label For
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
    Contact email
    <InputText @bind-Value="Input.Email" />
</label>
```

No ids, so focus works. But the name is still typed by hand, and drifts from the `[Display]` name your messages use. {.bad}

## See it work

Both forms have the same model. Click each label, then press **Save supplier** with the form empty. Which form's labels reach their fields and match their messages?

On the left, .NET 10's markup runs live: one label with a typo in its `for`, one wrapping its input. On the right, each is a `<Label For>`. Under each form, a panel reads the rendered markup: which label the browser ties to each input, and what has focus. The left page runs on .NET 11 too, so its wrapped input gets an `id="Input_Email"` that .NET 10 never rendered; the panel marks it, and no label uses it.

1. On the left, click **Company name**. Focus stays on nothing: the panel shows `<label for="supplier-compnay">` matching no id, and the input with no label.
2. On the right, click **Company name**. The cursor lands in the field: `<label for="Input_Company">` names `<input id="Input_Company">`.
3. Press **Save supplier** on both, empty. On the left, the label **Email** sits over "The Contact email field is required." On the right, label and message both say "Contact email".

Explain: Only the right form's labels reach their fields and match their messages. In .NET 11 every input renders an `id` from the field it binds, `Input.Company` becoming `Input_Company`, and `<Label For>` points at that same id, with its text from `[Display]`. The messages already used `[Display]` in .NET 10; now the label does too, so nothing is typed twice to drift.

## How it's built

What's new? `<Label For>` in place of each hand-typed label, and an `id` the input renders for itself.

The `<InputText>` lines are the same as in .NET 10. In .NET 11 every built-in input renders an `id` from the field it binds, dots becoming underscores, whether or not a `Label` points at it; an `id` you type yourself still wins. Both are plain rendered markup, so they behave the same in interactive render modes; these demos use static SSR only because they post a form. The panel under each form is the same small script on both sides.

### .NET 11: a Label for each input
code: Components/Guide/Demos/Labels/NewSupplier.razor

### The model names each field once, for labels and messages
code: Components/Guide/Demos/Labels/NewSupplierForm.cs

### .NET 10: a typed id with a typo, and a wrapped label that drifted
code: Components/Guide/Demos/Labels/NewSupplierNet10.razor

## Good to know

- **An explicit id wins.** Set `id` on an input and it keeps your id, so `<Label For>`'s `for` no longer matches it. Wrap the input instead: `<Label For="…"><InputText … /></Label>` needs no `for` at all.
- **The id comes from the property path.** A nested field is `Input_Address_City`; one in a list is `Input_Contacts[0]_Name`, which needs `[` and `]` escaped in CSS.
- **Inputs render an id now.** Check for CSS, scripts or test selectors that could clash with the new ids.
- **Your own inputs** derived from `InputBase<T>` should render `id="@IdAttributeValue"`, so labels can find them.
