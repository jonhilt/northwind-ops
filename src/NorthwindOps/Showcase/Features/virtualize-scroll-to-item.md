---
id: virtualize-scroll-to-item
title: Virtualize InitialItemIndex and ScrollToItemAsync
badge: Virtualize · scroll to item
release: Preview 6 (renamed in Preview 7)
mode: Interactive Server
sources: Components/Pages/Support/TicketThread.razor, Features/Support/SupportDesk.cs
docs: https://learn.microsoft.com/aspnet/core/blazor/components/virtualization?view=aspnetcore-11.0#scroll-to-a-specific-item
summary: In .NET 11, Virtualize can open at any item and scroll to any item from code, even one that isn't in the DOM yet. Long threads open at your first unread message, and "Jump to message #" lands exactly on the message.
---

## Try it

1. Open the order-sync incident. It opens at the red "New since you last read" line, hundreds of messages in, not at the top.
2. Type a number in **Jump to message #** and press **Enter**. The list scrolls straight to that message and flashes it, even though it wasn't rendered a moment ago.
3. Press **Latest**, then **First unread**, to jump to either end of what's new.
4. Add `?message=40` to the URL and load it: a link straight to a message. Before the circuit has started, the jump box submits the same way.

## The .NET 10 way

Before .NET 11, `Virtualize` had no way to start anywhere but the top, or to scroll from code. You worked out a pixel offset from `ItemSize` and scrolled the container with JavaScript:

```razor
<div class="thread-scroll" @ref="scroller">
    <Virtualize Items="messages" ItemSize="120">...</Virtualize>
</div>

@code {
    ElementReference scroller;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Only right if every message is exactly 120px tall
            await JS.InvokeVoidAsync("scrollToOffset", scroller, firstUnread * 120);
        }
    }
}
```

With messages of different heights the offset was a guess, so you landed a message or more off: our .NET 10 repro opened `?message=240` on #241, with #240 above the top, and a jump to #35 landed on #46. The list was blank until the circuit started (the prerendered HTML held only an empty spacer), then showed message 1 before jumping. The target wasn't in the DOM until Virtualize caught up with the scroll, so you couldn't scroll it into view or highlight it without polling for it.

Asked for since 2020 in [dotnet/aspnetcore#26943](https://github.com/dotnet/aspnetcore/issues/26943).

## How it works

```razor
<Virtualize @ref="list" Items="Messages" InitialItemIndex="InitialIndex">...</Virtualize>

await list.ScrollToItemAsync(index, cancellationToken);
```

- **`InitialItemIndex`** is the zero-based index to open at. It's applied once, on the first interactive render, once the item count is known; later changes are ignored. Out-of-range values are clamped, and `0` (the default) means no initial scroll. Even the prerendered HTML starts with the window around that item, so the first message never flashes. Because it only applies once, this app gives each ticket's thread its own `Virtualize` with `@key`.
- **`ScrollToItemAsync(int itemIndex, CancellationToken cancellationToken = default)`** loads and renders the window around the item if it isn't there, then aligns the item to the top of the viewport. It works with variable heights because it measures the rendered item rather than calculating an offset.
- **Last call wins:** a new call cancels one still in flight, and a scroll by the user takes precedence over both. The returned task completes when the item is aligned or the call is superseded, and faults with `OperationCanceledException` only if your own token is cancelled. Here the token is cancelled when you switch tickets.
- **It throws `InvalidOperationException` before the first interactive render**, telling you to use `InitialItemIndex` instead. That's why **First unread** and **Latest** are disabled until the circuit starts, and the jump box is an ordinary GET form until then. Call it on the renderer's synchronization context (wrap background work in `InvokeAsync`).
- `SupportDesk.OpeningIndex` decides where to open: the message in the link if there is one, otherwise the first unread, otherwise the latest.
- **Renamed in Preview 7:** Preview 6 called these `InitialIndex` and `ScrollToIndexAsync`. QuickGrid has the same pair, for grids with `Virtualize="true"`.
