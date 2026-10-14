---
id: ssr-client-validation
title: Client-side validation for static SSR forms
badge: Client validation · static SSR
release: Preview 5, reshaped in Preview 7
mode: Static SSR
sources: Components/Pages/Suppliers/SupplierEditor.razor, Features/Suppliers/SupplierCodeAttribute.cs, wwwroot/js/suppliers.js, Features/Suppliers/SupplierForm.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/forms/validation-client-side?view=aspnetcore-11.0
summary: A static server-rendered EditForm with a DataAnnotationsValidator now validates in the browser, with no circuit and no WebAssembly. The server writes the rules into the page and blazor.web.js enforces them as you type, using the same CSS classes as an interactive form. Your own attributes can add rules too.
---

## Try it

1. Open DevTools → Network. Type `acme` into **Supplier code** and press Tab. "Supplier code must look like ABCD-01…" appears and no request is made. That's the custom rule running in `suppliers.js`.
2. Type an email address without an `@` and press Tab. Errors appear when you leave a field, then update as you type.
3. Press **Save supplier** with the form empty. The browser stops the post and flags every required field, including the address.
4. Press **Save draft**. It has `formnovalidate`, so the browser lets it through. The server saves a draft as long as there's a company name.
5. View the page source and find `<blazor-client-validation-data data-rules="…">` at the end of the form: every rule and message, already in the page's language.

## The .NET 10 way

Static SSR forms only validated on the server. Every mistake cost a full post and a re-render:

```razor
<EditForm Model="Supplier" FormName="new-supplier" OnValidSubmit="SaveAsync">
    <DataAnnotationsValidator />   @* runs on the server, after the POST *@
    <InputText @bind-Value="Supplier!.Code" />
    <ValidationMessage For="() => Supplier!.Code" />
</EditForm>
```

To get instant feedback you had two options, and neither was good. You could make the form interactive, which costs a circuit per visitor just to say "this field is required". Or you could add jQuery and jQuery Unobtrusive Validation and write the `data-val-*` attributes by hand, keeping them in step with your C# attributes yourself.

## How it works

`DataAnnotationsValidator` marks the form for client validation. Each input registers its field as it renders, and the form writes the rules for those fields into a `<blazor-client-validation-data>` element. `blazor.web.js` reads it and validates on `change`, then also on `input` once a field has been wrong or the form has been submitted. It stops a submit that has errors and sets `invalid`, `valid` and `modified` classes, plus `aria-invalid`. The server still validates every post, so the browser rules are only a convenience.

- **Built-in rules:** `Required`, `StringLength`, `MaxLength`, `MinLength`, numeric `Range`, `RegularExpression`, `Compare`, `EmailAddress`, `Url`, `Phone`, `CreditCard` and `FileExtensions`.
- **Your own rules:** implement `IClientValidationRuleProvider` on the attribute and return `ClientValidationRule`s (a name and string parameters). Then register a JS function with `Blazor.formValidation.addValidator(name, fn)`. It gets `{ value, element, params }` and returns `{ success }`, synchronously. `Blazor.formValidation` exists as soon as `blazor.web.js` starts, so this app registers its validator once, from the `afterWebStarted` callback of a JS initializer (`NorthwindOps.lib.module.js`), and it still applies after enhanced navigation.
- **Deriving from a built-in attribute:** an attribute that derives from one, like `RegularExpressionAttribute`, and implements `IClientValidationRuleProvider` sends only its own rules to the browser. Without the interface it keeps the built-in rule.
- **`formnovalidate`** on a submit button skips the browser check, as in plain HTML. This form uses it for **Save draft**, **Add contact** and **Remove**, which post the form without needing it to be valid.
- **Messages and localization:** each rule's message is formatted on the server, in the request's culture. On this page they come from the same resource file as the server-side messages.
- **Only fields the server validates get rules.** For a `[ValidatableType]` model that includes nested objects and list items. Without one, only top-level properties do.
- **Async attributes get no browser rule.** The supplier code's uniqueness check runs on the server when you save.
- **Turning it off:** `<DataAnnotationsValidator DisableClientValidation="true" />` for one form, or `RazorComponentsServiceOptions.DisableClientValidation` for the app. It's on by default.
- **Gotcha: the rules are written once, when the form first renders.** An input that isn't there yet gets no browser rule, though the server still validates it. Two ways to hit this:
  - An input inside another component, such as a `<FormField>` wrapper, renders after the form has written its rules. RC2 doesn't change this. That's why this form's inner X-ray outlines are empty overlays laid over each section rather than wrappers around its fields.
  - On a post, the form renders before your submit handler runs, so a row the handler adds misses out. This form adds and removes contact rows in `OnInitialized` instead, where the posted button value is already bound.
- **Fixed in RC2:** in RC1, `Blazor.formValidation` only existed once a page with a client-validated form had loaded, which could be after an enhanced navigation, so a validator had to be registered again on every `enhancedload`. And an attribute that derived from a built-in one only got the built-in rule, even when it provided its own. RC2 fixes both ([dotnet/aspnetcore#69026](https://github.com/dotnet/aspnetcore/pull/69026)), and this app no longer needs its workaround.
