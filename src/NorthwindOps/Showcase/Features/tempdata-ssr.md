---
id: tempdata-ssr
title: TempData for static SSR
badge: TempData · one-time flash
release: Preview 2
mode: Static SSR
sources: Components/Pages/OrdersEdit/OrderDetail.razor, Components/Pages/OrdersEdit/FlashMessage.razor, Components/Pages/Orders.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#temporary-data-persistence
summary: Blazor now has TempData, the Post-Redirect-Get helper MVC and Razor Pages always had. Set a value as you redirect and the next request reads it once. Use it for the "Order 11078 saved" message after a form post, with no hand-rolled cookies and no query-string flags.
---

## Try it

1. On **Orders**, sort by **Total** and go to page 2. Click an order number.
2. Change the status, set a shipped date or write an internal note, then press **Save changes**.
3. You land back on the same page of the same sort, with a green "Order … saved" message.
4. Refresh. The message is gone, because TempData values are deleted once they've been read.
5. Try an invalid change, such as a shipped date on a Pending order. The form shows the error and nothing is saved, so no message is left for the list.
6. In DevTools → Network, look at the save. The POST answers with a 302 that sets the `.AspNetCore.Components.TempData` cookie, and the list's response deletes it. The cookie's value is encrypted.

## The .NET 10 way

Blazor had no TempData, so a message that had to survive a redirect needed somewhere to live. The Identity template in .NET 8–10 wrote its own cookie and read it back in a `StatusMessage` component:

```csharp
// IdentityRedirectManager.cs
public const string StatusCookieName = "Identity.StatusMessage";

static readonly CookieBuilder StatusCookieBuilder = new()
{
    SameSite = SameSiteMode.Strict,
    HttpOnly = true,
    IsEssential = true,
    MaxAge = TimeSpan.FromSeconds(5),   // hope the redirect lands within 5 seconds
};

public void RedirectToWithStatus(string uri, string message, HttpContext context)
{
    context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
    RedirectTo(uri);
}
```

```razor
@* StatusMessage.razor *@
@code {
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
    string? messageFromCookie;

    protected override void OnInitialized()
    {
        messageFromCookie = HttpContext.Request.Cookies[IdentityRedirectManager.StatusCookieName];
        if (messageFromCookie is not null)
        {
            HttpContext.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
    }
}
```

Every app copied this plumbing. The cookie held plain text, so anyone could edit the message, and it held one string, so there was no room for anything else you wanted to pass along.

The quick alternative was a flag in the URL: redirect to `/orders?saved=11078` and show a banner when it's there. But the flag stays in the address bar. Refresh and the message shows again. Bookmark or share the link and it shows again. You end up stripping it out with JavaScript.

## How it works

`AddRazorComponents()` registers TempData, so there's nothing to add to `Program.cs`. Statically rendered components get it in two ways:

- **A cascading `ITempData`.** `[CascadingParameter] ITempData? TempData` gives you a dictionary for the current request. `Get(key)` and the indexer read a value and mark it for deletion. `Peek(key)` reads without marking. `Keep()` and `Keep(key)` keep values for one more request. `FlashMessage.razor` on the Orders list uses `Get`, so the message shows once.
- **`[SupplyParameterFromTempData]`.** It binds one value to a property. The key is the property name unless you set `Name`. When the request ends, whatever the property holds is written to TempData. The order page sets `Flash` and calls `NavigateTo`. During a form post that's a 302, and the TempData cookie goes out with it.

Gotchas:

- **The attribute writes the property back on every request, even when it's null.** If a page reads a message through `[SupplyParameterFromTempData]` and leaves the property set, the message goes back into TempData and shows on every visit. Copy it to a field and set the property to `null`, as the .NET 11 Identity pages do, or read it with `ITempData.Get` as this app does. The write-back is also why just opening an order sets the TempData cookie here: the order page's `Flash` property is saved, as `null`.
- **One binding per key.** Only one component in a request can bind a given key with the attribute. A second one throws.
- **Where it's stored.** By default (`TempDataProviderType.Cookie`), values are serialized to JSON and encrypted with Data Protection. They're stored in a cookie named `.AspNetCore.Components.TempData`, split into chunks if large. The cookie is `HttpOnly` and `SameSite=Lax`, with `IsEssential = false`, so a cookie-consent policy can block it. Change any of that through `RazorComponentsServiceOptions.TempDataCookie`. To keep values on the server instead, set `TempDataProviderType = TempDataProviderType.SessionStorage` in `AddRazorComponents(options => ...)`, and add session: a distributed cache (`AddDistributedMemoryCache()` will do on one server), `AddSession()` and `UseSession()`. Without the cache, session can't start: `AddSession()` alone doesn't register an `IDistributedCache`. Despite the enum's XML comment, that uses ASP.NET Core session on the server, not the browser's `sessionStorage`.
- **Only simple values.** Since RC1, and unchanged in RC2, TempData stores `string`, `int`, `bool`, `Guid`, `DateTime` and int-based enums, plus their nullable forms. It also stores arrays, `List<T>`, `HashSet<T>` and `Collection<T>` of those, and `Dictionary<string, T>`, plus an `object[]` whose items are all supported. Anything else, including `decimal`, your own classes and C# union types, throws when TempData is saved. (RC2's union support is for validation, not storage.) Pass an id and load the rest.
- **Static SSR only.** Interactive components have no HTTP request to read from, so a `[SupplyParameterFromTempData]` property stays `null` there (with a logged warning).
- **Keep navigation non-throwing.** New projects set `<BlazorDisableThrowNavigationException>true</BlazorDisableThrowNavigationException>`, and this app does too. With it off, `NavigateTo` in a form handler throws. With an async handler like this page's, the response then starts before TempData is saved (still the case in RC2), so the redirect goes out without the cookie and a warning is logged. The switch goes into the app's runtimeconfig.json, so an integration-test project needs it too: this repo's test project sets it with `RuntimeHostConfigurationOption`.
- **Streaming rendering and cookies don't mix.** A cookie can't be set once the response has started, so on a `[StreamRendering]` page that has flushed, the cookie provider saves nothing for that request: values written at any point are dropped, values read with `Get` aren't deleted (so a flash would show again), and a warning is logged. Switch to session storage if you need that.
