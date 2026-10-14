---
id: supply-parameter-from-session
title: [SupplyParameterFromSession] for static SSR
badge: Session · [SupplyParameterFromSession]
release: Preview 5
mode: Static SSR
sources: Components/Pages/Purchasing/PurchaseOrderWizard.razor, Components/Pages/Purchasing/DraftBanner.razor, Program.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#session-data-persistence
summary: Binds a component property to a value in the user's HTTP session. It's read when the page renders and written back at the end of the request, so a multi-step static SSR form can keep its draft on the server with no hidden fields, no query string and no circuit.
---

## Try it

1. Click **New purchase order**, pick a supplier, then fill in a few quantities (or click **Fill in suggested quantities**) and go on to the review.
2. Refresh, or press Back. Everything you entered is still there. The URL has no query string, and the only hidden field in the forms is Blazor's `_handler` (the form's name): the strip under the wizard shows what's actually in session.
3. Go to **Orders**, then back to **Purchasing**. The list shows your draft, and **Resume draft** takes you back to the step you were on.
4. Open DevTools → Application → Cookies. The draft isn't in any of them: `.AspNetCore.Session` is an encrypted session ID, and the draft itself is on the server. (`.AspNetCore.Components.TempData` is the list page's, for its "sent" message.) Open the page in a private window and there's no draft.
5. Send the order. The draft is cleared and the list shows your new purchase order.

## The .NET 10 way

The quick option was hidden fields: every step re-posts everything the earlier steps collected.

```razor
<form method="post" @formname="po-lines" @onsubmit="SaveLines">
    <input type="hidden" name="Draft.SupplierId" value="@Draft.SupplierId" />
    @foreach (var (productId, quantity) in Draft.Lines)
    {
        <input type="hidden" name="Draft.Lines[@productId]" value="@quantity" />
    }
    ...
</form>
```

The draft only exists inside the page you're on. Refresh or press Back and it's gone or stale, leave for another page and it's lost, and anyone can edit it in DevTools before posting.

To keep it on the server you took the cascading `HttpContext` and did the session plumbing yourself:

```razor
@code {
    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

    PurchaseOrderDraft draft = new();

    protected override void OnInitialized()
    {
        var json = HttpContext.Session.GetString("po-draft");
        draft = json is null ? new() : JsonSerializer.Deserialize<PurchaseOrderDraft>(json)!;
    }

    void SaveLines()
    {
        draft.Lines = ...;
        HttpContext.Session.SetString("po-draft", JsonSerializer.Serialize(draft)); // in every handler, before redirecting
        Navigation.NavigateTo("purchasing/new/review");
    }
}
```

That still works in .NET 11, and it's still the way to store a complex object. But every page repeats the load and save code, a handler that forgets to save loses the user's work without any error, and with streaming rendering a write to a brand-new session after the response has started throws ("The session cannot be established after the response has started"), because the session cookie can no longer be sent.

## How it works

```csharp
// Program.cs
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
app.UseSession();

// The component
[SupplyParameterFromSession(Name = "po-draft-lines")]
public Dictionary<string, int>? DraftLines { get; set; }
```

The attribute is in `Microsoft.AspNetCore.Components.Web`. `Name` is the session key; leave it out and the property name is used. Keys are lower-cased.

- **Read once, written back at the end.** The value is read from session when the component first gets its parameters. After that the property is yours: the handlers here just assign to it. When the request finishes, after all rendering including streaming, every bound property is written back to session. Set a property to `null` and its key is removed, which is how sending or discarding the draft clears it.
- **Session has to be set up.** `AddSession` stores data in an `IDistributedCache`, so you need one registered: `AddDistributedMemoryCache` keeps it in this server's memory. Without one, the app can't create the session store and fails when it starts. `UseSession` is middleware, so it has to run before the request reaches Blazor's endpoint. Leave it out and a static SSR page that binds a session property fails with `InvalidOperationException: Session has not been configured for this application or request.`
- **Simple types only.** In RC2, as in RC1, a session property can be `int`, `bool`, `string`, `Guid`, `DateTime`, an `int`-based enum, or a nullable of one of those; or an array, `List<T>`, `HashSet<T>` or `Collection<T>` of them; or a `Dictionary<string, T>`; or an `object[]` of them. Any other property type throws `InvalidOperationException` as soon as the component binds, `decimal`, your own classes and C# union types included. (A property typed `object` or as an interface isn't checked until its value is written back.) That's why this draft is four properties, with the lines as product ID → quantity, and prices are looked up when the page renders. For a bigger draft, keep a `Guid` in session and the draft in your database.
- **Static SSR only.** Interactive components have no `HttpContext`, so the property stays `null`. With Interactive Server a warning is logged; under WebAssembly nothing supplies it at all.
- **One component per key per request.** If two components bind the same key while rendering the same request, it throws. The list's draft banner and the wizard share keys, but they never render together.
- **Binding a property starts a session.** The first page that renders one issues the `.AspNetCore.Session` cookie, even before anything is stored. This app sets `Cookie.IsEssential = true` so a cookie-consent policy can't block it.
- **Streaming SSR.** With `[StreamRendering]` the headers go out before the page finishes. When a session property is first bound, the framework arranges for the session to be established as the headers are sent, so the cookie still arrives in time (fixed in Preview 6). If that binding only happens after streaming has started, the framework logs a warning that session state wasn't persisted. That's true for a visitor who has no session cookie yet: it can't be sent any more, and storing a value fails with "The session cannot be established after the response has started". A visitor who already has the cookie keeps their values despite the warning.
- **Load balancing.** Session data lives in the `IDistributedCache`. With the memory cache that's one server's memory, so behind a load balancer you need sticky sessions or a shared cache such as Redis or SQL Server. The cookie is encrypted with Data Protection, so the servers also need a shared key ring. A restart empties the memory cache and every draft with it.
- **Idle timeout.** Session data is dropped after 20 minutes without a request by default. This app sets `IdleTimeout` to 30 minutes.
