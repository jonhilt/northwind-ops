---
id: virtualize-anchor-mode
title: Virtualize AnchorMode and ItemComparer for live feeds
badge: Virtualize · AnchorMode.End
release: Preview 4
mode: Interactive Server
sources: Components/Pages/Dashboard/LiveOrderFeed.razor, Components/Pages/Home.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/components/virtualization?view=aspnetcore-11.0
summary: Virtualize can now pin a list to its end. With AnchorMode set to End, new items appended to the list scroll into view while you're at the bottom, and stop as soon as you scroll up to read something. ItemComparer tells it which items it has already seen.
---

## Try it

1. Watch the **Live order feed** for a few seconds. A new event arrives every two to five seconds and the list scrolls to show it. The badge says **Following**.
2. Scroll up a little. The badge changes to **Paused**, and as new events arrive it counts them (**Paused · 2 new**) while the rows you're reading stay exactly where they are.
3. Scroll back to the bottom, press <kbd>End</kbd> with the list focused, or click **Jump to latest**. It starts following again.

## The .NET 10 way

`Virtualize` had no idea of following the end of a list. To tail a log or a feed you wrote JavaScript that remembered whether the user was at the bottom, and scrolled back there after every render:

```razor
<div class="feed" @ref="scroller">
    <Virtualize @ref="virtualize" ItemsProvider="LoadRowsAsync" Context="row">
        <FeedRow Row="row" />
    </Virtualize>
</div>

@code {
    async Task OnEventAdded()
    {
        // Ask before rendering: once the new row is in, the user is no longer at the bottom.
        var wasAtBottom = await JS.InvokeAsync<bool>("feed.isAtBottom", scroller);
        await virtualize.RefreshDataAsync();
        StateHasChanged();
        if (wasAtBottom)
        {
            await JS.InvokeVoidAsync("feed.scrollToBottom", scroller);
        }
    }
}
```

It never quite worked. `Virtualize` only renders the rows near the viewport, and in .NET 10 it sized all the others at one estimated height (`ItemSize` at first, then the average of the rows it had just rendered), so the scroll height was a guess. Scrolling to the bottom loaded new rows, the real heights differed, and you landed a little short, so you looped until it settled. Every event also cost an extra JS round trip, and if the user scrolled during that round trip, you pulled them back down.

## How it works

```razor
<Virtualize ItemsProvider="LoadRowsAsync" ItemComparer="SameEvent"
            AnchorMode="VirtualizeAnchorMode.End" Context="row">
    ...
</Virtualize>

@code {
    static readonly IEqualityComparer<FeedRow> SameEvent =
        EqualityComparer<FeedRow>.Create((a, b) => a?.Id == b?.Id, row => row.Id.GetHashCode());
}
```

- **`AnchorMode`** takes the `[Flags]` enum `VirtualizeAnchorMode`: `None`, `Start` (the default) or `End`, and `Start | End` pins both edges. `Start` keeps you at the top when new items arrive at the start, for a newest-first list. `End` scrolls to items appended at the end. `None` leaves the scroll position alone.
- **Following** means being within 2 pixels of the bottom. Scrolling away stops it, and scrolling back (or pressing <kbd>End</kbd>) starts it again. Blazor's own JavaScript tracks that. With an `ItemsProvider`, Virtualize still asks it over interop whether you're following when the count grows, but you don't write that call, and the script checks again just before it scrolls, so it won't pull back down someone who scrolled away in between.
- **`ItemComparer`** matters with an `ItemsProvider`. When a refresh returns more items, Virtualize compares an item it showed before with the item the provider now returns at the same position. The same item means rows were added at the end, so End mode follows them. A different item means rows were inserted above, so it holds your place. The default is `EqualityComparer<T>.Default`: fine for records, but for a class it compares references, and a provider that builds new objects every time (like this one, or any database query) makes every refresh look like an insert at the top. The feed would stop following. The new analyzer **BL0011** warns about an `ItemsProvider` with no `ItemComparer`. With an in-memory `Items` collection you don't need one.
- **Refresh it yourself.** With an `ItemsProvider`, Virtualize doesn't know new events exist until you call `RefreshDataAsync()`, which this feed does as each event arrives.
- **There's no API that says whether it's following.** The Following/Paused badge uses a few lines of this screen's own JavaScript with the same 2-pixel rule.
- **Write the enum out:** `AnchorMode="VirtualizeAnchorMode.End"`. A bare `AnchorMode="End"` doesn't compile. The value was called `Beginning` before Preview 7, so early samples using it don't compile either.
- **Fixed in RC2:** in RC1, with an `ItemsProvider` and `End`, the first load could occasionally leave the list at the top instead of the bottom, particularly on a slow connection. RC2 pins it to the bottom once the first items arrive ([dotnet/aspnetcore#69388](https://github.com/dotnet/aspnetcore/pull/69388)).
- **QuickGrid** has the same `AnchorMode` and `ItemComparer` parameters when `Virtualize="true"`. They were marked experimental (ASP0030) in RC1. RC2 drops that, so you no longer need to suppress the warning ([dotnet/aspnetcore#69354](https://github.com/dotnet/aspnetcore/pull/69354)).
