---
id: validatable-type-stable
title: Nested validation with [ValidatableType], no longer experimental
badge: [ValidatableType] · nested
release: Preview 7
mode: Any render mode
sources: Features/Suppliers/SupplierForm.cs, Program.cs, Components/Pages/Suppliers/SupplierEditor.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/forms/validation?view=aspnetcore-11.0
summary: Mark the form model [ValidatableType], call AddValidation(), and DataAnnotationsValidator validates the whole object graph, nested objects and list items included. In .NET 10 that was an experimental API you had to suppress a warning (ASP0029) to use. In .NET 11 it's a supported part of the framework.
---

## Try it

1. Leave **Street** empty, press **Add contact**, and type `not-an-email` into the new contact's **Email**. Press **Save supplier**. The address and the contact row are flagged along with the top-level fields.
2. Now check that the server agrees. In DevTools, disable JavaScript (Command Palette → "Disable JavaScript") and press **Save supplier** again. The page posts and comes back with the same errors on `Supplier.Address.Street` and `Supplier.Contacts[0].Email`.
3. Fill in the address, remove the contact, and save. Nested objects are validated on the way in, not checked by hand in the save handler.

## The .NET 10 way

The same code worked, but `[ValidatableType]` and `[SkipValidation]` were experimental, so using them was a compile error until you suppressed ASP0029, in each file or with `<NoWarn>` in the project:

```csharp
#pragma warning disable ASP0029 // experimental in .NET 10
[ValidatableType]
public sealed class SupplierForm
{
    public SupplierAddressForm Address { get; set; } = new();
    public List<SupplierContactForm> Contacts { get; set; } = [];
}
#pragma warning restore ASP0029
```

Before that, in .NET 8 and 9, nested validation meant the `ObjectGraphDataAnnotationsValidator` and `[ValidateComplexType]` from a package that never left preview, or validating child objects yourself. Plain `DataAnnotationsValidator` only checks the model's top-level properties.

## How it works

`AddValidation()` is intercepted by a source generator, which builds validation code for every `[ValidatableType]` class and for every type it can reach through their properties. `SupplierAddressForm` and `SupplierContactForm` need no attribute of their own. `DataAnnotationsValidator` asks `ValidationOptions` for the model's type info. When it finds some, it validates the whole graph and puts each message on the right nested field, such as `Supplier.Contacts[0].Email`.

- **Without `AddValidation()`,** or for a model type the generator doesn't know, `DataAnnotationsValidator` falls back to `Validator.TryValidateObject`, which only validates top-level properties.
- **Client-side validation follows the server.** On a static SSR form, nested fields only get browser rules when the server would validate them, which means through a `[ValidatableType]` graph.
- **`[SkipValidation]`** on a property or type leaves it out. It's out of experimental status too.
- **`ValidationOptions.MaxDepth`** (default 32) stops runaway recursion through circular references.
- **Lower-level API changes:** `IValidatableInfo` became `IValidatableTypeInfo`, `IValidatablePropertyInfo` and `IValidatableParameterInfo`, and the public `ValidatableTypeInfo`, `ValidatablePropertyInfo` and `ValidatableParameterInfo` base classes are gone (the generator now emits its own). `ValidateContext` gained `AddValidationError`. Only custom resolvers need to care.
- **Static SSR binding:** when the form posts no contacts, `Contacts` binds as `null`, not as the empty list from its initializer. `SupplierForm` guards the `Contacts` setter for that, and `Address` too.
