---
id: ssr-client-validation
title: Client-side validation
group: Forms
order: 10
hook: Static forms flag mistakes in the browser, with no round trip.
intro: Check a static SSR form in the browser, before it posts, with the attributes you already have.
mode: Static SSR
demo: demo/client-validation/suppliers/new
app: suppliers/new
---

## The problem

Someone types a bad email into a static form. Why does it take a round trip to say so?

In .NET 10, a static SSR form only validated on the server, so apps picked one of three workarounds.

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

Make a few mistakes, then save.

This is a cut-down copy of the New supplier screen, running as static SSR with no circuit. The time at the top only changes when the server renders the page.

1. Open DevTools → Network. Type `sales@` into **Email** and press Tab. The error appears, and no request is made.
2. Press **Save supplier** with **Company** empty. The browser stops the post and flags it. The server's time doesn't move.
3. Fix both fields and save. This time the form posts: the server checks the same rules again, adds the supplier, and the time moves.

## How it's built

Your model's attributes are the rules. The form writes them into the page, and Blazor's script checks each field as you leave it, then as you type while it's wrong.

There's nothing to switch on: every static form with a `DataAnnotationsValidator` gets it. To opt one out, set `DisableClientValidation="true"` on the validator.

### The model: its attributes are the rules, for browser and server alike
code: Components/Guide/Demos/ClientValidation/NewSupplierForm.cs

### The page: a plain static form, which saves only valid posts
code: Components/Guide/Demos/ClientValidation/NewSupplier.razor

## Good to know

- **The server still checks every post.** The browser rules are a convenience. With JavaScript off, or a button marked `formnovalidate`, the post goes through and the server catches it. Async attributes, like "is this code taken?", only run there.
- **Your own attributes can join in.** Implement `IClientValidationRuleProvider` on the attribute, then register a matching JS function with `Blazor.formValidation.addValidator`.
- **Blank isn't null.** A static form posts an empty field as `""`. The browser lets a blank optional email, phone or URL through, but `[EmailAddress]`, `[Phone]` and `[Url]` reject `""` on the server. Turn `""` into `null` in the property's setter.
- **Inputs inside other components miss out.** The form writes its rules when it first renders. An input inside a wrapper component, like a `<FormField>`, renders later and gets no browser rule, though the server still checks it.
