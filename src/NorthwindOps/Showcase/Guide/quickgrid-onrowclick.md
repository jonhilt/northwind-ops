---
id: quickgrid-onrowclick
title: Clickable grid rows
group: Grids & lists
order: 31
hook: Click anywhere on a row to open it.
intro: Open a record when someone clicks anywhere on its row, with no extra buttons and no JavaScript. New in .NET 11.
mode: Interactive Server
demo: demo/row-click/tickets
before: demo/row-click/tickets/net10
labels: .NET 10: a button in every row | .NET 11: OnRowClick
app: support
---

## The problem

People click a row to open it. How do you know which row they clicked?

QuickGrid in .NET 10 had no row events, so apps used one of two workarounds.

### A button in every row

```razor
<QuickGrid Items="tickets">
    <PropertyColumn Property="t => t.Subject" />
    <PropertyColumn Property="t => t.Company" />
    <TemplateColumn>
        <button @onclick="() => OpenAsync(context)">
            Open
        </button>
    </TemplateColumn>
</QuickGrid>
```

Only the button opens the ticket. Click anywhere else on the row and nothing happens. {.bad}

### Catch the click in JavaScript

```razor
<QuickGrid Items="tickets"
    RowClass='t => $"ticket-{t.Id}"'>…</QuickGrid>

@code {
    protected override async Task
        OnAfterRenderAsync(bool first)
    {
        if (!first) return;
        self = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("rowClicks", self);
    }
    [JSInvokable]
    public Task OpenAsync(int id) => ...;
}
```

Plus a script to find the clicked row and read its id, and a .NET reference you must remember to dispose. {.bad}

## See it work

Both grids list the same eight tickets. Click a few rows: the subject, the customer, the gap between. Which grid opens them?

On the left, each row has an **Open** button, the .NET 10 way. On the right, the grid has `OnRowClick`. Both count every click on the grid, and every ticket opened.

1. On the left, click a subject or a customer. **Clicks on the grid** goes up, **Tickets opened** doesn't: only the **Open** button opens a ticket.
2. On the right, click anywhere on a row. Each click opens that row's ticket below the grid, and the two counts stay equal.
3. Hover over the right-hand rows: the pointer cursor comes with `OnRowClick`, and the open row stays highlighted.

Explain: `OnRowClick` puts the click handler on each row, and QuickGrid passes your method the clicked row's item. The whole row is the target, so there's nothing to miss and no button to add.

## How it's built

What's new? One parameter on the grid, and a handler that takes the row's item.

The page is static SSR, and renders the list with `@rendermode="InteractiveServer"`, because a click handler needs a circuit.

### .NET 11: OnRowClick, and the handler it calls
code: Components/Guide/Demos/RowClick/TicketList.razor

### .NET 10: a button in every row
code: Components/Guide/Demos/RowClick/TicketListNet10.razor

## Good to know

- **On .NET 10, `OnRowClick` compiles and does nothing.** QuickGrid passes unknown attributes through to the `<table>`, so it renders a dead `onrowclick` attribute with no warning. Check you're on .NET 11.
- **Rows can't take focus.** Keyboard users can't press Enter on a row, so keep a link or button in it as well. The full Support screen makes each subject a link.
- **Buttons inside a row?** Add `@onclick:stopPropagation` to them. Otherwise a click on the button opens the row too.
- **It needs interactivity.** On a static SSR page the rows still render, but clicking them does nothing.
