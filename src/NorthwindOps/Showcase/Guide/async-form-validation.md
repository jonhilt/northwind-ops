---
id: async-form-validation
title: Async validation
group: Forms
order: 11
hook: "Is this code taken?" as an attribute, with "Checking…" while it runs.
intro: Check a value against your database as part of validation, and let the form wait for the answer before it saves.
mode: Interactive Server
demo: demo/async-validation/suppliers/new
app: suppliers/new/live
---

## The problem

Is this supplier code already taken? Only the database knows. How does the form ask it?

Validation in .NET 10 was synchronous, so apps used one of two workarounds.

### Check again when they save

```csharp
async Task SaveAsync(EditContext form)
{
    if (await db.Suppliers.AnyAsync(
            s => s.Code == Input.Code))
    {
        messages.Add(() => Input.Code,
            "That code is already used.");
        form.NotifyValidationStateChanged();
        return;
    }
    // ...save
```

The rule lives in the save handler, away from the others, and nobody hears about it until they press Save. {.bad}

### Check in a field-changed handler

```csharp
form.OnFieldChanged += async (_, e) =>
{
    if (e.FieldIdentifier.FieldName != "Code")
        return;
    messages.Clear(e.FieldIdentifier);
    if (await IsTakenAsync(Input.Code))
        messages.Add(e.FieldIdentifier,
            "That code is already used.");
    form.NotifyValidationStateChanged();
};
```

An `async void` handler with no "checking" state and no cancellation, and Save doesn't wait for it, so you check again on submit anyway. {.bad}

## See it work

Try a code that's taken, then one that's free.

This is a cut-down copy of the New supplier screen, running on a circuit. The code lookup takes 1.5 seconds on purpose, like a busy ERP.

1. Type `EXOT-01` into **Supplier code** and press Tab. "Checking…" appears under it while the lookup runs.
2. Then "Supplier code is already used by another supplier." Exotic Liquids has that code.
3. Type a company name and a new code, like `ACME-42` (if someone got there first, change the digits). Press **Save supplier**. The button says "Checking…" and waits for the lookup before it saves.
4. Press **Save supplier** again. This time the code is taken, by the supplier you just saved, and nothing is saved.

## How it's built

The check is an async validation attribute. EditForm awaits it before it saves, and the `EditContext` says when it's running.

The page runs on Interactive Server, so the check starts as you leave the field. On a static form, the same attribute runs when the form posts: it has no browser rule.

### The attribute asks the database, and can be cancelled
code: Components/Guide/Demos/AsyncValidation/UniqueSupplierCodeAttribute.cs

### The model: the async rule sits with the others
code: Components/Guide/Demos/AsyncValidation/NewSupplierForm.cs

### The page shows "Checking…", and saves once every check passes
code: Components/Guide/Demos/AsyncValidation/NewSupplier.razor

## Good to know

- **Two kinds of pending.** `IsValidationPending(() => Input.Code)` is true while that field's check runs. `IsValidationPending()`, with no field, is only true while Save waits for the whole form.
- **Name the field in the result.** Pass `[context.MemberName!]` to `ValidationResult`, as here. Without it, on a model that isn't `[ValidatableType]`, Save's check files its message under no field, and the form silently doesn't save.
- **Override `IsValid` too.** It's abstract. Throw, as here: Blazor only calls `IsValidAsync`.
- **Faulted isn't invalid.** If a check throws, the field gets no message. `IsValidationFaulted()` is true instead, and the form won't submit, so show a banner saying so.
