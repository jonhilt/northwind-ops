---
id: environment-view
title: EnvironmentView
badge: EnvironmentView
release: Preview 1
mode: Any render mode
sources: Components/Layout/MainLayout.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/fundamentals/environments?view=aspnetcore-11.0
summary: A built-in component that renders its content only in the hosting environments you name. Use it for a banner, a debug panel or a "reset demo data" button that must never appear in Production.
---

## Try it

The striped banner at the top of every page is inside an `EnvironmentView` with `Exclude="Production"`. Run the app with `ASPNETCORE_ENVIRONMENT=Production` and the banner doesn't render at all: it isn't just hidden with CSS.

## The .NET 10 way

You injected the host environment and wrote the check yourself:

```razor
@inject IWebHostEnvironment Env

@if (!Env.IsProduction())
{
    <div class="env-banner">@Env.EnvironmentName environment</div>
}
```

That works on the server. A component that also runs in WebAssembly needs `IWebAssemblyHostEnvironment` instead, which is a different type, so shared components ended up with two code paths or a custom abstraction. MVC and Razor Pages have long had an `<environment>` tag helper for this, but Blazor didn't.

## How it works

```razor
<EnvironmentView Include="Development,Staging"> ... </EnvironmentView>
<EnvironmentView Exclude="Production"> ... </EnvironmentView>
```

- `Include` and `Exclude` take comma-separated, case-insensitive environment names, with the same rules as MVC's `<environment>` tag helper. If a name is in both, `Exclude` wins.
- It resolves `IHostEnvironment`, which .NET 11 now also registers in Blazor WebAssembly, so the same component works in every render mode.
- It shipped as `EnvironmentBoundary` in the early previews and was renamed in Preview 6. Preview 7 fixed WebAssembly picking up the wrong environment after enhanced navigation.
