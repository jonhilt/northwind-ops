---
id: supply-parameter-from-session
title: Session state
group: Pages & navigation
order: 21
hook: A draft that survives refresh, Back and leaving the page.
intro: Keep a multi-step form's draft on the server, in session, with one attribute.
mode: Static SSR
demo: demo/session/restock
app: purchasing
---

## The problem

A form takes two steps. Where do you keep what they typed in step 1?

A static SSR page forgets everything between requests, so in .NET 10 apps used one of two workarounds.

### Carry it in hidden fields

```razor
<form method="post" @formname="send"
      @onsubmit="Send">
    <AntiforgeryToken />
    @foreach (var (id, packs) in Draft)
    {
        <input type="hidden"
               name="Draft[@id]" value="@packs" />
    }
    <button>Send to supplier</button>
</form>
```

The draft only lives in the page. Leave it and it's gone, and anyone can edit it in DevTools. {.bad}

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

The same load and save code on every page, and a handler that forgets to save loses the work with no error. {.bad}

## See it work

Fill in step 1, then wander off and come back.

This is a two-step restock order on static pages, running for real. The draft is kept on the server.

1. Type a few pack quantities and press **Review order →**. The address bar has no query string.
2. Press **← Change quantities**, or **Start over** above the demo. Your quantities are still filled in.
3. Go to the review again and press **Send to supplier**. The key is removed from session, so **← Change quantities** shows empty boxes.

## How it's built

Bind a property to a session key. It's read as the page renders, and saved when the request ends.

Session needs setting up once in `Program.cs`: `AddDistributedMemoryCache()`, `AddSession()` and `app.UseSession()`. This app already has them for the Purchasing screen.

### Step 1 fills the draft, then moves on
code: Components/Guide/Demos/SessionDraft/RestockLines.razor

### Step 2 reads the same key, and clears it when sent
code: Components/Guide/Demos/SessionDraft/RestockReview.razor

## Good to know

- **Simple types only.** Strings, ints, bools, Guids, `DateTime`s and int enums, plus arrays, lists and `Dictionary<string, T>`s of those. Anything else, even a `decimal`, throws. For a big draft, keep an id in session and the draft in your database.
- **Static SSR pages only.** Interactive components have no HTTP request, so the property stays `null`.
- **Null removes the key.** Set the property to `null`, as Send does, and the key is deleted from session.
- **One server's memory.** `AddDistributedMemoryCache` keeps session data in this server's memory. Behind a load balancer, use sticky sessions or a shared cache such as Redis.
