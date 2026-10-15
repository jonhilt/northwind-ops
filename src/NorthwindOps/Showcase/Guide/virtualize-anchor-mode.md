---
id: virtualize-anchor-mode
title: Lists that follow the newest item
group: Grids & lists
order: 40
hook: A live feed that scrolls to new items, unless you're reading.
intro: Keep a chat or live feed on its newest item, without yanking readers away from older ones. New in .NET 11.
mode: Interactive Server
demo: demo/anchor-mode/inbox
before: demo/anchor-mode/inbox/net10
labels: .NET 10: plain Virtualize | .NET 11: AnchorMode.End
app:
---

## The problem

New messages keep arriving at the bottom of a list. How do you keep showing the newest one, but leave someone reading older ones where they are?

`Virtualize` in .NET 10 kept its scroll position when items were added, so new ones landed out of sight. Apps used one of two workarounds.

### Track the bottom with JavaScript

```razor
@code {
    async Task OnMessage(Message message)
    {
        var atBottom = await JS.InvokeAsync<bool>(
            "chat.isAtBottom", scroller);
        messages.Add(message);
        StateHasChanged();
        if (atBottom) await JS.InvokeVoidAsync(
            "chat.scrollToBottom", scroller);
    }
}
```

A JS round trip per message, and with mixed heights the jump could land short of the bottom, after which `isAtBottom` said no and the feed stopped following for good. {.bad}

### Don't virtualize

```razor
<div class="feed" @ref="scroller">
    @foreach (var message in messages)
    {
        <ChatMessage Message="message" />
    }
</div>
@* ...and the same JavaScript to scroll down *@
```

Every message stays on the page, so a busy feed gets slower as the day goes on. {.bad}

## See it work

Both inboxes get a new message every 2.5 seconds. Which one keeps the newest in view, and does it pull you back down when you scroll up to read?

The same messages, some of them long. On the left is the plain `Virtualize` .NET 10 had; on the right, the same list with `AnchorMode`. Under each, the first line says where you are; the second tallies each new message by where you were when it arrived: **At the bottom** (did it follow?) or **Scrolled up** (did it pull you down?).

1. The right opens on the newest message. The left opens at the top, with nearly 300 **newer out of sight**: scroll it all the way down, until it says **at the bottom**.
2. Wait for two or three messages. On the right, **At the bottom** counts every one as **followed**. On the left, it says **0 followed**: the first new message lands below the fold, and **newer out of sight** climbs from there.
3. Scroll up a little on each, even one wheel notch: any distance from the bottom counts. Wait again. On both, **Scrolled up** says **0 pulled you down**, and the message you're reading doesn't move.
4. Scroll back to the very bottom. The right picks up following again. The left is left behind by the next message, every time.

Explain: Without `AnchorMode`, `Virtualize` keeps your scroll position, so a row added at the end lands below the fold, even when you were at the bottom. `AnchorMode="VirtualizeAnchorMode.End"` follows the end only while you're at the bottom (within 2px), and stops as soon as you scroll away, so it never pulls a reader back down. Blazor's own script does the scrolling: no JavaScript of yours, no round trip per message.

## How it's built

What's new? One parameter on `Virtualize`. You add items as before, and it does the scrolling.

`VirtualizeAnchorMode` is a set of flags: `Start` (the default) keeps a newest-first list at its top, `End` follows a list that grows at the bottom, `Start | End` pins both, and `None` turns anchoring off. It follows the end of the nearest parent that scrolls (`overflow-y: auto` or `scroll`), so give the list a box with a fixed height, like the demo's 300px, and render it interactively, because following happens live.

### .NET 11: AnchorMode.End, and a plain Add
code: Components/Guide/Demos/AnchorMode/MessageFeed.razor

### The page: static, with the list interactive
code: Components/Guide/Demos/AnchorMode/SupportInbox.razor

### .NET 10: the same list, without AnchorMode
code: Components/Guide/Demos/AnchorMode/MessageFeedNet10.razor

## Good to know

- **On .NET 10, `AnchorMode` builds, then the page returns a 500.** Razor passes the unknown attribute through, so there's no error or warning, and at runtime: "does not have a property matching the name 'AnchorMode'". Check you're on .NET 11.
- **Write `VirtualizeAnchorMode.End`, not `End`.** A bare `AnchorMode="End"` doesn't compile.
- **Loading rows with `ItemsProvider`? Set `ItemComparer` too.** Compare by id. Otherwise the fresh objects from each query look like rows inserted above, and the list stops following. Analyzer BL0011 warns you. Use RC2 or later: in RC1 the first load could stay at the top.
- **Upgrading? Delete the JavaScript.** In our test on .NET 11, the old `isAtBottom`/`scrollToBottom` workaround stopped following at the first new message. Set `AnchorMode` instead.
