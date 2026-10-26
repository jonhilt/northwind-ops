# Northwind Ops

**Live at [lab.practicaldotnet.io](https://lab.practicaldotnet.io).**

A working back office for a food wholesaler, built with Blazor on **.NET 11** to show line-of-business developers what's new. Each screen uses a .NET 11 feature the way a real app would. Press **X** (or the X-ray button) to outline every .NET 11 feature on the page. Click a badge to see the app's real code, how you'd have done it in .NET 10, and how it works.

## Run it

You need the .NET 11 RC3 SDK (`11.0.100-rc.3.26522.148`, pinned in `global.json`).

```bash
dotnet run --project src/NorthwindOps
dotnet test
```

Or with Docker, which doesn't need anything installed:

```bash
docker build -t northwind-ops .
docker run -p 8080:8080 northwind-ops   # http://localhost:8080
```

The SQLite database is rebuilt with fresh demo data when the app starts and then every hour, so visitors can edit freely.

Every merge to `main` deploys to the live site; see [deploy/README.md](deploy/README.md).

## What's inside

| Screen | .NET 11 features |
| --- | --- |
| Every page | `EnvironmentView` banner, `BasePath` |
| Dashboard | `CacheView` KPI tiles, a live feed with `Virtualize` `AnchorMode.End`, interactive panels wrapping static content (ChildContent across render modes) |
| Orders | QuickGrid sorting and paging in the URL on a static SSR page; edit an order and get a one-time TempData flash |
| Suppliers | Client-side validation in static SSR (plus a custom rule), async validation, `Label` and `DisplayName`, localized validation messages, nested `[ValidatableType]` validation |
| Purchasing | A session-backed wizard with `[SupplyParameterFromSession]` and `RelativeToCurrentUri` links |
| Support | QuickGrid `OnRowClick`, `Virtualize` with variable-height messages, `InitialItemIndex` and `ScrollToItemAsync`, under a strict CSP |

The research behind the feature list is in [docs/research](docs/research/net11-blazor-features.md).

Seed data: customers, suppliers and products come from Microsoft's [Northwind sample](https://github.com/microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs) (MIT). Code highlighting uses [Prism](https://prismjs.com) (MIT).
