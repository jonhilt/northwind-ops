---
id: tempdata-ssr
title: TempData
group: Pages & navigation
order: 20
hook: "Order saved" after a redirect, shown once.
intro: Show a one-time message after a redirect, like "Order 11078 saved", the way MVC and Razor Pages always could. New in .NET 11. It rides in an encrypted cookie that lasts until it's read.
mode: Static SSR
demo: demo/tempdata/orders
before: demo/tempdata/orders/net10
labels: .NET 10: a flag in the URL | .NET 11: TempData
app: orders
---

## The problem

You save an order and redirect back to the list. How does the list know to say "saved"?

Blazor in .NET 10 had no built-in way to carry a message across a redirect, so apps used one of two workarounds.

### Put a flag in the URL

```razor
@* After saving: NavigateTo("orders?saved=11078") *@
@if (Saved is not null)
{
    <div class="flash">Order @Saved saved</div>
}

@code {
    [SupplyParameterFromQuery]
    public int? Saved { get; set; }
}
```

The flag stays in the address. Refresh, bookmark or share the link and "saved" shows again, for an order nobody just saved. {.bad}

### Write your own cookie

```csharp
// The Identity template's IdentityRedirectManager
Response.Cookies.Append("Status",
    "Order 11078 saved",
    new() { MaxAge = TimeSpan.FromSeconds(5) });

// On the next page: read it, then delete it
message = Request.Cookies["Status"];
Response.Cookies.Delete("Status");
```

Plumbing copied by hand, in plain text anyone can edit, and a redirect slower than 5 seconds loses the message. {.bad}

## See it work

Save an order on each side, then refresh both lists. Which one says "saved" a second time?

On the left, the edit page redirects to the list with `?saved=…` in the address, the .NET 10 way. On the right, it sets TempData. Under each list, **Carried by** says what brought the message.

1. On each side, click an order number, type an internal note and press **Save changes**. Both lists say "Order … saved". The left's **Carried by** shows the address ending `?saved=…`; the right's shows an encrypted TempData cookie, and its address has no flag.
2. Press **⟳** above each demo. The left still says "saved". The right's message has gone, and **Carried by** says "no TempData cookie".
3. On the left, click the link someone could send, `…/net10?saved=99999`: "Order 99999 saved", for an order that doesn't exist. A bookmark or a shared link replays the flag the same way.

Explain: TempData keeps the message in a cookie that the redirect sets, encrypted and signed, so an edited cookie is ignored. The list's `Get` reads it and marks it for deletion, so that response deletes the cookie and a refresh has nothing to show. The address never changes, so there's nothing to share or forge.

## How it's built

What replaces the flag? One property the edit page sets, and one call the list makes.

There's nothing to set up in `Program.cs`: `AddRazorComponents()` already includes TempData.

### The edit page sets the message as it redirects
code: Components/Guide/Demos/TempData/EditOrder.razor

### The list reads it once, and shows it
code: Components/Guide/Demos/TempData/OrderList.razor

## Good to know

- **Write with the attribute, read with `Get`.** `[SupplyParameterFromTempData]` reads the value in and writes the property back when the request ends, so a list that read the message through it would keep it for the next visit too. `ITempData.Get` reads it and lets it go. Or set the property to null after reading it.
- **Simple values only.** Strings, ints, bools, Guids, `DateTime`s and int enums, plus arrays, lists and dictionaries of those. Store an id, not an object: anything else, even a `decimal`, throws when TempData is saved.
- **Static SSR pages only.** An interactive component has no HTTP request, so `ITempData` cascades as null there and `TempData?.Get` gives nothing: read the message on a statically rendered page.
- **An encrypted cookie, by default.** It's a browser-session cookie with no time limit, deleted by the response that reads it, and encrypted and signed with Data Protection, so an edited one is ignored. Set `TempDataProviderType.SessionStorage` in `AddRazorComponents` options to keep it in server session instead, and add `AddDistributedMemoryCache()`, `AddSession()` and `UseSession()`.
