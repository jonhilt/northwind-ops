---
id: validatable-type-stable
title: Nested validation
group: Forms
order: 15
hook: Validate the address and every contact, with a supported attribute.
intro: Validate a form's nested objects and list items with [ValidatableType], a supported API in .NET 11 rather than an experimental one.
mode: Static SSR
demo: demo/nested-validation/with
before: demo/nested-validation/without
labels: Without [ValidatableType] | With [ValidatableType]
app: suppliers/new
---

## The problem

Your form has an address and a list of contacts. Does `DataAnnotationsValidator` check them?

Only if the model is marked `[ValidatableType]`. Unmarked, it checks the model's own properties and stops, so a bad contact saves without a word. .NET 10 had the mark, but only as an experimental API.

### Use the experimental attribute

```csharp
#pragma warning disable ASP0029 // experimental
[ValidatableType]
public class NewSupplierForm
{
    public AddressForm Address { get; set; }
    public List<ContactForm> Contacts { get; set; }
}
#pragma warning restore ASP0029
```

It worked, but ASP0029 is an error until you suppress it, and an experimental API comes with no promise it will survive the next release. {.bad}

### Validate the children yourself

```csharp
var errors = new List<ValidationResult>();
Validator.TryValidateObject(Input.Address,
    new(Input.Address), errors, true);
foreach (var contact in Input.Contacts)
    Validator.TryValidateObject(contact,
        new(contact), errors, true);
// ...then map each error back to its field
```

The pre-.NET 10 way: no experimental API, but every nested type is yours to walk, and every error yours to map back to its field. {.bad}

## See it work

Both forms start with an empty city and a contact whose email is `sales@`. Which one saves?

The two forms are the same. Only the model behind the right-hand one is marked. Browser validation is off here, so both forms post and the server decides.

1. Press **Save supplier** on the left. It saves, empty city and bad email included.
2. Press it on the right. **City** and both contact emails are flagged, and nothing saves.
3. On the right, press **+ Add contact**, then save. The new empty row is flagged too: every item in the list is checked.

Explain: `DataAnnotationsValidator` checks what the model's validation reaches. Unmarked, that's only `Company`, the one property with rules of its own. `[ValidatableType]` lets the validation source generator walk into `Address` and every item in `Contacts`.

## How it's built

What makes the right-hand form different? One attribute on its model.

`Program.cs` calls `builder.Services.AddValidation()` once, for both forms. In .NET 11, `[ValidatableType]` is a supported API, so there's no `#pragma` to add.

### Two models with the same fields: only the right one is marked
code: Components/Guide/Demos/NestedValidation/SupplierForms.cs

### Each page hands the shared form its model
code: Components/Guide/Demos/NestedValidation/With.razor

### The form binds nested fields by their full path, and its validator checks what the model reaches
code: Components/Guide/Demos/NestedValidation/SupplierEditor.razor

## Good to know

- **Only the root needs the attribute.** The generator finds `AddressForm` and `ContactForm` through its properties.
- **A marked type's base classes are walked too.** Even where the base class is used on its own, unmarked. That's why this demo's two models share an interface rather than a base class.
- **No `AddValidation()`, no nesting.** Without it, `DataAnnotationsValidator` falls back to checking top-level properties only.
- **Browser validation follows the same rule.** A static form only sends the browser rules for fields the server validates, so unmarked nested fields go unchecked there too.
