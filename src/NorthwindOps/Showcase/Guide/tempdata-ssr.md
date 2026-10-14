---
id: tempdata-ssr
title: TempData
group: Pages & navigation
order: 20
hook: "Order saved" after a redirect, shown once.
intro: Show a one-time message after a redirect, like "Order 11078 saved".
mode: Static SSR
demo: demo/tempdata/orders
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

The flag stays in the address bar. Refresh, bookmark or share the link and "saved" shows again. {.bad}

### Write your own cookie

```csharp
// Before redirecting: set a short-lived cookie
HttpContext.Response.Cookies.Append("Status",
    "Order 11078 saved",
    new() { MaxAge = TimeSpan.FromSeconds(5) });

// On the next page: read it, then delete it
message = HttpContext.Request.Cookies["Status"];
HttpContext.Response.Cookies.Delete("Status");
```

Plumbing every app copied by hand, in a plain-text cookie anyone can edit. {.bad}

## See it work

Edit an order, then refresh the list.

This is a cut-down copy of the Orders screen, running for real. The message shows once, then it's gone.

1. Click an order number, type an internal note and press **Save changes**.
2. You land back on the list with "Order … saved", and the address bar has no flag in it.
3. Press **⟳** above the demo to refresh. The message has gone: it was read once, so TempData deleted it.

## How it's built

One page writes the message as it redirects. The next page reads it once and shows it.

There's nothing to set up in `Program.cs`: `AddRazorComponents()` already includes TempData.

### The edit page saves, then writes the message
code: Components/Guide/Demos/TempData/EditOrder.razor

### The list reads it once, and shows it
code: Components/Guide/Demos/TempData/OrderList.razor

## Good to know

- **Simple values only.** Strings, ints, bools, Guids, `DateTime`s and int enums, plus arrays, lists and dictionaries of those. Store an id, not an object: anything else, even a `decimal`, throws when TempData is saved.
- **Static SSR pages only.** Interactive components have no HTTP request, so there's no TempData for them to read.
- **Kept in an encrypted cookie.** By default. Set `TempDataProviderType.SessionStorage` in `AddRazorComponents` options to keep it in server session instead, and add `AddDistributedMemoryCache()`, `AddSession()` and `UseSession()`.
- **`[SupplyParameterFromTempData]` writes back.** Whatever the property holds at the end of the request is saved again, so read a message with `TempData.Get` (as the list does), or clear the property after reading it.
