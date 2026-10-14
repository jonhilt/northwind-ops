---
id: virtualize-scroll-to-item
title: Open a list at any item
group: Grids & lists
order: 42
hook: Open a long thread at message #240, or jump there from code.
intro: Open a virtualized list at a chosen item, and scroll to any item from code, even one that isn't rendered yet.
mode: Interactive Server
demo: demo/scroll-to-item/incident?message=240
app: support
---

## The problem

A link points to message #240 of a long thread. How do you open the list right there?

`Virtualize` in .NET 10 always opened at the top and couldn't scroll from code, so apps used one of two workarounds.

### Scroll to a calculated offset

```razor
<div class="thread" @ref="thread">
    <Virtualize Items="messages" ItemSize="120"
                Context="m">…</Virtualize>
</div>

@code {
    protected override async Task
        OnAfterRenderAsync(bool first)
    {
        // Right only if every message is 120px
        var top = (Number - 1) * 120;
        if (first) await JS.InvokeVoidAsync(
            "scrollToOffset", thread, top);
    }
}
```

With messages of different heights you land somewhere near it, and only after the list has shown message 1. {.bad}

### Render them all, then scroll into view

```razor
@foreach (var m in messages)
{
    <article id="msg-@m.Number">…</article>
}

@code {
    protected override async Task
        OnAfterRenderAsync(bool first)
    {
        if (first) await JS.InvokeVoidAsync(
            "scrollToId", $"msg-{Number}");
    }
}
```

It lands exactly, but only because every message is on the page, so a long thread is slow to render and scroll. {.bad}

## See it work

Follow a link to a message, then jump around.

The demo opened a link to message #240 of the Support desk's longest thread. Only the messages near the viewport are on the page.

1. The thread opens at #240, highlighted, not at the top.
2. Type `35` into **Jump to message #** and press **Go**. The list goes straight to #35, which wasn't rendered a moment ago.
3. Press **Latest** to jump to the end. Then press **⟳** above the demo: the link opens at #240 again.

## How it's built

Set `InitialItemIndex` to open at an item. Call `ScrollToItemAsync` to go to one later.

Both take a zero-based index, so message #240 is index 239. Scrolling from code needs the circuit, so the buttons start disabled.

### Opens at the linked message, and jumps on request
code: Components/Guide/Demos/ScrollToItem/LinkedThread.razor

## Good to know

- **`InitialItemIndex` applies once.** Changing it later does nothing. To open a different thread at its own place, give each thread's `Virtualize` a `@key`.
- **Not before the circuit starts.** `ScrollToItemAsync` throws `InvalidOperationException` until the first interactive render. Use `InitialItemIndex` for where the list opens.
- **The last call wins.** A new call cancels one still in flight, and the user scrolling beats both. Pass a `CancellationToken` to cancel one yourself.
- **Renamed in Preview 7.** Preview 6 called them `InitialIndex` and `ScrollToIndexAsync`. QuickGrid has the same pair, for grids with `Virtualize="true"`.
