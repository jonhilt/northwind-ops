---
id: supply-parameter-from-session
title: Session state
group: Pages & navigation
order: 21
hook: A draft that survives refresh, Back and leaving the page.
intro: Keep a multi-step form's draft on the server, in session, with one attribute on a property. New in .NET 11. In .NET 10 you could already read and write session by hand; the attribute does that work for you.
mode: Static SSR
demo: demo/session/restock
before: demo/session/restock/net10
labels: .NET 10: hidden fields | .NET 11: [SupplyParameterFromSession]
app: purchasing
---

## The problem

A form takes two steps. Where do you keep what they typed in step 1?

A static SSR page forgets everything between requests, so in .NET 10 apps used one of two workarounds.

### Carry it in hidden fields

```razor
@foreach (var (id, packs) in draft)
{
    <input type="hidden"
           name="Draft[@id]" value="@packs" />
}

@code {
    [SupplyParameterFromForm(FormName = "send")]
    public Dictionary<int, int>? Draft { get; set; }
}
```

The draft only lives in the page. A refresh or a link loses it, and anyone can edit it in DevTools. {.bad}

### Read and write session by hand

```razor
@code {
    [CascadingParameter]
    HttpContext Http { get; set; } = default!;

    void Load() => draft = JsonSerializer
        .Deserialize<Dictionary<string, int>>(
        Http.Session.GetString("draft") ?? "{}")!;

    void Save() => Http.Session.SetString(
        "draft", JsonSerializer.Serialize(draft));
}
```

This works, but every page repeats it, and a handler that forgets `Save()` loses the work with no error. {.bad}

## See it work

Both pages take the same two-step restock order. Fill in step 1 on each, go to the review, then refresh. Which one still has your order?

On the left, step 2 carries the draft in hidden fields, the quick .NET 10 way, and both steps share one URL. On the right, step 1 and step 2 are two pages that bind one property to session. The grey strip at the top shows what the server holds in session.

1. On each side, type a few quantities and press **Review order →**. Both list your order. The left strip says nothing is in session, only hidden fields in the page; the right strip shows `restock-draft` holding your quantities, and the right's address moves on to `…/restock/review`.
2. Press **⟳** on each side's bar, like F5. The left is back to an empty step 1. The right still shows your order.
3. On the right, press **← Change quantities**: your numbers are filled in. Go to the review again and press **Send to supplier**. The strip now says nothing: the key is gone.
4. On the left, review again, then right-click a line, choose Inspect, and change a hidden field's value to `9999`. Press **Send to supplier**: it sends 9999 packs, and a red line says that's over the 99-pack cap step 1 applied. The right has nothing in the page to edit.

Explain: `[SupplyParameterFromSession]` reads the property from this browser's session as the page renders, and writes it back when the request ends. Session data stays on the server; the browser holds only a cookie naming its session. So every later request, a refresh, a link or Back, finds the draft, and DevTools can't change it. Hidden fields exist only in the page that was posted, so a refresh loses them and anyone can edit them. Reading session by hand, the other .NET 10 way, behaves like the right side (its code is the last card under How it's built): what's new is that the attribute does the loading and saving for you.

## How it's built

What's new? One attribute on a property, and plain assignments to it.

Session itself isn't new: it's set up once, as in .NET 10 (first card). The grey strip reads the session by hand (`SessionStrip.razor`), which still works in .NET 11.

### Setup, unchanged from .NET 10: a session store and the middleware
code: Components/Guide/Demos/SessionDraft/SessionSetup.cs

### Step 1 fills the draft, then redirects
code: Components/Guide/Demos/SessionDraft/RestockLines.razor

### Step 2 reads the same key, and clears it when sent
code: Components/Guide/Demos/SessionDraft/RestockReview.razor

### The .NET 10 page on the left: hidden fields, both steps on one URL
code: Components/Guide/Demos/SessionDraft/RestockNet10.razor

### The other .NET 10 way: step 1 again, loading and saving session by hand
code: Components/Guide/Demos/SessionDraft/RestockLinesByHand.razor

## Good to know

- **Simple types, string keys.** Strings, ints, bools, Guids, `DateTime`s and int enums, plus arrays, lists and dictionaries of those. A dictionary counts only if its keys are strings, so product ids go in as `"31"`: a `Dictionary<int, int>` throws, and so does a `decimal` or your own class. For a big draft, keep an id in session and the draft in your database.
- **Static SSR pages only.** Interactive components have no HTTP request, so the property stays `null`.
- **Tied to the browser by a cookie.** The first page that binds a session property issues `.AspNetCore.Session`, an encrypted id. Another browser, or a private window, has its own session. Idle past the timeout (20 minutes by default, 30 here) and the draft is gone.
- **One server's memory.** `AddDistributedMemoryCache` keeps session data in this server's memory, so a restart empties it. Behind a load balancer, use sticky sessions or a shared cache such as Redis.
