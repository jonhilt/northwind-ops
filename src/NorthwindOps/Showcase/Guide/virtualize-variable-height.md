---
id: virtualize-variable-height
title: Lists with items of any height
group: Grids & lists
order: 41
hook: One-liners and long emails in one virtualized list, without the jumps.
intro: Virtualize a list whose items run from one line to a page, and let them grow in place.
mode: Interactive Server
demo: demo/variable-height/incident
app: support
---

## The problem

A thread mixes one-line log entries with long emails. How do you virtualize it without the list jumping about?

`Virtualize` in .NET 10 sized unseen items from the few it had just rendered, so mixed heights made the scrollbar jump. It also turned off scroll anchoring, so a message growing above you pushed you down. Apps used one of two workarounds.

### Make every item the same height

```razor
<Virtualize Items="messages" ItemSize="120"
            Context="m">
    @* 120px tall, overflow: hidden, in CSS *@
    <article class="preview">
        <b>@m.Author</b> <p>@m.Body</p>
        <button @onclick="() => open = m">
            Read all
        </button>
    </article>
</Virtualize>
<MessageDialog Message="open" />
```

A one-liner gets the same box as an email, and every long message is cut off and read in a dialog. {.bad}

### Load older messages a page at a time

```razor
<button @onclick="() => shown += 50">
    Show 50 older messages
</button>
@foreach (var m in messages.TakeLast(shown))
{
    <ThreadMessage Message="m" />
}
```

Nothing is virtualized: every message you load stays on the page, so a long thread still gets slow. {.bad}

## See it work

Scroll the thread, then expand everything.

This is the Support desk's longest thread, over 400 messages. Only the ones near the viewport are on the page.

1. Drag the scrollbar down, then back up. Log lines and long emails mix, but the scrollbar doesn't jump.
2. Press **Show more** on a long message. It opens in place.
3. Scroll on until no **Show more** is in view, and note the message at the top. Press **Expand all**: it stays put, though the long messages above it have opened. Scroll up to see them.

## How it's built

There's nothing new to write. The same `Virtualize` now measures items as they render.

`ItemSize` is only a first guess now: the demo guesses 80px, near its average. When items above you change height, Virtualize keeps what you're reading still.

### A plain Virtualize, with messages that open and close
code: Components/Guide/Demos/VariableHeight/IncidentThread.razor

## Good to know

- **Give it a scrolling box.** Virtualize scrolls inside the nearest parent with `overflow-y: auto` or `scroll`. Give that box a fixed height, like the demo's 440px.
- **More items render by default.** `OverscanCount` now defaults to 15 either side, up from 3. If your items are expensive to render, set it back to 3. QuickGrid still defaults to 3.
- **Refreshing starts the measuring again.** `RefreshDataAsync()` and changing `ItemSize` both reset the average height it has measured.
