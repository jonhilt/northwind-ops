---
id: virtualize-variable-height
title: Virtualize: variable-height items and a stable viewport
badge: Virtualize · variable heights
release: Preview 3 (heights), Preview 4 (anchoring)
mode: Interactive Server
sources: Components/Pages/Support/TicketThread.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/components/virtualization?view=aspnetcore-11.0
summary: Virtualize now measures items as they render and keeps the content you're reading still when items above it change height. A thread of one-line log entries and multi-page emails virtualizes properly, and messages can expand in place.
---

## Try it

1. Open the order-sync incident (the ticket with the most messages). It mixes one-line sync log entries with emails several paragraphs long, but only the messages near the viewport are in the DOM.
2. Scroll up and down quickly. The scrollbar doesn't jump around and the list doesn't stutter.
3. Pick a message and keep your eye on it, with a few long messages above it. Press **Expand all**: every long message above it grows, and the one you're reading stays where it was.
4. Click **Show more** on a long message to expand just that one.

## The .NET 10 way

`Virtualize` sized its spacers from an average of whichever items happened to be rendered at that moment, and turned off the browser's scroll anchoring on the scroll container. With items of very different heights the scrollbar jumped as the average changed, and anything above the viewport that changed height pushed what you were reading out of view. So you made every item the same height:

```razor
<Virtualize Items="messages" ItemSize="120" Context="m">
    @* Fixed height and overflow: hidden in CSS, so ItemSize stays true *@
    <article class="message-preview">
        <strong>@m.Author</strong>
        <p>@m.Body</p>
        <button @onclick="() => opened = m">Read full message</button>
    </article>
</Virtualize>

<MessageDialog Message="opened" OnClose="() => opened = null" />
```

Every message, however short, got the same box, and anything long was cut off and opened in a dialog, because expanding it in place would move the list. The other option was to give up on virtualization and page the thread ("Load 50 older messages").

## How it works

```razor
<div class="thread-scroll">   @* overflow-y: auto *@
    <Virtualize Items="Messages" Context="message" ItemSize="96">
        <article class="msg">...</article>
    </Virtualize>
</div>
```

There's no new API. The same `Virtualize` behaves differently:

- **It measures what it renders.** Whenever a spacer comes into view, it measures the items rendered between the spacers, adds their real height to a running total over everything measured so far, and sizes the spacers from that average. `ItemSize` is only the first guess, used until the first measurement.
- **It keeps the viewport stable.** The browser's native scroll anchoring (`overflow-anchor`) is left on, and the two spacers are excluded from being anchors, so the anchor is always a real message. Where native anchoring isn't available (inside a `<table>`, or a browser that doesn't support `overflow-anchor`), a `ResizeObserver` corrects `scrollTop` when items above the viewport change size.
- **`OverscanCount` now defaults to 15**, up from 3, so more items are rendered either side of the viewport and fast scrolling shows fewer gaps. QuickGrid's own `OverscanCount` still defaults to 3. That's up to 24 more items in the DOM than before; set `OverscanCount="3"` if your items are expensive to render.
- **Gotchas:** `RefreshDataAsync()` and changing `ItemSize` both reset the measured average. Virtualize scrolls inside the nearest ancestor with `overflow-y: auto` or `scroll`. Give the list a container with a fixed height, as here, or it uses the whole page.
