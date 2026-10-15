# Northwind Ops

A Blazor showcase of .NET 11 features for line-of-business developers. See README.md.

## Build and test

- The SDK is pinned to .NET 11 RC2 in `global.json`. If `dotnet --list-sdks` doesn't have it, there's a user-local install at `~/.dotnet-sdks/11.0-rc2`: prefix commands with `DOTNET_ROOT=$HOME/.dotnet-sdks/11.0-rc2 PATH=$HOME/.dotnet-sdks/11.0-rc2:$PATH`.
- `dotnet test` runs everything. Check UI changes in a browser (`dotnet run --project src/NorthwindOps`).
- `docker build .` must keep working: it's how the app is deployed.

## How the showcase works

- **Pages default to static SSR.** Only opt into an interactive render mode when the feature being shown needs one, and say which mode a page uses with the `mode-chip` in its header.
- **X-ray.** Wrap the UI that demonstrates a feature in `<XRay For="feature-id">`. Mark the code that implements it with `@* xray:feature-id *@ ... @* /xray:feature-id *@` (Razor) or `// xray:feature-id ... // /xray:feature-id` (C#/JS). The drawer strips the markers and highlights those lines.
- **Each feature has a write-up** in `src/NorthwindOps/Showcase/Features/{id}.md`: front matter (id, title, badge, release, mode, sources, docs, summary), then `## Try it`, `## The .NET 10 way` and `## How it works`. `XRayContentTests` fails if the components, markers and write-ups get out of step.
- `wwwroot/js/xray.js` is plain JS on purpose, so X-ray works on pages with no circuit.
- Feature facts come from `docs/research/net11-blazor-catalogue.json`. Check APIs against the RC2 packages before writing about them: several were renamed between previews.

## The Field Guide

The main way to learn a feature: `/guide` lists them, and `/guide/{id}`, `/guide/{id}/try` and `/guide/{id}/code` step through one. The aim is clarity, not completeness: one idea per sentence, with the full write-up (`/xray/{id}`) holding the rest.

- **An entry** is `Showcase/Guide/{id}.md` (the id must match a write-up in `Showcase/Features/`), plus small demo pages in `Components/Guide/Demos/<Feature>/`, routed under `/demo/<feature>/…` with `@layout DemoLayout`. The guide shows the demo in a frame, so it has a real URL, redirects and refresh.
- **The format** (copy `validatable-type-stable.md`): front matter `id`, `title`, `group`, `order`, `hook` (index card line), `intro`, `mode`, `demo` (start URL, no leading slash), `before` and `labels` (see below), `app` (the screen in the full app, optional). Then:
  - `## The problem`: a question a developer would recognise, then a one-line lead. One `###` per .NET 10 workaround: code of about 10 lines at most and 55 characters wide, then one sentence ending `{.bad}` saying why it hurts.
  - `## See it work`: the reader's prediction, as a question; a one-line lead; 2–4 numbered steps of what to watch for; then `Explain: …`, the answer they check their guess against.
  - `## How it's built`: a question-style summary, a lead, then one `###` caption per code card with a `code: path` line naming a demo file.
  - `## Good to know`: 1–4 lines of `- **Title.** Detail.`, folded away until opened.
- **Show the old way failing, beside the new.** `before:` names a second demo, the same screen without the feature, and `labels: Without X | With X` names the two sides. "See it work" then runs both side by side, so the reader watches the failure rather than reading about it. A before demo may fail outright: an exception on a `/demo/…` page renders `DemoCrash`, a 500 that names the exception in the frame. Where .NET 11 fixed the bug itself, so the old failure can't run, the before demo replays what the .NET 10 repro returned with `<Net10Recording>`, labelled as recorded: its markup where that behaves the same (dead buttons, inline styles under the same CSP), or a Playwright video in `wwwroot/guide/recordings/` where the failure is motion. Entries without `before:` still show one demo; `docs/research/field-guide-audit/scorecard.md` lists which ones to convert, worst first.
- **Check an entry the way a learner meets it:** `node tools/guide-shots/shoot.mjs <outDir> <baseUrl> <id>` screenshots its three steps. A subagent given only those screenshots should be able to say what the problem is, where it fails, and which lines fix it.
- **Demo files are shown whole**, so the reader sees where data comes from and how it's rendered as well as the feature itself. Keep them realistic, short (about 45 lines) and narrow (about 90 characters), and mark the feature's lines with `xray:{id}` markers so they're highlighted.
- **WebAssembly demos:** components that must run in the browser go in `src/NorthwindOps.Client/<Feature>/` and are hosted from a demo page in the server project with `@rendermode="InteractiveWebAssembly"`. Their source is embedded as `Client/<Feature>/…`, so a code card names them `code: Client/<Feature>/File.razor`. The client can't use the server's services or database: give it data as parameters, or an HTTP endpoint.
- **Browser options:** a demo sets its own with `<ConfigureBrowser>`. Don't add `WithBrowserOptions` to `MapRazorComponents`: in RC2 it gives every page one shared options object that `<ConfigureBrowser>` writes into, so one visitor's demo settings would reach every page for everyone. `GuideServerTests` checks this.
- `GuideTests` checks every entry has each part, that its pages and demo render, and that its code cards mark the feature.

## Walkthroughs (the guide's next version)

`/learn/{id}` is one long page per feature, read top to bottom like an article, with the real demos as its figures: the old way running and failing, why, the fix, the same demo with the fix, the code, the .NET 10 way, .NET 11, and Good to know. `/guide` links an entry to its walkthrough once it has one. Copy `Components/Learn/Walkthroughs/NestedValidation.razor`.

- **A walkthrough** is a Razor page at `/learn/{id}` with `@layout LearnLayout`, its id a Field Guide feature. Styles are `wwwroot/css/learn.css`, all under `.learn`.
- **Figures** are numbered and framed. `<DemoFigure Number Title Url Label Side>` runs a `/demo/…` page inline, its `Caption` saying what to do in it and what to watch for. `<CodeFigure Number Path Feature>` shows a real demo file with its `xray:` lines highlighted. A short snippet or a build result goes in a plain `<div class="fig">`.
- **Be exact about .NET 10.** Say up front what's new and what isn't, and check claims against a .NET 10 build.
- `LearnTests` checks every walkthrough renders, its code highlights the feature and its demos load. Prove a walkthrough with a cold read: a subagent given only screenshots of the page.

## Adding a screen

Screens are built in parallel, so keep each one in its own files and touch shared files only where listed.

- **Pages** go in `Components/Pages/<Screen>/`, with screen-only components alongside. Style them with scoped `.razor.css` files. `app.css` holds the shared look (CSS variables, `.card`, `.page-head`, `.mode-chip`, `.pill`, `table.quickgrid`), so reuse those rather than editing it.
- **Services** go in `Features/<Screen>/`.
- **Data:** put new entities in `Data/<Screen>.cs` and configure them with `IEntityTypeConfiguration<T>`, which `NorthwindDb` picks up automatically. Use `db.Set<T>()` rather than adding a `DbSet` property. Seed with an `IDemoDataSeeder` registered in DI. Adding a property to an existing entity is fine if the screen needs it.
- **Program.cs:** only add lines, in a block commented with the screen's name.
- **Leave alone:** `MainLayout.razor` (the nav already links `orders`, `suppliers`, `purchasing` and `support`), `Home.razor` (the dashboard screen's), `app.css` and `README.md`.
- **Tests** go in `tests/NorthwindOps.Tests/<Screen>Tests.cs`.
- **Demo data:** the shared public database resets every hour (`DemoData`), so screens can let visitors save changes.
