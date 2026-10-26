# Northwind Ops

A Blazor showcase of .NET 11 features for line-of-business developers. See README.md.

## Build and test

- The SDK is pinned to .NET 11 RC3 in `global.json`. If `dotnet --list-sdks` doesn't have it, there's a user-local install at `~/.dotnet-sdks/11.0-rc3`: prefix commands with `DOTNET_ROOT=$HOME/.dotnet-sdks/11.0-rc3 PATH=$HOME/.dotnet-sdks/11.0-rc3:$PATH`.
- `dotnet test` runs everything. Check UI changes in a browser (`dotnet run --project src/NorthwindOps`).
- `docker build .` must keep working: it's how the app is deployed.

## How the showcase works

- **Pages default to static SSR.** Only opt into an interactive render mode when the feature being shown needs one, and say which mode a page uses with the `mode-chip` in its header.
- **X-ray.** Wrap the UI that demonstrates a feature in `<XRay For="feature-id">`. Mark the code that implements it with `@* xray:feature-id *@ ... @* /xray:feature-id *@` (Razor) or `// xray:feature-id ... // /xray:feature-id` (C#/JS). The drawer strips the markers and highlights those lines.
- **Each feature has a write-up** in `src/NorthwindOps/Showcase/Features/{id}.md`: front matter (id, title, badge, release, mode, sources, docs, summary), then `## Try it`, `## The .NET 10 way` and `## How it works`. `XRayContentTests` fails if the components, markers and write-ups get out of step.
- `wwwroot/js/xray.js` is plain JS on purpose, so X-ray works on pages with no circuit.
- Feature facts come from `docs/research/net11-blazor-catalogue.json`. Check APIs against the RC3 packages before writing about them: several were renamed between previews.

## The Field Guide

The main way to learn a feature, and the site's home page: `/` (also `/guide`) lists them and links to the app's dashboard at `/app`, and each has a walkthrough at `/learn/{id}`, one page read top to bottom like an article, with the real demos as its figures: the .NET 10 way running and failing, why, the fix, the same demo with the fix, the code, the .NET 10 way in detail, .NET 11, and Good to know. The old three-step pages (`/guide/{id}`, `/try`, `/code`) redirect there. The aim is clarity, not completeness: one idea per sentence.

- **An entry** is `Showcase/Guide/{id}.md`, front matter only (the id must match a write-up in `Showcase/Features/`): `id`, `title`, `group`, `order`, `hook` (index card line), `mode`, `demo` (the .NET 11 demo, no leading slash), and `before` and `labels` (the .NET 10 demo, and both demos' labels as `.NET 10: … | .NET 11: …`).
- **A walkthrough** is a Razor page at `/learn/{id}` with `@layout LearnLayout`. Copy `Components/Learn/Walkthroughs/NestedValidation.razor`. Styles are `wwwroot/css/learn.css`, all under `.learn`.
- **Figures** are numbered and framed. `<DemoFigure Number Title Url Label Side>` runs a `/demo/…` page inline (`Side="before"` for .NET 10, `"after"` for .NET 11), its `Caption` saying what to do in it and what to watch for. `<CodeFigure Number Path Feature>` shows a real demo file with its `xray:` lines highlighted. A short snippet or a build result goes in a plain `<div class="fig">`. Where the problem can't be seen in a demo (it happens on the server, say), an animated figure can show it: a `<figure class="fig anim" data-anim>` whose markup is the last frame, with `style="--d:1.2s"` on each element that arrives later (see `ServerInitiatedCircuitPause.razor`, `learn.css` and `learn.js`). Each tells one story, .NET 10 or .NET 11, never both side by side. It rests on its last frame, so make that frame show the outcome, and plays from the start only when the reader presses Play. Demos are stacked, one per figure, so never write "left" or "right".
- **Demos** are small pages in `Components/Guide/Demos/<Feature>/`, routed under `/demo/<feature>/…` with `@layout DemoLayout`, so each has a real URL, redirects and refresh. Keep them realistic, short (about 45 lines) and narrow (about 90 characters), and mark the feature's lines with `xray:{id}` markers.
- **Show the .NET 10 way failing.** A .NET 10 demo may fail outright: an exception on a `/demo/…` page renders `DemoCrash`, a 500 that names the exception in the frame. Where .NET 11 fixed the bug itself, so the old failure can't run, the .NET 10 demo replays what the repro in `docs/research/field-guide-audit/repros/` returned with `<Net10Recording>`, labelled as recorded: its markup where that behaves the same (dead buttons, inline styles under the same CSP), or a Playwright video in `wwwroot/guide/recordings/` where the failure is motion.
- **Be exact about .NET 10.** Say up front what's new and what isn't, and check claims against a .NET 10 build.
- **WebAssembly demos:** components that must run in the browser go in `src/NorthwindOps.Client/<Feature>/` and are hosted from a demo page in the server project with `@rendermode="InteractiveWebAssembly"`. Their source is embedded as `Client/<Feature>/…`, so a code figure names them `Path="Client/<Feature>/File.razor"`. The client can't use the server's services or database: give it data as parameters, or an HTTP endpoint.
- **Browser options:** a demo sets its own with `<ConfigureBrowser>`. Don't add `WithBrowserOptions` to `MapRazorComponents`: in RC2 and RC3 it gives every page one shared options object that `<ConfigureBrowser>` writes into, so one visitor's demo settings would reach every page for everyone. `GuideServerTests` checks this.
- **Check a walkthrough the way a learner meets it:** `node tools/guide-shots/learn-shots.mjs <outDir> <baseUrl> <id>` screenshots it top to bottom. A subagent given only those screenshots should be able to say what the problem is, where it fails, and which lines fix it.
- `GuideTests` checks every entry has a write-up and a walkthrough, its demos run and its old URLs redirect; `LearnTests` checks every walkthrough renders, its code highlights the feature, and it runs a .NET 10 and a .NET 11 demo (only the .NET 10 one may crash on purpose).

## Adding a screen

Screens are built in parallel, so keep each one in its own files and touch shared files only where listed.

- **Pages** go in `Components/Pages/<Screen>/`, with screen-only components alongside. Style them with scoped `.razor.css` files. `app.css` holds the shared look (CSS variables, `.card`, `.page-head`, `.mode-chip`, `.pill`, `table.quickgrid`), so reuse those rather than editing it.
- **Services** go in `Features/<Screen>/`.
- **Data:** put new entities in `Data/<Screen>.cs` and configure them with `IEntityTypeConfiguration<T>`, which `NorthwindDb` picks up automatically. Use `db.Set<T>()` rather than adding a `DbSet` property. Seed with an `IDemoDataSeeder` registered in DI. Adding a property to an existing entity is fine if the screen needs it.
- **Program.cs:** only add lines, in a block commented with the screen's name.
- **Leave alone:** `MainLayout.razor` (the nav already links `orders`, `suppliers`, `purchasing` and `support`), `Home.razor` (the dashboard screen's), `app.css` and `README.md`.
- **Tests** go in `tests/NorthwindOps.Tests/<Screen>Tests.cs`.
- **Demo data:** the shared public database resets every hour (`DemoData`), so screens can let visitors save changes.
