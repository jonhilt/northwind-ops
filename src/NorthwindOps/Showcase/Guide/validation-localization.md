---
id: validation-localization
title: Translated validation messages
group: Forms
order: 12
hook: Translate every [Required] at once, with one key.
intro: Translate validation messages through IStringLocalizer, by naming each message after its rule.
mode: Static SSR
demo: demo/validation-localization/suppliers/new?culture=de-DE
app: suppliers/new?culture=de-DE
---

## The problem

Your form has to speak German. How do you translate "The Company field is required." on every field of every form?

Blazor's validation in .NET 10 didn't use `IStringLocalizer`, so apps used one of two workarounds.

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

A copy of each attribute you use, a plain `[Required]` that slips in stays English, and `[EmailAddress]` is sealed. {.bad}

## See it work

Make a mistake in German, then in English.

This is a cut-down copy of the New supplier screen, as a static page. The links at the top switch the request's culture.

1. Press **Lieferant speichern** with the form empty. Both messages are German: "Firma ist ein Pflichtfeld."
2. Type `sales@` into **E-Mail** and press Tab. "E-Mail ist keine gültige E-Mail-Adresse." The browser shows it, but the server translated it when it rendered the page.
3. Click **English** and try again. There are no English keys, so each attribute's own message shows.

## How it's built

Name a message after its rule, like `RequiredAttribute_Error`. Validation looks it up in an `IStringLocalizer` for every attribute of that kind.

`Program.cs` already calls `AddValidation()` and `AddLocalization()`. It points this form at `SupplierMessages` with `ValidationOptions.LocalizerProvider`. By default, each model type gets its own `.resx` file.

### The localizer: one German message per kind of rule
code: Components/Guide/Demos/ValidationLocalization/SupplierMessages.cs

### The model: plain attributes, with no message settings
code: Components/Guide/Demos/ValidationLocalization/NewSupplierForm.cs

### The page: a static form, in the request's culture
code: Components/Guide/Demos/ValidationLocalization/NewSupplier.razor

## Good to know

- **More specific keys win.** `NewSupplierForm_Email_EmailAddressAttribute_Error` beats `NewSupplierForm_EmailAddressAttribute_Error`, which beats `EmailAddressAttribute_Error`.
- **Extra values are `{1}`, `{2}`.** `StringLength` passes its maximum as `{1}`. Your own attributes can pass theirs by overriding `FormatMessage`, new in RC2.
- **Labels don't use the localizer.** `Label` and `DisplayName` read `[Display]`. Translate them with `[Display(ResourceType = …)]`, or by hand as this page does.
- **Pick the resource per type.** `LocalizerProvider` gets the model type and the `IStringLocalizerFactory`. The Suppliers screen sends all its form types to one shared `SupplierText.resx`.
