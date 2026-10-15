---
id: async-form-validation
title: Async validation
group: Forms
order: 11
hook: "Is this code taken?" as an attribute, with "Checking…" while it runs.
intro: Check a value against your database as part of validation, and let the form wait for the answer before it saves. New in .NET 11.
mode: Interactive Server
demo: demo/async-validation/suppliers/new
before: demo/async-validation/suppliers/new/net10
labels: .NET 10: a field-changed handler | .NET 11: an async attribute
app: suppliers/new/live
---

## The problem

Is this supplier code already taken? Only the database knows. How does the form ask it?

Validation in .NET 10 was synchronous: a validation attribute that blocked on the database deadlocked the circuit. So apps used one of two workarounds.

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

Save doesn't wait for it, so a quick Save goes through with a taken code, and an older check can land its answer on a newer value. {.bad}

## See it work

Both forms check whether a supplier code is taken, with a lookup that takes 1.5 seconds on purpose. Type a taken code and press Save straight away: which form saves it?

On the left, the check runs in a field-changed handler, the .NET 10 way. On the right, it's an async validation attribute. `EXOT-01` belongs to Exotic Liquids.

1. On the left, type a company, type `EXOT-01` into **Supplier code**, and press **Save supplier** at once. Save goes ahead: only the database's unique index stops a duplicate, and "already used" turns up afterwards.
2. Do the same on the right. The button says **Checking…** and waits, then shows "already used". Nothing is saved.
3. On each side, type `EXOT-01`, press Tab, then quickly change it to `ZZZZ-99`. On the left, the old answer lands on the free code. On the right, the newer edit cancels the old check.

Explain: In .NET 11, validation can be async. `EditForm` awaits every check before it calls `OnValidSubmit`, `IsValidationPending` says when one is running, and a newer edit cancels an older check through its `CancellationToken`.

## How it's built

What replaces the handler? An attribute, with the other rules on the model.

The page runs on Interactive Server, so the check starts as you leave the field. On a static form, the same attribute runs when the form posts: it has no browser rule.

### The attribute asks the database, and can be cancelled
code: Components/Guide/Demos/AsyncValidation/UniqueSupplierCodeAttribute.cs

### The model: the async rule sits with the others
code: Components/Guide/Demos/AsyncValidation/NewSupplierForm.cs

### The page shows "Checking…", and saves once every check passes
code: Components/Guide/Demos/AsyncValidation/NewSupplier.razor

### The .NET 10 form: a handler nothing waits for
code: Components/Guide/Demos/AsyncValidation/NewSupplierNet10.razor

## Good to know

- **Two kinds of pending.** `IsValidationPending(() => Input.Code)` is true while that field's check runs. `IsValidationPending()`, with no field, is only true while Save waits for the whole form.
- **Name the field in the result.** Pass `[context.MemberName!]` to `ValidationResult`, as here. Without it, on a model that isn't `[ValidatableType]`, Save's check files its message under no field, and the form silently doesn't save.
- **Override `IsValid` too.** It's abstract, and synchronous. Throw, as here: Blazor only calls `IsValidAsync`, and blocking on the database there would deadlock the circuit.
- **Faulted isn't invalid.** If a check throws, the field gets no message. `IsValidationFaulted()` is true instead, and the form won't submit, so show a banner saying so.
