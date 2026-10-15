---
id: virtualize-scroll-to-item
title: Open a list at any item
group: Grids & lists
order: 42
hook: Open a long thread at message #300, or jump there from code.
intro: Open a virtualized list at a chosen item, and scroll to any item from code, even one that isn't rendered yet. New in .NET 11.
mode: Interactive Server
demo: demo/scroll-to-item/incident?message=300
before: demo/scroll-to-item/incident/net10?message=300
labels: .NET 10: scroll to an offset | .NET 11: InitialItemIndex
app: support
---

## The problem

A link points to message #300 of a long thread. How do you open the list right there?

`Virtualize` in .NET 10 always opened at the top, and had no way to scroll to an item: JavaScript could only scroll its box to a pixel offset. Apps used one of two workarounds.

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

`ItemSize` is your guess at each item's height, which `Virtualize` uses to size the space for items it hasn't rendered. Messages aren't all 120px, so you land on a neighbour, with the one you wanted off the top of the box. {.bad}

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

It lands exactly, but only by giving up virtualizing: every message in the thread is on the page. {.bad}

## See it work

Both lists open the same link, to message #300 of a 420-message thread, then jump to #35. Which one shows you the message you asked for?

On the left, the .NET 10 way: offset maths and a line of JavaScript. On the right, `InitialItemIndex` and `ScrollToItemAsync`. The message you asked for is yellow, tagged "▲ end of the message you asked for", and the readout under each list says where it is.

1. Press **⟳** on each side and watch the list open. The left is empty until the circuit starts, then jumps to its offset. Its readout says #300's header is a few hundred pixels above the top, out of sight: the box starts with the end of #301, then #302. The right's first HTML already holds the messages around #300, and #300 is at the top, yellow.
2. Type `35` into **Jump to message #** on each side and press **Go**. The left lands eight messages late: the top of the box is #43, with #35 out of sight above. The right puts #35 at the top.
3. Read **On the page** on each side: about 30 of the 420 messages. Both lists are still virtualized, but only the right lands where you asked.

Explain: The offset maths assumes every message is 120px. The first 34 messages average about 100px, so #35 really starts near 3,480px, and 34 × 120 = 4,080px scrolls about 600px past it, into the #40s. Near #300, `Virtualize` places the messages it hasn't rendered by its own estimate, which doesn't match the guess either. .NET 11's `Virtualize` finds the item itself: it renders the messages around it, then aligns that element to the top, so it's exact whatever the heights.

## How it's built

What's new? A parameter to open at an item, and a method to go to one later.

Both take a zero-based index, so message #300 is index 299. The highlighted lines on the .NET 11 card are the new API; on the .NET 10 card they are the old offset maths, for comparison.

The rest isn't new. The list waits for its messages (`@if`), because `InitialItemIndex` applies once. The buttons stay disabled until the circuit starts, because `ScrollToItemAsync` throws `InvalidOperationException` before then. The yellow highlight is the app's own `target` class.

### New in .NET 11: opens at the linked message, and jumps on request
code: Components/Guide/Demos/ScrollToItem/LinkedThread.razor

### The old .NET 10 workaround: an offset from a guessed height
code: Components/Guide/Demos/ScrollToItem/LinkedThreadNet10.razor

## Good to know

- **`InitialItemIndex` applies once.** Changing it later does nothing. To open a different thread at its own place, give each thread's `Virtualize` a `@key`.
- **`ScrollToItemAsync` throws during prerendering.** It throws `InvalidOperationException` until the first interactive render. Use `InitialItemIndex` for where the list opens.
- **A second `ScrollToItemAsync` cancels the first.** A new call cancels one still in flight, and the user scrolling beats both. Pass a `CancellationToken` to cancel one yourself.
- **On .NET 10, `InitialItemIndex` compiles, but the page fails at runtime with a 500.** Razor passes unknown attributes through, so the build succeeds; rendering then throws because `Virtualize` has no such property. `ScrollToItemAsync` doesn't compile. Preview 6 called them `InitialIndex` and `ScrollToIndexAsync`; QuickGrid has the same pair, for grids with `Virtualize="true"`.
