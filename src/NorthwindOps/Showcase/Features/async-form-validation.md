---
id: async-form-validation
title: Async form validation
badge: Async validation · pending & faulted
release: Preview 5, reshaped in Preview 7
mode: Static SSR + Interactive Server
sources: Features/Suppliers/AsyncChecks.cs, Components/Pages/Suppliers/SupplierEditor.razor, Features/Suppliers/SupplierForm.cs, Features/Suppliers/SupplierDirectory.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/forms/validation-advanced?view=aspnetcore-11.0#asynchronous-validation
summary: Validation attributes can now be async, so "is this supplier code already taken?" is just another attribute. EditForm awaits every check before OnValidSubmit, in every render mode. In an interactive form, EditContext tracks each check while it runs, so you can show "Checking…", disable Save, and tell a failed check apart from an invalid value.
---

## Try it

1. On the static form, fill everything in but use the supplier code **EXOT-01** (Exotic Liquids already has it), then press **Save supplier**. The page takes about 1.5 seconds: the server awaits the deliberately slow lookup before it answers, then shows "Supplier code is already used by another supplier."
2. Open the **live-checking version**. Type **EXOT-01** and press Tab. "Checking…" appears beside the field and Save is disabled until the lookup comes back.
3. Type a VAT number ending in 0, like **DE123456780**: the (simulated) EU VAT register has no record of it. Now try a Greek one, **EL123456789**. That register is "down", so the field says it couldn't check rather than showing an error.
4. Press **Save** with the Greek number on either version. Nothing is saved, and a banner explains why.

## The .NET 10 way

Validation was synchronous. A uniqueness check meant wiring up your own `ValidationMessageStore` and an `async void` event handler, and checking again by hand on submit:

```csharp
editContext.OnFieldChanged += async (_, e) =>   // async void: if the lookup throws, the circuit goes down
{
    if (e.FieldIdentifier.FieldName != nameof(SupplierForm.Code)) return;
    checkingCode = true;
    StateHasChanged();
    messages.Clear(e.FieldIdentifier);
    if (await directory.IsCodeTakenAsync(Supplier.Code!))   // no cancellation: an old answer can win
        messages.Add(e.FieldIdentifier, "That code is taken.");
    checkingCode = false;
    editContext.NotifyValidationStateChanged();
};

async Task SaveAsync()
{
    if (!editContext.Validate()) return;                        // can't wait for a check still running
    if (await directory.IsCodeTakenAsync(Supplier.Code!)) ...  // so do it all again
}
```

None of that worked on a static SSR form, where the only place to run the check was your submit handler.

## How it works

Derive from `AsyncValidationAttribute` (new in .NET 11's `System.ComponentModel.DataAnnotations`) and override `IsValidAsync(value, context, cancellationToken)`. The synchronous `IsValid(value, context)` is abstract too, so you must override it as well: this app's throw `NotSupportedException`. Get services from the `ValidationContext`. `IAsyncValidatableObject` is the async version of `IValidatableObject`, and `Validator` has matching `TryValidateObjectAsync` methods.

- **Submit:** `EditForm` now calls `EditContext.ValidateAsync()` and awaits it before `OnValidSubmit`. With `OnSubmit`, as here, you call `ValidateAsync()` yourself. `Validate()` is `[Obsolete]`, and `AddAsyncValidator` throws during a synchronous `Validate()`.
- **Static SSR:** the POST waits for every check, so there's nothing to show while it runs. The two checks on this form run at the same time, so a save waits about 1.5 seconds, not 2.7.
- **Interactive:** when a field changes, `DataAnnotationsValidator` registers its check with `RegisterAsyncFieldValidator`. A newer edit cancels the check it replaces, and `ValidateAsync` cancels any still running before it validates the whole form.
- **State:** `IsValidationPending()` and `IsValidationFaulted()` have form-wide, `FieldIdentifier` and `() => Model.Field` overloads. Inputs get `pending` or `faulted` CSS classes while a check runs or after it fails.
- **Faulted isn't invalid.** If a check throws, the field is marked faulted, not given a message. `ValidateAsync` returns `false`, sets `IsValidationFaulted()`, and clears the form's other messages too, so show a banner like this form does.
- **Order:** in the `[ValidatableType]` pipeline, a property's async attributes only run once its synchronous ones pass, so a badly formatted code never reaches the database.
- **Async attributes have no browser rule**, even on a form with client-side validation. They always run on the server.
