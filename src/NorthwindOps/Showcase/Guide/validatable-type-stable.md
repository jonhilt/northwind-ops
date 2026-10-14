---
id: validatable-type-stable
title: Nested validation
group: Forms
order: 15
hook: Validate the address and every contact, with a supported attribute.
intro: Validate a form's nested objects and list items with [ValidatableType], no longer experimental in .NET 11.
mode: Static SSR
demo: demo/nested-validation/suppliers/new
app: suppliers/new
---

## The problem

Your form has an address and a list of contacts. How do you validate those too?

`DataAnnotationsValidator` only checks the model's own properties. In .NET 10, apps used one of three workarounds.

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

It worked, but an experimental API in production code came with no promise it would survive the next release. {.bad}

### Add the old preview package

```razor
@* Microsoft.AspNetCore.Components
   .DataAnnotations.Validation, preview only *@
<EditForm Model="Input" OnValidSubmit="SaveAsync">
    <ObjectGraphDataAnnotationsValidator />
</EditForm>

@* ...and on every nested property of the model:
   [ValidateComplexType]
   public AddressForm Address { get; set; } *@
```

A package that never left preview, and an attribute on every nested property you remember to mark. {.bad}

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

Only after Save, and with each error mapped back to its field by hand. {.bad}

## See it work

Leave the nested fields wrong, then save.

This is a cut-down copy of the New supplier screen, as a static page. City is on the supplier's address, and the contact is the first item in a list.

1. Press **Save supplier** with the form empty. **City** and both contact fields are flagged, along with **Company**.
2. Type `sales@` into **Contact email** and press Tab. The list item's own `[EmailAddress]` flags it.
3. Fill everything in and save. The server checks the whole graph again, then adds the supplier and its contact.

## How it's built

Mark the form's model `[ValidatableType]`. `DataAnnotationsValidator` then checks everything the model reaches.

`Program.cs` calls `AddValidation()` once for the whole app. In .NET 11, there's no `#pragma` to add.

### The model: one attribute on the root
code: Components/Guide/Demos/NestedValidation/NewSupplierForm.cs

### The page binds the nested fields by their full path
code: Components/Guide/Demos/NestedValidation/NewSupplier.razor

## Good to know

- **Only the root needs the attribute.** A source generator finds `AddressForm` and `ContactForm` through its properties.
- **No `AddValidation()`, no nesting.** Without it, `DataAnnotationsValidator` falls back to checking top-level properties only.
- **`[SkipValidation]` leaves something out.** Put it on a property or a type. It's no longer experimental either.
- **A static form can post no list.** With no contact rows, `Contacts` binds as `null`, not an empty list. Guard the setter, as this model does.
