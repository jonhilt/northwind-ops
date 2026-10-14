---
id: quickgrid-onrowclick
title: Clickable grid rows
group: Grids & lists
order: 31
hook: Click anywhere on a row to open it.
intro: Open a record when someone clicks anywhere on its row, with no extra buttons and no JavaScript.
mode: Interactive Server
demo: demo/row-click/tickets
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
    public Task Open(int id) => OpenAsync(id);
}
```

Plus a script to find the clicked row and read its id, and a .NET reference you must remember to dispose. {.bad}

## See it work

Click a ticket, then another.

These are the Support desk's eight most recently active tickets, in a QuickGrid running on a circuit.

1. Click anywhere on a row: the number, the customer, the space between. Its latest message opens below the grid.
2. Hover over the rows. The pointer cursor comes free with `OnRowClick`.
3. Click another row. The highlight moves with it, because `RowClass` marks the open ticket.

## How it's built

Give the grid an `OnRowClick` handler. QuickGrid calls it with the clicked row's item.

The page is static SSR, and renders the list with `@rendermode="InteractiveServer"`, because a click handler needs a circuit.

### The handler gets the clicked ticket, and loads it
code: Components/Guide/Demos/RowClick/TicketList.razor

## Good to know

- **It needs interactivity.** On a static SSR page the rows still render, but clicking them does nothing.
- **Rows can't take focus.** Keyboard users can't press Enter on a row, so keep a link or button in it as well. The full Support screen makes each subject a link.
- **Buttons inside a row?** Add `@onclick:stopPropagation` to them. Otherwise a click on the button opens the row too.
- **Name the item type.** With a method group, like `OnRowClick="OpenAsync"`, the grid can't infer its type. Add `TGridItem="TicketRow"`, as the demo does.
