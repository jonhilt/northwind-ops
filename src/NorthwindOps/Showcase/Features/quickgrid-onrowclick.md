---
id: quickgrid-onrowclick
title: QuickGrid OnRowClick
badge: QuickGrid · OnRowClick
release: Preview 1
mode: Interactive Server
sources: Components/Pages/Support/TicketDesk.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid?view=aspnetcore-11.0
summary: QuickGrid can now tell you which row was clicked. OnRowClick is an EventCallback that receives the row's item, so a master/detail screen needs no per-row buttons and no JavaScript.
---

## Try it

1. Click anywhere on a ticket's row: the status pill, the date, the empty space. The thread opens beside the list and the address bar changes to `/support/{id}`.
2. Press **Tab** until a ticket's subject is focused, then press **Enter**. Rows can't take focus, so each subject is also a link.
3. Press **Back**. You return to the previous ticket, because the selection lives in the URL.
4. Hover a row: QuickGrid gives clickable rows a pointer cursor by itself.

## The .NET 10 way

QuickGrid had no row events. You either gave every row a button and accepted that only the button was clickable:

```razor
<QuickGrid Items="tickets">
    <PropertyColumn Property="t => t.Subject" />
    <PropertyColumn Property="t => t.Status" />
    <TemplateColumn>
        <button @onclick="() => Open(context)">Open</button>
    </TemplateColumn>
</QuickGrid>
```

or made the whole row clickable with JavaScript: tag each row with `RowClass`, put the id in the markup, add a click listener to the table, find the closest `<tr>`, and call back into .NET with `DotNetObjectReference` and `[JSInvokable]`. That's a JS file, interop plumbing and a disposal bug waiting to happen, for something users expect from every list screen.

## How it works

```razor
<QuickGrid TGridItem="TicketRow" Items="rows" OnRowClick="OpenTicket" RowClass="RowClass">
```

- `OnRowClick` is an `EventCallback<TGridItem>`. QuickGrid attaches an `@onclick` to each `<tr>` and passes your handler the row's item.
- When it's set, every row gets the `row-clickable` class, which QuickGrid's own stylesheet turns into `cursor: pointer`. If you also use `RowClass`, the two classes are combined (here, `selected` and `has-unread`).
- **It needs an interactive render mode.** It's a C# event handler, so on a static SSR page the rows render but clicking does nothing. That's why this ticket desk is an Interactive Server island on an otherwise static page.
- **Rows aren't focusable.** Learn's advice is to keep a keyboard-accessible link or button in the row as well. Here the subject is a real `<a href>`, which also works for middle-click, "open in new tab" and before the circuit starts.
- **Controls inside a row** should use `@onclick:stopPropagation`, or a click on them also fires `OnRowClick`. That only stops Blazor's own event bubbling, so the browser still follows the subject link; the row handler just doesn't run a second navigation.
- Selecting a row calls `NavigationManager.NavigateTo("support/{id}")`. The page is static, so that's an enhanced navigation: the server renders the new URL and the interactive desk receives the new `TicketId` parameter without restarting its circuit.
- It works the same with `Virtualize="true"`: virtualized rows get the handler too.
