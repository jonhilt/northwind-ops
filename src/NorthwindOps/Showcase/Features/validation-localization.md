---
id: validation-localization
title: Localized validation messages
badge: Validation localization
release: Preview 7, key conventions in RC1, FormatMessage in RC2
mode: Any render mode
sources: Program.cs, Features/Suppliers/SupplierText.cs, Features/Suppliers/SupplierForm.cs, Features/Suppliers/SupplierCodeAttribute.cs
docs: https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-11.0#localize-validation-messages
summary: With AddValidation() and AddLocalization(), .NET 11's validation looks up every error message in an IStringLocalizer by a naming convention, so "RequiredAttribute_Error" translates every [Required] at once, with no resource settings on each attribute. The same messages reach the browser for client-side validation on static pages.
---

## Try it

1. Click **Deutsch**, then press **Lieferant speichern** with the form empty. Every message is German: "Firmenname ist ein Pflichtfeld." The browser shows them, but the server translated them when it rendered the page.
2. Type `de123` into **USt-IdNr.** and press Tab. That message has a key of its own, `SupplierForm_VatNumber_RegularExpressionAttribute_Error`, so it can explain the format. A pattern on any other property would try `SupplierForm_RegularExpressionAttribute_Error` and then `RegularExpressionAttribute_Error`. This resource file has neither, so it would get the attribute's built-in English message.
3. Type `acme` into **Lieferantencode** and press Tab: "Der Lieferantencode muss wie ABCD-01 aussehen: 4 Großbuchstaben, ein Bindestrich und 2 Ziffern." The 4, the 2 and the example come from the attribute, not the resource file.
4. Fill in the form with the supplier code **EXOT-01** and save. The server's message is German too: "Lieferantencode wird bereits von einem anderen Lieferanten verwendet."
5. Switch back to **English** and repeat. The same keys come from `SupplierText.resx` instead of `SupplierText.de.resx`.

## The .NET 10 way

Each attribute pointed at a resource property of its own:

```csharp
[Required(ErrorMessageResourceType = typeof(SupplierText), ErrorMessageResourceName = nameof(SupplierText.CompanyRequired))]
[StringLength(40, ErrorMessageResourceType = typeof(SupplierText), ErrorMessageResourceName = nameof(SupplierText.CompanyTooLong))]
public string? Company { get; set; }
```

That's two resource settings on every attribute of every property. You could share one string per rule, like `"{0} is required."`, but nothing applied it for you: an attribute you forgot stayed in English. Blazor's validation didn't go through `IStringLocalizer` (MVC's `AddDataAnnotationsLocalization` doesn't apply to it), so a database-backed localizer couldn't help. And static SSR forms didn't validate in the browser anyway.

## How it works

`AddValidation()` turns on .NET 11's validation for `[ValidatableType]` models, and `AddLocalization()` registers an `IStringLocalizerFactory`. When both are present, each failed rule looks up its message template in this order and formats it with the field's display name as `{0}`:

1. the attribute's `ErrorMessage`, used as a key, if you set one
2. `{Type}_{Property}_{Attribute}_Error`, for example `SupplierForm_VatNumber_RegularExpressionAttribute_Error`
3. `{Type}_{Attribute}_Error`, for example `SupplierForm_RequiredAttribute_Error`
4. `{Attribute}_Error`, for example `RequiredAttribute_Error`

If no key matches, you get the attribute's own message. An attribute that sets `ErrorMessageResourceType` keeps its own resource message. The template is filled in by the attribute's `FormatMessage(format, name)`, a new virtual method on `ValidationAttribute` in RC2. Attributes with extra values pass them as `{1}` and `{2}`: `StringLength` passes its maximum, then its minimum. Your own attributes only get `{0}` unless they override it. `SupplierCodeAttribute` does, so its message can say how many letters and digits a code has, and show an example, from the attribute's own `Letters` and `Digits`:

```csharp
public override string FormatMessage(string format, string name) =>
    string.Format(CultureInfo.CurrentCulture, format, name, Letters, Digits, Example);
```

`SupplierCodeAttribute_Error` is then `"{0} must look like {3}: {1} capital letters, a dash and {2} digits."`, and German puts the same values in its own word order. The same override fills in the attribute's built-in message, `"{0} must look like {3}."`, when no key matches.

- **Which resource?** `ValidationOptions.LocalizerProvider` picks the localizer for each model type. By default it's `factory.Create(type)`, one resource per model class. This app sends every type in the Suppliers namespace to one shared `SupplierText` and leaves other screens' types alone.
- **Display names.** A plain `[Display(Name = "Company name")]` is looked up in the same localizer, but only for messages. `Label` and `DisplayName` don't use `IStringLocalizer`: they call `DisplayAttribute.GetName()`. So this screen uses `[Display(ResourceType = typeof(SupplierText))]`, which the labels, the grid headers and the messages all honour.
- **Client-side rules** for static SSR forms are formatted the same way, in the request's culture, when the page renders.
- **Culture:** the supplier pages read `?culture=de-DE` with request localization middleware for `/suppliers` only. The live form runs on a circuit, which doesn't see the page's query string, so it stays in English. An app-wide language choice would normally live in a culture cookie instead.
- **API churn:** `AddValidationLocalization` went in Preview 7 and `MessageKeyProvider` in RC1. RC1 had `IValidationMessageFormatter` for attributes with extra format values. RC2 removed it in favour of overriding `ValidationAttribute.FormatMessage` ([dotnet/aspnetcore#69116](https://github.com/dotnet/aspnetcore/pull/69116)), so code that implements it no longer compiles. Learn still shows `IValidationMessageFormatter`.
