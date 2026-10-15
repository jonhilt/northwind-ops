---
id: virtualize-variable-height
title: Lists with items of any height
group: Grids & lists
order: 41
hook: One-liners and long emails in one virtualized list, without the jumps.
intro: Virtualize a list whose items run from one line to a page, and let them grow in place. Fixed in .NET 11.
mode: Interactive Server
demo: demo/variable-height/incident
before: demo/variable-height/incident/net10
labels: .NET 10 · recorded | .NET 11 · live
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

The same thread on each side: one-line log entries mixed with long emails. Scroll, then press **Expand all**. Which list keeps still?

.NET 11 fixed this inside `Virtualize`, so the old list can't run here. The left is a recording of our .NET 10 repro doing exactly that, with the same readout along the bottom as under the live list.

1. Watch the left recording's readout: while it scrolls, the scroll range swings by **13,191px**, so the scrollbar thumb jumps about. Then Expand all pushes the message being read **274px** down the page.
2. On the right, drag the scrollbar down, then back up. The readout's swing stays a small fraction of the left's 13,191px (a few thousand pixels, depending on how you scroll): the thumb barely moves.
3. Scroll to the middle and press **Expand all**. The readout says how far the message at the top moved: 0px, though the long messages above it have opened.

Explain: .NET 10's `Virtualize` sized every unseen item from the dozen it had just rendered, so each new mix of short and long messages resized the whole range. It also turned off the browser's scroll anchoring. .NET 11 keeps a running average of everything it has measured, and keeps what you're reading still when items above it change height.

## How it's built

What changed in your code? Nothing new to write: the same `Virtualize`. If you'd forced every item to one height, delete the fixed height, the `overflow: hidden` and the "Read all" dialog, and let messages be their natural size.

`ItemSize` is only a first guess now: the demo guesses 80px, near its average, and Virtualize measures the real heights as it renders.

### A plain Virtualize, with messages that open and close
code: Components/Guide/Demos/VariableHeight/IncidentThread.razor

## Good to know

- **Give it a scrolling box.** Virtualize scrolls inside the nearest parent with `overflow-y: auto` or `scroll`. Give that box a fixed height, like the demo's 440px.
- **Close, not exact.** The range still drifts a little as it learns, and a list whose heights vary wildly is estimated less well. It's steady, not pixel-perfect.
- **More items render by default.** `OverscanCount` now defaults to 15 either side, up from 3. If your items are expensive to render, set it back to 3. QuickGrid still defaults to 3.
- **Refreshing starts the measuring again.** `RefreshDataAsync()` and changing `ItemSize` both reset the average height it has measured.
