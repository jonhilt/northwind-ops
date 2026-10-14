---
id: virtualize-anchor-mode
title: Lists that follow the newest item
group: Grids & lists
order: 40
hook: A live feed that scrolls to new items, unless you're reading.
intro: Keep a chat or live feed on its newest item, without yanking readers away from older ones.
mode: Interactive Server
demo: demo/anchor-mode/inbox
app:
---

## The problem

New messages keep arriving at the bottom of a list. How do you keep showing the newest one, but leave someone reading older ones where they are?

`Virtualize` in .NET 10 had no idea of following the end of a list, so apps used one of two workarounds.

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

A JS round trip per message, and Virtualize sized unseen rows from an average, so with mixed heights the jump often fell short of the bottom. {.bad}

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

Watch the inbox, then scroll up to read.

A new message arrives every few seconds. Order numbers count up, so you can tell where you are.

1. Wait a few seconds. Each new message appears at the bottom, and the list scrolls to show it.
2. Scroll up a little. New messages still arrive below, but the ones you're reading stay put.
3. Scroll back to the very bottom. The list follows new messages again.

## How it's built

Set `AnchorMode` to `End`, then just add items. Virtualize does the scrolling.

The page is static SSR, and renders the message list with `@rendermode="InteractiveServer"`, because following happens live in the browser.

### The list follows its end while you're at the bottom
code: Components/Guide/Demos/AnchorMode/MessageFeed.razor

## Good to know

- **Write the enum out.** `AnchorMode="VirtualizeAnchorMode.End"`. A bare `AnchorMode="End"` doesn't compile.
- **Give it a scrolling box.** Virtualize follows the end of the nearest parent with `overflow-y: auto` or `scroll`. Give that box a fixed height, like the demo's 300px.
- **Using ItemsProvider? Set ItemComparer.** Compare by id. Otherwise fresh objects from each query look like rows inserted above, and the list stops following. Analyzer BL0011 warns you. A plain `Items` list, like this one, doesn't need it.
- **Fixed in RC2.** With an `ItemsProvider`, RC1's first load could leave the list at the top. RC2 pins it to the bottom once the first items arrive.
