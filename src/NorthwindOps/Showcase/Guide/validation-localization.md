---
id: validation-localization
title: Translated validation messages
group: Forms
order: 12
hook: Translate every [Required] at once, with one key.
intro: Translate validation messages through IStringLocalizer, by naming each message after its rule. New in .NET 11.
mode: Static SSR
demo: demo/validation-localization/suppliers/new?culture=de-DE
before: demo/validation-localization/suppliers/new/net10?culture=de-DE
labels: .NET 10: a resource on every attribute | .NET 11: one localizer
app: suppliers/new?culture=de-DE
---

## The problem

Your form has to speak German. How do you translate "The Company field is required." on every field of every form?

In .NET 10, Blazor's validation never asked an `IStringLocalizer`, so each attribute had to carry its own translation.

### Point every attribute at a resource

```csharp
[Required(
    ErrorMessageResourceType = typeof(Messages),
    ErrorMessageResourceName = "Required")]
[StringLength(40,
    ErrorMessageResourceType = typeof(Messages),
    ErrorMessageResourceName = "TooLong")]
public string? Company { get; set; }
```

Two settings on every attribute of every property, and any attribute you miss stays in English. {.bad}

### Subclass every attribute

```csharp
public class LocalizedRequiredAttribute
    : RequiredAttribute
{
    public LocalizedRequiredAttribute()
    {
        ErrorMessageResourceType = typeof(Messages);
        ErrorMessageResourceName = "Required";
    }
}
```

A copy of each attribute you use, a plain `[Required]` that slips in stays English, and `[EmailAddress]` is sealed, so it can't be subclassed at all. {.bad}

## See it work

Both forms are German, with an empty **Firma** and the email `sales@`. Press save: which one answers entirely in German?

On the left, each attribute points at a resource, the .NET 10 way, and one was missed. On the right, the attributes are plain and one localizer translates them all.

1. Press **Lieferant speichern** on the left. "Firma ist ein Pflichtfeld." is German, but the email's message is English: its `[EmailAddress]` has no resource settings.
2. Press it on the right. Both messages are German, from attributes with no message settings at all.

Explain: In .NET 11, validation asks an `IStringLocalizer` for every message, by rule name: `RequiredAttribute_Error`, `EmailAddressAttribute_Error`. One entry per kind of rule covers every property, so there's no attribute left to miss.

## How it's built

Where does the German come from on the right? One localizer, keyed by rule name.

The key is the attribute's class name plus `_Error`, like `RequiredAttribute_Error`, and `{0}` is the field's display name. `Program.cs` calls `AddLocalization()` and `AddValidation()`, and `ValidationOptions.LocalizerProvider` picks the localizer for each model type. Without it, each type gets its own `.resx` file.

### The localizer: one German message per kind of rule
code: Components/Guide/Demos/ValidationLocalization/SupplierMessages.cs

### Wiring it up: which localizer each model type uses
code: Components/Guide/Demos/ValidationLocalization/ValidationLocalizationSetup.cs

### The .NET 11 model: plain attributes, with no message settings
code: Components/Guide/Demos/ValidationLocalization/NewSupplierForm.cs

### The .NET 10 model: a resource on every attribute, and one missed
code: Components/Guide/Demos/ValidationLocalization/NewSupplierFormNet10.cs

## Good to know

- **More specific keys win.** `NewSupplierForm_Email_EmailAddressAttribute_Error` beats `NewSupplierForm_EmailAddressAttribute_Error`, which beats `EmailAddressAttribute_Error`.
- **No key? The attribute's own English message shows.** So English needs no entries, and a missing German key falls back the same way.
- **Extra values are `{1}`, `{2}`.** `StringLength` passes its maximum as `{1}`. Your own attributes can pass theirs by overriding `FormatMessage`, new in RC2.
- **Labels don't use the localizer.** `Label` and `DisplayName` read `[Display]`. Translate them with `[Display(ResourceType = …)]`, or by hand as this page does.
