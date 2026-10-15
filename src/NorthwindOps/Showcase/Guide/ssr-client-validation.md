---
id: ssr-client-validation
title: Client-side validation
group: Forms
order: 10
hook: Static forms flag mistakes in the browser, with no round trip.
intro: Check a static SSR form in the browser, before it posts, with the attributes you already have. New in .NET 11.
mode: Static SSR
demo: demo/client-validation/suppliers/new
before: demo/client-validation/suppliers/new/net10
labels: .NET 10: checked on the server | .NET 11: checked in the browser
app: suppliers/new
---

## The problem

Someone types a bad email into a static form. Why does it take a round trip to say so?

Static SSR forms arrived in .NET 8, and up to .NET 10 they only validated on the server. Apps picked one of three workarounds.

### Let the server catch it

```razor
<EditForm Model="Input" FormName="new-supplier"
          OnValidSubmit="SaveAsync">
    @* Runs on the server, after the post *@
    <DataAnnotationsValidator />
    <InputText @bind-Value="Input!.Email" />
    <ValidationMessage For="() => Input!.Email" />
</EditForm>
```

Every mistake costs a full post and a re-render, and nobody hears about it until they press Save. {.bad}

### Make the form interactive

```razor
@rendermode InteractiveServer

<EditForm Model="Input" OnValidSubmit="SaveAsync">
    <DataAnnotationsValidator />
    <InputText @bind-Value="Input!.Email" />
    <ValidationMessage For="() => Input!.Email" />
</EditForm>
```

Feedback as soon as they leave a field, but every visitor now holds a circuit open just to hear "this field is required". {.bad}

### Add jQuery Unobtrusive Validation

```razor
<script src="jquery.js"></script>
<script src="jquery.validate.js"></script>
<script src="jquery.validate.unobtrusive.js"></script>

<InputText @bind-Value="Input!.Email"
    data-val="true"
    data-val-required="Email is required."
    data-val-email="Email isn't valid." />
<span data-valmsg-for="Input.Email"></span>
```

Three more libraries, and every rule written twice: once in C#, once by hand in the markup. {.bad}

## See it work

Both forms are static SSR, with the same model and the same validator. Type a bad email and press Tab, then press Save with Company empty. Which form tells you without asking the server?

On the left, the form's browser rules are switched off, so its inputs carry no rules, exactly as a .NET 10 app rendered them. On the right is .NET 11's default. Each side counts the posts that reach the server under its heading.

1. Type `sales@` into **Email** on each side and press Tab. The left shows nothing. The right says "The Email field is not a valid e-mail address." at once, and its **Posts to the server** stays at 0.
2. Press **Save supplier** on each side, with **Company** empty. The left posts: **Posts to the server** goes to 1, and both errors come back from the server. On the right, the post never leaves the browser: both fields are flagged, and the count stays at 0.
3. Fix both fields on each side and save. Both forms post, the server checks the same rules again, and the supplier is added. Count the posts: 2 on the left, 1 on the right, the one that saved.

Explain: In .NET 11, `DataAnnotationsValidator` on a static form also writes the model's rules into the page, as a `<blazor-client-validation-data>` element. `blazor.web.js` reads them, checks each field as you leave it, and stops a submit that has errors. On .NET 10 the inputs carried no rules at all, so only the server could check them.

## How it's built

What do you change to turn it on? Nothing: the form and the model are the same code you'd write on .NET 10.

The new part is behaviour, not code. In .NET 11 every static form with a `DataAnnotationsValidator` gets browser rules, checked by `blazor.web.js`, the script every Blazor Web App already loads. So the highlighted validator line is one you already have. To keep a form server-only, add `DisableClientValidation="true"` (new in .NET 11), as the left-hand form does; `RazorComponentsServiceOptions.DisableClientValidation` turns it off for the whole app. `Enhance` and the hidden **Posts** field are on both sides and play no part in it.

### The model: the same attributes are the rules, for browser and server alike
code: Components/Guide/Demos/ClientValidation/NewSupplierForm.cs

### .NET 11: a plain static form; the validator you already had now writes the rules into the page
code: Components/Guide/Demos/ClientValidation/NewSupplier.razor

### .NET 10's behaviour: the same form, opted out with DisableClientValidation
code: Components/Guide/Demos/ClientValidation/NewSupplierNet10.razor

## Good to know

- **The server still checks every post; the browser check is a convenience.** With JavaScript off, or a button marked `formnovalidate`, the post goes through and the server catches it. Async attributes, like "is this code taken?", only run there.
- **An input inside a wrapper component gets no browser rule, only the server check.** The form writes its rules when it first renders, and an input inside another component, like a `<FormField>`, renders after that. Put the inputs directly in the form, or accept server-only checks for them.
- **A blank optional email passes in the browser but fails on the server.** A static form posts an empty field as `""`, and `[EmailAddress]`, `[Phone]` and `[Url]` reject `""` on the server. Turn `""` into `null` in the property's setter.
- **Your own validation attributes can get browser rules too.** Implement `IClientValidationRuleProvider` on the attribute, then register a matching JS function with `Blazor.formValidation.addValidator`.
