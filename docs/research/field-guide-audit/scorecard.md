# Field Guide audit: scorecard

25 Field Guide entries, scored on three questions:

- **Problem**: does a newcomer come away knowing what was wrong before, and is that framing honest?
- **See it fail**: does the entry let you see the old behaviour fail, not just read about it?
- **Fix lines**: do the highlighted lines show the feature, and only the feature?

Each question is scored **strong**, **weak** or **wrong**. Three inputs fed the scores:

1. **Cold read.** A reader with no context saw only the published screenshots and said what they understood and what confused them.
2. **Original issue.** The GitHub issue or PR the feature came from, used to check the entry's framing against the pain people actually reported.
3. **.NET 10 repro.** A small app on SDK 10.0.102 that runs the old behaviour and the entry's workarounds, usually driven by a Playwright probe in `tools/guide-shots/probes/`. Repros are in `repros/<id>/`.

Severity (1-10) is how much the entry needs rework. Every entry has a repro that ran and showed the problem. Entries with no result: none.

## Fixed since

| Entry | Problem | See it fail | Fix lines | How it was checked |
|---|---|---|---|---|
| validatable-type-stable | strong | strong | strong | Cold read of new screenshots: clear / shown / clear. The demos run without and with the attribute side by side. GuideTests post a bad contact to both sides. |
| environment-view | strong | strong | strong | Four cold reads of new screenshots; the last rated it clear / shown / clear once the fix line and the shorthand were highlighted apart. The .NET 10 panel (IWebAssemblyHostEnvironment) returns a real 500, shown beside the working EnvironmentView page. GuideTests check both. |
| server-initiated-circuit-pause | strong | strong | strong | Reframed honestly: pausing worked in .NET 10, and server code could ask through JS interop, but that await never returns. Both ways run side by side; the panel shows the server's request still waiting vs answered `true`. Two cold reads, the last clear / shown / clear. |
| validation-localization | strong | strong | strong | The .NET 10 workaround (a resource on every attribute, one missed) runs beside the .NET 11 localizer: "The E-Mail field is not a valid e-mail address." in a German form vs all German. Wiring now has its own code card. Cold read: clear / shown / mostly clear, then the key rule was stated outright. |
| renderfragment-across-render-modes | strong | strong | strong | .NET 11 fixed the bug, so the left side replays the 500 recorded from the .NET 10 repro (the captain's call for bug-fix entries), beside the same page working live. Cold read: clear / shown / clear. |
| quickgrid-url-paging | strong | strong | strong | .NET 11 changed QuickGrid itself, so the left replays the exact markup the .NET 10 repro returned for `?sort=Price&direction=desc&page=2`: page 1, and buttons with no handler, still inert when clicked. Beside it, the live grid opens on page 2. Cold read: clear / shown / clear. |
| virtualize-csp | strong | strong | strong | The left replays the rows and spacers the .NET 10 repro rendered, under the same `style-src 'self'` policy, so the browser refuses its inline spacer heights live: it stops at Order #19, scrollHeight 760px, with real CSP console errors. Three cold reads; fix lines went vague → clear once the unchanged `<Virtualize>` carried a highlighted before/after of its spacer. |
| virtualize-variable-height | strong | strong | strong | The jump lives in .NET 10's Virtualize script, so the left is a Playwright video of the repro with `scroll-readout.js` along the bottom; the same readout runs live under the .NET 11 list. Same sequence: swing 13,191px vs 1,985px, Expand all moved the message 274px vs 0px. Cold read: clear / shown / clear. |
| wasm-culture-from-server | strong | strong | strong | The switch is app-wide, so the left records two moments from the .NET 10 repro (de-DE prerender, en-US after WebAssembly starts, and workaround 2's forgotten date). The live invoice persists what the server rendered and shows it beside what WebAssembly shows now. The switch moved out of Program.cs into a shown setup file. Two cold reads, the last clear / shown / clear. |
| quickgrid-onrowclick | strong | strong | strong | Live on both sides: the .NET 10 button-per-row grid beside `OnRowClick`, each counting clicks on the grid against tickets opened. Three cell clicks: 3 / 0 on the left, 3 / 3 on the right. The highlight is split into new (OnRowClick, the handler) and not new (RowClass). Cold read: clear / shown / clear. |
| async-form-validation | strong | strong | strong | Live on both sides: the .NET 10 `async void` field-changed handler beside the async attribute, against the same 1.5 s lookup. A taken code saved at once: on .NET 10 validation passes and only the unique index stops the duplicate; on .NET 11 Save waits and nothing is saved. A quick second edit leaves the stale "already used" on .NET 10 only. Cold read: clear / shown / mostly clear. |
| label-and-input-ids | strong | strong | strong | Cold read: problem clear / failure shown / fix lines clear. The revision addresses its confusions. The live .NET 10 side's panel now flags its id="Input_Email" as .NET 11's ("Not new" behaviour of the page host, not the .NET 10 markup). The code comments and the How it's built lead say every InputText renders an id from its field, with or without a Label, and that a typed id wins. The lead also says why the demos use static SSR. The probe (label-and-input-ids-sides.mjs) prints left Focus: nothing with `<label for="supplier-compnay">` matching no id and the Email vs "Contact email" drift, and right Focus: Input_Company with all rows matched. `dotnet test` 281 passed. |
| displayname-component | strong | strong | strong | Cold read rated problem clear / failure shown / fix lines clear. Revised for its confusions: the left grid now shows the untitled column printing Contact, the readout is labelled guide scaffolding, the null model, HeaderTemplate (no string form) and lost bold/sort are explained in the open, and the German's source is named in step 3. The probe prints 2/3 then 0/3 matching headers on the left and 3/3 then 3/3 on the right. dotnet test passed 282/282. |
| supply-parameter-from-session | strong | strong | strong | Cold read rated problem clear, failure shown, fix lines clear. This revision adds a setup card (cookie, timeout, UseSession), the by-hand .NET 10 way as a code card, a string-keys comment, and an on-screen red 'Over step 1's 99-pack cap' line. The probe prints the left sending 9999 packs with that line and 0 hidden inputs on the right, and the refresh still empties the left while restock-draft = {"31":5,"21":7} stays on the right. 283 tests pass. |
| relative-navigation | strong | strong | strong | Cold read rated it problem clear / failure shown / fix lines clear. In the revision, both demos now show a 'You are at' address readout, the folder rule in card 3 is highlighted, the runtime of each frame and the option's name come up front, and the .NET 10 compile chip is reworded. The probe (relative-navigation-compare.mjs) prints left /lines Not Found, right .../12047/lines, and after Orders then 12218 the right shows .../12218/summary. The redirect step now has its own screenshot. 285 tests pass. |
| virtualize-scroll-to-item | strong | strong | strong | The cold read rated it problem clear, failure shown, fix lines clear. After that, the asked-for message got an end-of-message tag and the readout names a cut-off top message, for example "the end of #301, then #302". The .NET 10 bands now cover only the two offset lines and are marked as the old workaround, and the guard band is gone. The steps quote measured values: #43 after Go 35, because 4,080px overshoots #35's real 3,484px. The probe virtualize-scroll-to-item-compare.mjs prints the readouts, scrollTop and the target's real offset on both sides and in the guide frames; 281 tests pass. |
| circuit-auto-pause | strong | strong | strong | Cold read rated the problem clear, the failure shown and the fix lines clear. Revised for its confusions: card 3 highlights only PostAsync and is captioned 'Not new'; the lead gives the install step and says nothing goes in Program.cs; card 2 explains ConfigureBrowser; step 4 explains why paused-for reads 20 s left and 10 s right. The probe re-checked every step ('Not posted · once' vs 'Posted at · once', then 'twice'), and dotnet test passes 280. |
| browser-options | strong | strong | strong | Cold read rated it clear / shown / clear. Its points are now fixed. A measured-waits readout runs on both sides: the probe printed '0 s ×10, then 5 s ×1' on the left and '2 s ×n' on the right, so the countdown can't be misread. The default is worded the same everywhere. A config-way screenshot shows 'Blazor didn't start' with Picked 0 of 4. The config link no longer uses enhanced navigation, which had kept the old Blazor running. WithBrowserOptions and the C#→JSON name mapping are shown, and the .NET 10 card is captioned as for comparison. dotnet test 281/281. |
| wasm-hosted-services | strong | strong | strong | A cold read of the screenshots rated it clear / shown / clear. In response, the recorded .NET 10 rows now name their panel (this bell / the right side's line / workaround 2), and the bells keep checks and new updates as separate counts. Program.cs highlights only AddHostedService and explains the lambda. The probe printed left 0×→1× with a new since time vs right 5×→6× from the same start, the repro re-run on 10.0.102 gave AddHostedService 0/0/0, and dotnet test passes 280. |
| ssr-client-validation | strong | strong | strong | Cold read rated it problem clear, failure shown, fix lines clear, and every confusion point is now fixed. The server clock is gone, so each side shows only its post count. The validator line is labelled as unchanged code with new behaviour, DisableClientValidation is marked new in .NET 11 with the app-wide switch, blazor.web.js is explained, and the Good to know chips explain themselves. The sides probe ran both screenshots in one run and printed posts 0/0 after Tab (only the right flags sales@), 1/0 after Save, and 2/1 after the fixed save; dotnet test passes 281. |
| wasm-env-vars-config | strong | strong | strong | Cold read rated it clear / shown / clear. The revision fixes all six confusions: Program.cs WithBrowserOptions snippet, demo-only table moved out of the client card, honest .NET 10 comparison (script + line + package), OrdersApi.Url explained, full Calling address, breaking change in the intro. The probe printed left Config=staging (failed, 0 orders), right Config=demo/env-config/api/orders calling http://localhost:5436/... (5 orders), and after 'Build's own settings' Variable=none and staging failed. dotnet test 280/280. |
| cacheview | strong | strong | strong | Cold read: problem clear, failure shown, fix lines clear. Revised for its confusions: labelled the since-app-start tallies, dropped the 'Not new' highlighted card, stated CacheView's keying and SSR/GET scope, and cited the repro for OutputCache's antiforgery failure. The probe prints 5 queries ran (IMemoryCache) against 1 (CacheView) for five simultaneous visitors after expiry. |
| tempdata-ssr | strong | strong | strong | Cold read rated it problem clear, failure shown, fix lines clear. This revision acts on its notes. Both sides are live, and the probe prints and screenshots three moments. After a save, both say saved; the left's Carried by shows the full ?saved=12219 address and the right's shows an encrypted cookie. After ⟳, the left still says saved and the right has none (Check your guess opened in the shot). A forged ?saved=99999 link shows "Order 99999 saved" on the left. Playwright checks confirmed three claims: reading through [SupplyParameterFromTempData] shows the message on every visit while Get shows it once; an edited TempData cookie is ignored; the cookie is a session cookie with no MaxAge. Code page is two cards, 2815 px tall (was 4019). |
| virtualize-anchor-mode | strong | strong | strong | Cold read rated it clear / shown / clear. Its confusions are fixed: the readout now tallies each new message as 'At the bottom: N arrived, N followed · Scrolled up: N arrived, N pulled you down', so step 3 and the falling behind in step 4 show on screen. The probe prints left '2 arrived, 0 followed' against right '7 arrived, 7 followed', and '0 pulled you down' on both. There is now a render-mode code card, the enum's values are named, and the Good to know titles explain themselves. The Good to know chips start folded by design. |
| web-worker-template | strong | strong | strong | Cold read rated it problem clear, failure shown, fix lines vague, because it was unclear what's new if WebWorkerClient is 'your source'. Revised: the intro, Explain and code lead now say the new thing is the template-generated client and scripts (request ids, errors, timeouts, start-up), the code comments say 'New in .NET 11: ... C# the template writes into your project', and a new 'Call a misspelled method' button shows the run-time 'Method not found' exception. The .NET 10 card now comes first, and step 1 says ~N instead of a fixed count and explains the shared tab thread. The compare probe printed 0 of ~26 / 30 of ~30 / 27 of ~27 plus the Method-not-found status; the shared-thread probe measured a 2633 ms right-frame stall. dotnet test 280/280. |

## Scorecard

Sorted by severity (highest first), then by number of "wrong" scores, then by number of "weak" scores.

| # | Entry | Problem | See it fail | Fix lines | Kind | Repro shows problem |
|---|---|---|---|---|---|---|
| 1 | validatable-type-stable: Nested validation ([ValidatableType] no longer experimental) | weak | wrong | weak | api-stability | yes |
| 2 | environment-view: Content for some environments only | weak | wrong | weak | ergonomics | yes |
| 3 | server-initiated-circuit-pause: Pause circuits from the server | weak | wrong | weak | ergonomics | yes |
| 4 | validation-localization: Translated validation messages | strong | wrong | weak | behaviour | yes |
| 5 | renderfragment-across-render-modes: Static content in interactive components | strong | wrong | weak | behaviour | yes |
| 6 | quickgrid-url-paging: Grid state in the URL | strong | wrong | weak | behaviour | yes |
| 7 | virtualize-variable-height: Lists with items of any height | strong | wrong | weak | behaviour | yes |
| 8 | virtualize-csp: Virtualize under a strict CSP | strong | wrong | weak | behaviour | yes |
| 9 | wasm-culture-from-server: The same culture in the browser | strong | wrong | weak | behaviour | yes |
| 10 | quickgrid-onrowclick: Clickable grid rows | strong | wrong | weak | ergonomics | yes |
| 11 | async-form-validation: Async validation | strong | wrong | strong | behaviour | yes |
| 12 | label-and-input-ids: Labels and input ids | strong | wrong | strong | behaviour | yes |
| 13 | displayname-component: DisplayName | strong | wrong | strong | ergonomics | yes |
| 14 | supply-parameter-from-session: Session state | strong | wrong | strong | ergonomics | yes |
| 15 | relative-navigation: Relative links | strong | wrong | strong | ergonomics | yes |
| 16 | virtualize-scroll-to-item: Open a list at any item | strong | wrong | strong | behaviour | yes |
| 17 | circuit-auto-pause: Pause hidden tabs (AutoPause) | strong | wrong | strong | ergonomics | yes |
| 18 | browser-options: Reconnection settings in C# | strong | wrong | strong | ergonomics | yes |
| 19 | wasm-hosted-services: Background services in the browser | strong | wrong | strong | ergonomics | yes |
| 20 | ssr-client-validation: Client-side validation (static SSR forms) | strong | weak | weak | behaviour | yes |
| 21 | wasm-env-vars-config: Server settings for WebAssembly | weak | weak | strong | behaviour | yes |
| 22 | cacheview: Caching part of a page | strong | weak | strong | ergonomics | yes |
| 23 | tempdata-ssr: TempData | strong | weak | strong | ergonomics | yes |
| 24 | virtualize-anchor-mode: Lists that follow the newest item | strong | weak | strong | behaviour | yes |
| 25 | web-worker-template: .NET in a Web Worker | strong | weak | strong | ergonomics | yes |

Totals: Problem: 21 strong, 4 weak. See it fail: 19 wrong, 6 weak, no strong. Fix lines: 14 strong, 11 weak. Every cold read rated the failure "described-only".

## API stability, not behaviour

Only one entry's real change is an experimental API becoming supported:

- **validatable-type-stable.** Nested-graph validation with DataAnnotationsValidator + AddValidation() already worked in .NET 10. The change in .NET 11 is that the public [ValidatableType] and [SkipValidation] attributes are now supported rather than experimental. You no longer have to suppress error ASP0029, and the attribute no longer clashes (CS0104) with the SDK's hidden embedded copy. The entry should teach "without the root marked, nested errors silently pass" as the concept, and present .NET 11 as "the opt-in is now a stable, public API".

Three more entries are close to this. Their capability existed in .NET 10, and .NET 11 adds a supported API or tooling for it, but no behaviour changed: server-initiated-circuit-pause (JS interop to `Blazor.pauseCircuit` already worked), supply-parameter-from-session (manual `ISession` + JSON) and web-worker-template (the .NET 10 docs article). They are classed as ergonomics.

## Patterns

- **The failure is never on screen.** No entry scores strong on "see it fail", and every cold reader rated the failure "described-only". The demos run only the .NET 11 path, and the old behaviour exists only as red text under code. The repros show the failure is usually easy to show live: an HTTP 500, a dead button, a duplicate save, an English message in a German form.
- **Screenshots show the idle starting state.** Empty forms, page 1 of a grid, "0 updates", "not paused yet". Even the fixed behaviour isn't visible without clicking.
- **.NET 11 is rarely named.** Most entries say ".NET 10" for the old way and never say the feature is new in .NET 11, so readers have to infer the version.
- **Highlights mix the feature with plumbing or unchanged code.** Examples: `Enhance`, `RowClass`, `TGridItem`, null-guard setters, reconnect-UI ids, unchanged `<Virtualize>`, `@inject IHostEnvironment`. The real switch (Program.cs, the rendered HTML) is often not shown at all.
- **Key rules are folded away in "Good to know".** Requirements and gotchas (needs prerendering, needs a scroll box, it's a snapshot, off by default, don't mix WithBrowserOptions and ConfigureBrowser) are hidden in collapsed chips that a screenshot reader can't open.
- **The .NET 10 gap is sometimes overstated, or the real workaround is left out.** The entries skip the workaround people actually used: JS interop pause, `AddEnvironmentVariables()`, manual session, the docs-article worker, the plain DataAnnotationsValidator. Separately, .NET 11 markup typed on .NET 10 often compiles silently and then does nothing or returns a 500 at run time (OnRowClick, AnchorMode, InitialItemIndex, CacheView, DisplayName, Label, RelativeToCurrentUri), and no entry warns about it.

---

## Entries, worst first

### 1. validatable-type-stable: Nested validation ([ValidatableType] no longer experimental)

**Verdict:** Problem weak · See it fail wrong · Fix lines weak · api-stability · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/65161 (fix via API proposal #67544, PR #67634). Reporter: the attribute "surfaces the ASP0029 warning, which makes it difficult to recommend this approach despite it being functionally correct."

**Repro:** Without [ValidatableType], a POST with an empty City and contact email `sales@` reached OnValidSubmit and saved (`SAVED company=Acme city='' contactEmail='sales@'`). With the root marked, all three nested errors appeared. Without AddValidation(), the form saved silently again. A bare [ValidatableType] builds clean, because it binds to the SDK's embedded attribute. The public attribute fails with **error** ASP0029. Adding `using Microsoft.Extensions.Validation;` fails with CS0104. On the live demo, Save is blocked in the browser with 0 POSTs.

**Inaccurate claims:**
- "In .NET 10, apps used one of three workarounds": [ValidatableType] + AddValidation() was the .NET 10 feature itself. The other two workarounds are .NET 8/9 history.
- Workaround 1 implies the #pragma was needed. A bare attribute built with 0 warnings.
- ASP0029 is a compile error, not a warning.
- The snippet with the `using` fails with CS0104, not ASP0029.
- "The server checks the whole graph again": the invalid Save never posts, because the browser blocks it with .NET 11 client validation, a separate feature.
- Mode is "Static SSR" in the guide and "Any render mode" in the write-up.

**Fix plan:**
- Reframe: nested validation worked in .NET 10. .NET 11 makes the opt-in attribute supported (no ASP0029, no CS0104). Move the other workarounds to a one-line .NET 8/9 note.
- Side-by-side demo: an unmarked model, run server-side only, saves bad data, while the marked model flags City and the contact fields.
- Render the nested shape (Address fieldset, Contacts rows) with the bound field names visible.
- Rewrite "See it work" as predict/watch/explain.
- Split out client validation, or disable it for this demo.
- Narrow the highlights to the `using` + [ValidatableType] + `AddValidation()`, and move the binding plumbing to Good to know.
- Add a .NET 10 vs .NET 11 diff of the model file. Fix the run-together heading.

### 2. environment-view: Content for some environments only (EnvironmentView)

**Verdict:** Problem weak · See it fail wrong · Fix lines weak · ergonomics · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/63689 (PR #64616, renamed in #67369). danroth27: "tricky to do correctly for components that run on WebAssembly and are prerendered."

**Repro:** `IHostEnvironment` in the client project fails with CS0234 until a package is added. `IWebAssemblyHostEnvironment` has `Environment`, not `EnvironmentName` (CS1061), and its server prerender returns **HTTP 500** ("no registered service"). `IHostEnvironment` prerenders fine, then throws when WebAssembly starts: the error UI is shown and the component never becomes interactive.

**Inaccurate claims:**
- `IWebHostEnvironment` "can't run on WebAssembly": in fact it doesn't compile in the client project.
- The WebAssembly service is presented as "a different service". In a prerendered app it returns a 500, which is the core pain in the issue.
- The shown `@inject` and `@using` look required. Neither is needed, because EnvironmentView is in Microsoft.AspNetCore.Components.Web.
- Step 3's "never rendered" claim can't be checked in place.

**Fix plan:**
- Reframe "The problem" as three cards, one per real .NET 10 failure, quoting the repro errors.
- Add a readout of what the server and WebAssembly each report, next to the Include/Exclude values.
- Add an environment switcher (Demo/Production) with the emitted HTML, plus a captured .NET 10 500.
- Rewrite "See it work" as predict/watch/explain.
- Show the minimum use first, with no inject and no using, and say it's new in .NET 11.
- Bring "Not a secret keeper" into the body. Explain RendererInfo.Name.
- Fix the write-up's origin links.

### 3. server-initiated-circuit-pause: Pause circuits from the server

**Verdict:** Problem weak · See it fail wrong · Fix lines weak · ergonomics · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/62327 (design #66244, API #66798, PR #66265). javiercn: "Our current APIs need to be triggered from the browser."

**Repro:** A restart under a half-filled tab gives "Rejoin failed", a reload and an empty form. A pause started by the browser survives a restart in .NET 10, and the dialog still says "paused by the server". Server code calling `JS.InvokeVoidAsync("Blazor.pauseCircuit")` also pauses the tab and restores it; the awaited call never returns. `RequestCircuitPauseAsync` fails with CS1061 on .NET 10.

**Inaccurate claims:**
- "Only the browser could pause a circuit" / "the server had no way to ask": server code could, through JS interop.
- The workarounds shown leave out the real one (JS interop plus blocking ApplicationStopping).
- The "paused by the server" dialog text is stock .NET 10 text and proves nothing about the new API.
- Pause plus resume across a restart already worked. The only new thing is how the pause gets started.
- The write-up's "Preview 7" (the API first shipped in preview 4).
- The ApplicationStopping advice leaves out the blocking wait.

**Fix plan:**
- Reframe as ergonomics, and replace the ProtectedLocalStorage workaround with the real JS-interop one.
- Add one sentence explaining what pause does.
- Add "Drop this circuit" vs "Drain this server" buttons, with the circuit id and process visible.
- Show the restored [PersistentState] values and where they were held.
- Rewrite "See it work" as predict/watch/explain, and stop using the dialog text as evidence.
- Highlight a Drainer singleton on ApplicationStopping. Show the Program.cs registration. Un-highlight board plumbing.
- Note that ApplicationStopping must block, with a timeout.

### 4. validation-localization: Translated validation messages

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · behaviour · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/4848 (also #33073, #64626; design #65539, PRs #66646, #68202).

**Repro:** On de-DE with AddLocalization + AddValidation and an `IStringLocalizer` holding `RequiredAttribute_Error`, the messages were still English ("The Firma field is required."). The per-attribute resource workaround with one [EmailAddress] missed rendered mixed German and English. The .NET 10 `ValidationOptions` has no LocalizerProvider.

**Inaccurate claims:**
- "Keys are the English text": validation keys are rule names. English works through fallback.
- .NET 11 is never named.
- The Tab-time client validation isn't flagged as a separate .NET 11 feature.
- The display-name keys vs "Labels don't use the localizer" is unexplained.
- The default .resx is never named.

**Fix plan:**
- Add a .NET 10 panel or toggle showing an English (or mixed) message in a German form.
- Add a code card for the AddLocalization/AddValidation/LocalizerProvider wiring.
- Show a lookup table after submit: the keys tried, which one hit, the template, the {0} lookup.
- Rewrite "See it work" as predict/watch/explain.
- Fix the SupplierMessages comments.
- Fix the Workaround 2 column-split layout.
- Name .NET 11, and promote "More specific keys win".

### 5. renderfragment-across-render-modes: Static content in interactive components

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · behaviour · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/52768 (PR #66528; user pain in #55319, #57709).

**Repro:** The build is clean. `/warehouse` returns **HTTP 500** with `InvalidOperationException: Cannot pass the parameter 'ChildContent' ... cannot be serialized`. With the whole page interactive (workaround 1), it works, and an inline DateTime.Now in the fragment re-runs on reopen. The demo's stamp is held in a field, so it stays the same under the workaround too.

**Inaccurate claims:**
- "ChildContent is code, and can't be serialized" is still true. The delegate never crosses; a recorded render tree does. Without that distinction, it reads as a contradiction.
- The step 3 timestamp doesn't separate .NET 10 from .NET 11.
- "In .NET 10 that threw" doesn't say it's a run-time 500, not a compile error.
- "Nothing to switch on" holds only with prerendering on.

**Fix plan:**
- Show the captured 500 and exception text next to the working card.
- Add a "What crossed?" panel: the marker comment and its size, plus the shape of the recorded tree.
- Make the snapshot observable with an inline DateTime.Now that stays frozen while the card's own clock ticks.
- Show the "render on the first pass" gotcha as a failing variant, and take it out of the feature highlight.
- Rewrite "See it work" as predict/watch/explain.
- Rewrite the lead: ".NET 11 sends what it rendered instead." Unfold "It's a snapshot" and "Event handlers don't cross".
- State the scope: Server and WebAssembly, non-generic RenderFragment only.

### 6. quickgrid-url-paging: Grid state in the URL

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · behaviour · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/51249 (PR #65451, API review #66830).

**Repro:** On a static .NET 10 page, the headers and pager render as `<button type="button">` that do nothing: no request, no change, and query parameters are ignored. On an interactive page, sort and page work, but the URL never changes and a refresh goes back to page 1. Live .NET 11 renders `<a class="col-title" href="?sort=Price&direction=asc&page=2">`.

**Inaccurate claims:**
- "Only worked in an interactive render mode" understates it: on a static page the buttons are silently dead.
- It's framed as static-only, but interactive grids sync the URL too.
- The on-by-default breaking change and its AppContext opt-out aren't mentioned.
- It doesn't mention that sort links keep the current page.

**Fix plan:**
- Add a .NET 10 vs .NET 11 strip: dead buttons, an interactive grid reset by refresh, the .NET 11 URL.
- Show the rendered header HTML before and after, and a concrete address bar. Note that sort uses the column Title.
- Start the screenshot or demo at `?sort=Price&direction=desc&page=2`.
- Rewrite "See it work" as predict/watch/explain.
- Use a diff-style highlight for the removed `@rendermode`.
- Give the cause in workaround 1 (state lives in the circuit).
- Promote QueryParameterNameOptions, the interactive sync and the opt-out switch out of Good to know.

### 7. virtualize-variable-height: Lists with items of any height

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · behaviour · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/25058 (design #65158, anchoring fix #65939).

**Repro:** On .NET 10, scrollHeight swung between 33,057 and 45,607px, the top message went backwards 10 times while scrolling down, and Expand all pushed the message being read 549px down (overflow-anchor: none). On .NET 11, scrollHeight ranged only from 34,115 to 35,067px, the top message never went backwards, and Expand all moved it 1px.

**Inaccurate claims:**
- "The scrollbar doesn't jump" slightly overstates it: there's still about 3% drift.
- "Any height" oversells it, because extreme variance still estimates poorly.
- The write-up and the guide disagree on ItemSize (96 vs 80) and the source file.

**Fix plan:**
- Add side-by-side .NET 10 / .NET 11 panes, using motion or recorded numbers.
- Add live readouts: total messages, articles in the DOM, scrollHeight, "reading #N at Ypx".
- Rewrite "See it work" as predict/watch/explain (scroll drift; Expand all).
- Explain scroll anchoring in plain words, and simplify the step instructions.
- Narrow the highlight to Virtualize/ItemSize, and present the change as a diff against workaround 1.
- State "No new API. Upgrade and delete the workaround." Answer the ItemSize questions.
- Unfold "Give it a scrolling box". Change "doesn't jump" to "barely moves".

### 8. virtualize-csp: Virtualize under a strict CSP

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · behaviour · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/66840 (PR #66680).

**Repro:** On .NET 10 under `style-src 'self'`, the spacers measure 0px and scrollHeight is 760px instead of 40,000px. The list ends at row 19 of 1,000, with 5 CSP console errors and nothing visible on screen. On .NET 11 the spacers carry `data-blazor-virtualize-reserved-height`, measure their full height, and there are 0 errors.

**Inaccurate claims:**
- Nothing is factually wrong.
- The unchanged `<Virtualize>` is highlighted as feature code, and "no inline styles" sits next to the folded note that DevTools still shows `style="height"`.
- "Loses its scroll range" undersells it: the list silently ends at row 19.

**Fix plan:**
- Add a reconstructed ".NET 10 spacers" panel next to the real list, under the same policy (or use the repro screenshot).
- Add live spacer readouts: attribute, measured height, scrollHeight, rows reachable.
- Rewrite "See it work" as predict/watch/explain.
- Caption the Virtualize card "Unchanged". Add a before/after snippet of one spacer.
- Explain CSSOM in one sentence, and promote the DevTools note.
- Explain or un-highlight the frame-ancestors line.
- State that it applies to every interactive render mode and to QuickGrid.

### 9. wasm-culture-from-server: The same culture in the browser

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · behaviour · severity 6.
**Origin:** https://github.com/dotnet/aspnetcore/issues/26941 (from #26890; PRs #63144, #67367).

**Repro:** The prerendered HTML shows de-DE (1.234,56, Freitag). Once WebAssembly starts in an en-US browser, it flips to 1,234.56 and Friday (in fr-FR, to 1 234,56). The parameter workaround fixed the amount, but the date still flipped. The `Blazor.start applicationCulture` workaround held de-DE. AddLocalization had no effect on .NET 10.

**Inaccurate claims:**
- Nothing is false.
- It leaves out that the culture only travels when the component is prerendered.
- The highlighted lines aren't the switch; that's in Program.cs, which isn't shown.
- "Saves its culture with the page's state" is vague (it's persisted component state).

**Fix plan:**
- Add a strip: prerendered values, recorded .NET 10 values after WebAssembly starts, live .NET 11 values.
- Record the text at prerender and after hydration, next to navigator.language.
- Optionally show a prerender:false comparison, labelled honestly.
- Rewrite "See it work" as predict/watch/explain.
- Add a Program.cs card ("This turns it on"). Retitle the component card "What you no longer write".
- Show the decoded persisted culture payload.
- Name .NET 11. Add the cookie + DefaultThreadCurrentCulture workaround and the prerender requirement.

### 10. quickgrid-onrowclick: Clickable grid rows

**Verdict:** Problem strong · See it fail wrong · Fix lines weak · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/44899 (PR #64602).

**Repro:** Button per row: clicking any cell other than the button does nothing (0 handler calls). Div in every cell: clicks on the cell padding miss. `OnRowClick="Open"` on .NET 10 builds with 0 warnings and renders a dead `onrowclick="Open"` attribute. On .NET 11, leaving out TGridItem gives CS1503.

**Inaccurate claims:**
- The subtitle cites a `@rendermode` that's in a file the entry doesn't show.
- The JS workaround's Open(int) doesn't match the demo's OpenAsync(TicketRow).
- RowClass is highlighted as if it were new.
- The silent .NET 10 trap isn't mentioned.

**Fix plan:**
- Show button-per-row and OnRowClick grids side by side, with click counters and outlined click areas.
- Rewrite "See it work" as predict/watch/explain.
- Show the TicketRow passed to the handler, live.
- Split the highlight into "new" (OnRowClick and the handler) and "existing/glue" (RowClass, TGridItem). State the signature.
- Show the Tickets.razor `@rendermode` line.
- Add the silent-trap note.
- Make the workaround signatures consistent, and optionally add the div-in-cell workaround.

### 11. async-form-validation: Async validation

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · behaviour · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/7680 (PRs #66526, #67323, #67662).

**Repro:** `async void OnFieldChanged`: pressing Save during the check saved a duplicate EXOT-01, and "already used" arrived 1.1 s later. A fast edit let a stale answer land on a free code. A synchronous attribute that blocks on the DB call deadlocked the circuit, with no error logged.

**Inaccurate claims:**
- Nothing is factually wrong.
- Workaround 2's cost is understated: it saves a duplicate, and stale results land on the wrong value.
- It leaves out the deadlocking synchronous attribute.
- It never names .NET 11.

**Fix plan:**
- Run the .NET 10 workaround and the .NET 11 form side by side against the same 1.5 s lookup.
- Render the Suppliers table live.
- Rewrite "See it work" as predict/watch/explain, including a cancellation case.
- Reframe step 4 as proof that Save waits.
- Name .NET 11, and add the deadlock line.
- Explain the IsValid override, who starts and cancels the check, and the redraw comment.
- Use a mid-check screenshot.

### 12. label-and-input-ids: Labels and input ids

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · behaviour · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/49147 (PR #64636; #64791 → PR #64821).

**Repro:** `<Label>` on .NET 10 gives only warning RZ10012. Inputs render with no id. Clicking a label focuses BODY, and the textbox has no accessible name. A typo in for/id builds silently. The wrapped label "Phone" sits over "The Contact phone field is required." The .NET 10 messages already used [Display].

**Inaccurate claims:**
- The message wording in step 3 isn't new in .NET 11.
- .NET 11 is never named.
- The "Static SSR" badge vs "Any render mode".
- The demo dropped the localisation angle.

**Fix plan:**
- Show the .NET 10-style and .NET 11 forms side by side.
- Add a "Rendered HTML" panel per form with for/id matched or unmatched. Show Input.Company → Input_Company.
- Show the label-vs-message drift after submit.
- Rewrite "See it work" as predict/watch/explain.
- Name .NET 11, and say messages already used [Display].
- Add a "Renders ..." comment to the Email pair. Clarify "the id comes from the property path".
- Fix the Workaround 2 layout, and promote "An explicit id wins".

### 13. displayname-component: DisplayName

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/49147 (PR #64636; related #47155, #67070).

**Repro:** `<DisplayName>` on .NET 10 gives RZ10012 warnings and renders as a literal unknown element. A QuickGrid PropertyColumn with no Title shows "Code/Company/Contact". The validation messages say "Contact person" while the hand-typed header still says "Main contact": the drift is visible on one page.

**Inaccurate claims:**
- The "Static SSR" badge vs "Any render mode".
- "Same words as the form's labels" implies the .NET 10 labels came from [Display]; they were hand-typed too.
- DisplayNameFor vs DisplayName naming isn't linked.
- `class="quickgrid"` is on a plain table, and the real QuickGrid failure is never mentioned.
- The dummy `names` instance isn't required.

**Fix plan:**
- Add a "Before (.NET 10)" panel: typed headers, an untitled QuickGrid, a validation summary.
- Show the [Display] attributes and the resx keys (English/Deutsch).
- Show the form's labels the headers should match.
- Rewrite "See it work" as predict/watch/explain (rename the field or switch to German).
- Capture the screenshot in German.
- Frame it as the Blazor version of DisplayNameFor, sibling to Label.
- Fix the badge and the table class, and drop the dummy-instance ritual.

### 14. supply-parameter-from-session: Session state ([SupplyParameterFromSession])

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/66827 (#64422, epic #64893, PR #65184).

**Repro:** With hidden fields, a refresh loses the draft, and a tampered `Draft[chai]=9999` was accepted. Manual session with Save() works. Leaving out Save() sends a 302 with no cookie, the review page says "no draft", and nothing is logged. The attribute fails with CS0246 on .NET 10.

**Inaccurate claims:**
- The hidden-field snippet isn't a complete pattern (same-property binding returned 400).
- Refresh and Back are never demonstrated.
- The live page seems to differ from the repo (a stale deploy).
- The framing implies new capability; manual session already worked.

**Fix plan:**
- Add a "What's in session" strip.
- Show the hidden-fields lane and the session lane side by side, optionally with a forgot-Save lane.
- Rewrite "See it work" as predict/watch/explain.
- Add notes on write-back after NavigateTo and on string keys. Unfold "Simple types only".
- Complete the hidden-field snippet.
- Frame it honestly in the intro.
- Redeploy.

### 15. relative-navigation: Relative links (RelativeToCurrentUri)

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/23615 (PR #64670, API #65060).

**Repro:** `NavigateTo("summary")` from /orders/11077 returns a 302 to /summary, which is a 404. Bare tabs resolve to /lines etc. and land on 404s. `RelativeToCurrentUri="true"` on .NET 10 compiles silently and is splatted into the HTML. On live .NET 11, a trailing slash breaks the redirect (/orders/12252/12252/summary), and order 11077 doesn't exist.

**Inaccurate claims:**
- The examples use order 11077, which doesn't exist live.
- The redirect shown as correct breaks with a trailing slash.
- "Like a page with no <base>" isn't exact: dot segments aren't normalised.
- The "Static SSR" badge vs any render mode.

**Fix plan:**
- Show two tab strips against the same URL, option off and option on.
- Add a URL segment diagram and an after-click screenshot.
- Rewrite "See it work" as predict/watch/explain.
- Fix the trailing-slash redirect, or demo the trap.
- Use real ids (12252).
- Move "Off by default" into the problem section, and add the .NET 10 silent-splat gotcha.
- Trim the OrderPage card, and fix the mode chip.

### 16. virtualize-scroll-to-item: Open a list at any item

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · behaviour · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/26943 (PRs #66753, #67312).

**Repro:** Offset maths for #240 landed on #241, with #240 267px above the viewport. "Go to 35" landed on #46, with #35 not in the DOM. scrollIntoView by id found nothing. The prerender was an empty 60,000px spacer. `InitialItemIndex` on .NET 10 compiles, then returns a 500. `ScrollToItemAsync` fails with CS1061. The live .NET 11 prerender already starts at #225.

**Inaccurate claims:**
- "Shows message 1 first" is really blank, then #1, then near the target.
- "Somewhere near it" undersells landing 11 items off.
- "Slow" isn't visible at 500 items; DOM size is the honest cost.
- "#35 wasn't rendered" can't be checked on the page.
- .NET 11 is never named.

**Fix plan:**
- Add a DOM-range readout.
- Show a .NET 10 offset-maths pane next to the .NET 11 pane.
- Add a view-source/prerender toggle.
- Rewrite "See it work" as predict/watch/explain.
- Call out the count guard, the IsInteractive buttons and the app-owned highlight.
- Name .NET 11 and the old API names.
- Cite #26943.

### 17. circuit-auto-pause: Pause hidden tabs (AutoPause)

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/64886 (PR #67098).

**Repro:** With no script, a hidden tab held its circuit for 20 s and more. The guide's DIY script paused and restored state on a new circuit, but with the cursor left in a half-typed note it paused anyway, and the half-typed text was lost.

**Inaccurate claims:**
- The "RC2: don't mix the two" bullet says this app mixes them; it doesn't.
- "Pauses mid-edit" never says what you lose.
- "Mid-edit" isn't defined, and the step ordering isn't explained.

**Fix plan:**
- Add a .NET 10 script / AutoPause / no-pausing toggle.
- Add a circuit event timeline.
- Show the [PersistentState] payload, and what gets rebuilt.
- Rewrite "See it work" as predict/watch/explain.
- State the lost-text consequence and the .NET 10 pause background.
- Fix the RC2 bullet, and show the Program.cs form.
- Explain prerender:false and the 10 s vs 15 s gap.

### 18. browser-options: Reconnection settings in C#

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/66393 (team-driven, no user issue; PRs #67337, #67918).

**Repro:** Correct `Blazor.start` options give "1 of 20 ... in 2 seconds". A typo (maxRetry) behaves exactly like the defaults ("11 of 30 ... in 5 seconds"), with no warning. A missing config value renders `maxRetries: ,`, which is a SyntaxError: Blazor never starts and Pick is dead.

**Inaccurate claims:**
- The reconnect-UI id spans are highlighted as the feature, but they are an existing convention.
- "Other startup settings" is never shown.
- The default timing is left out.
- The screenshot shows the state before the drop.

**Fix plan:**
- Add a three-way demo: typo / missing config / BrowserOptions.
- Show the decoded `Blazor-Configuration` comment and a BrowserOptions tree.
- Rewrite "See it work" as predict/watch/explain.
- State the default timing.
- Narrow the highlight.
- Show WithBrowserOptions and config-bound code.
- Unfold the RC2 and static-page gotchas, and explain `static readonly`.

### 19. wasm-hosted-services: Background services in the browser

**Verdict:** Problem strong · See it fail wrong · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/63637 (revives #33537; PR #63814).

**Repro:** Without the Hosting.Abstractions package it doesn't build (CS0234/CS0246). With it, the AddHostedService watcher sits at started=False, ticks=0 forever with no warning, while the hand-started singleton keeps counting across pages and the component timer resets.

**Inaccurate claims:**
- "Compiled fine": only after adding a package.
- The version is never named.
- Surviving navigation was already possible with workaround 2; the library angle (OpenTelemetry) is left out.
- "Kept running" isn't true when no bell is listening, because the watcher skips its checks.

**Fix plan:**
- Show three counters side by side: hosted on .NET 10, hand-started, component timer.
- Show the OrderUpdate JSON and the request URL, and capture after a few ticks.
- Rewrite "See it work" as predict/watch/explain.
- Name .NET 11, fix "compiled fine", add the library line.
- Explain the double registration, and diff it against the manual StartAsync.
- Unfold "One thread" and "Lives as long as the page". Fix the `Changed is null` comment.

### 20. ssr-client-validation: Client-side validation (static SSR forms)

**Verdict:** Problem strong · See it fail weak · Fix lines weak · behaviour · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/51040 (PRs #66420, #66441, #67855, #69026).

**Repro:** On .NET 10 the inputs are bare `<input class="valid">` with no rules. Tab does nothing, and Save sends a POST that moves the server clock. On .NET 11 the same source renders `<blazor-client-validation-data data-rules=...>`, and the errors show with 0 POSTs.

**Inaccurate claims:**
- No facts are wrong.
- `Enhance` is highlighted but plays no part.
- "Writes them into the page" never names the element.
- "The browser stops the post" reads like HTML5 validation; it's blazor.web.js.
- It never says .NET 11, or that the gap dates back to .NET 8.

**Fix plan:**
- Show the rendered HTML on .NET 10 vs .NET 11, mapping each attribute to its JSON rule.
- Add a "Before" toggle (DisableClientValidation) with a POST counter and server clock.
- Un-highlight `Enhance`. Say "these lines are identical to .NET 10".
- Rewrite "See it work" as predict/watch/explain.
- Use an after-error screenshot.
- Name the version and blazor.web.js, and unfold the wrapper-inputs limitation.

### 21. wasm-env-vars-config: Server settings for WebAssembly

**Verdict:** Problem weak · See it fail weak · Fix lines strong · behaviour · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/64576 (PR #64578; BrowserOptions #66393, #30116, #68816).

**Repro:** `BrowserOptions` doesn't exist (CS0234). `AddEnvironmentVariables()` needs a package (CS1061). With a hand-written `Blazor.start` variable, `Environment.GetEnvironmentVariable` returns the server URL while `Config[...]` still returns the staging URL from the file. Layering AddEnvironmentVariables() fixes it.

**Inaccurate claims:**
- "Merge it with appsettings yourself": the .NET 10 fix was one AddEnvironmentVariables() line plus a package, and it isn't mentioned.
- "Only from files" is true by default only.
- The JS snippet leaves out `autostart="false"`.
- The write-up says "Preview 1", but the BrowserOptions half came later (Preview 7).
- "Build's own settings" isn't the .NET 10 failure.

**Fix plan:**
- Reframe around "the variable arrived but IConfiguration read the file". State the honest two-part cost.
- Add a key/file/env-var/winner table.
- Add a ".NET 10 IConfiguration" column next to .NET 11, plus GetEnvironmentVariable.
- Show the "Build's own settings" result inline.
- Rewrite "See it work" as predict/watch/explain.
- Add a WithBrowserOptions card fed from server config.
- Move SourceOf out of the way. Name .NET 11 and promote the breaking change.

### 22. cacheview: Caching part of a page

**Verdict:** Problem strong · See it fail weak · Fix lines strong · ergonomics · severity 5.
**Origin:** https://github.com/dotnet/aspnetcore/issues/55520 (community #49130; design #65756, PR #65772; renamed #67724/#67776).

**Repro:** Plain: 1.5 s per request. IMemoryCache: 0 ms on a hit but a component is still created each time, and 5 concurrent misses ran 5 queries (no stampede protection). [OutputCache] freezes the whole page, and with a form on it, antiforgery means **nothing** is cached, with no warning. `<CacheView>` on .NET 10 gives only RZ10012 and emits a literal element.

**Inaccurate claims:**
- "HTML is still rendered" costs about 0 ms; the measurable cost, stampedes, is left out.
- Workaround 1 is missing `@inject` and `rows`.
- The hand-rolled HtmlRenderer workaround isn't mentioned.
- The form/output-cache problem is understated.
- "Live holes" refers to a dashboard that isn't in this demo.
- The Task.Delay lines are highlighted as feature code.

**Fix plan:**
- Add a cache on/off toggle or side-by-side panels with timing badges and query counters.
- Outline the cached region, and add a live hole showing hit or miss.
- Rewrite "See it work" as predict/watch/explain.
- Fix workaround 1's code and its verdict line.
- Sharpen workaround 2's line.
- Re-caption the skipped lines.
- Retitle the Good to know chips, and state the static-SSR-only scope.

### 23. tempdata-ssr: TempData

**Verdict:** Problem strong · See it fail weak · Fix lines strong · ergonomics · severity 4.
**Origin:** https://github.com/dotnet/aspnetcore/issues/49683 (epic #64893, design #65039, API #66390).

**Repro:** The query flag sticks on refresh, and a forged `?saved=99999` shows a false banner. The hand-rolled cookie can be forged ("Your account is suspended..."), and after a 6 s redirect the message is lost (MaxAge 5). The .NET 11 API fails with CS0246 on .NET 10. Live .NET 11 uses an encrypted cookie that is consumed on read; a plain GET of the edit page also sets it.

**Inaccurate claims:**
- Nothing is wrong.
- It leaves out the 5 s MaxAge loss, the original complaint.
- .NET 11 is never named.

**Fix plan:**
- Add a "what's in the cookie" strip (encrypted vs readable).
- Add a "Before" pane with the query flag, refresh and a shared link.
- Rewrite "See it work" as predict/watch/explain.
- Use an after-save screenshot.
- Add the MaxAge loss to workaround 2, and cite the Identity template.
- Name .NET 11.
- Explain the attribute-vs-Get split, why it's nullable, and the null write.

### 24. virtualize-anchor-mode: Lists that follow the newest item

**Verdict:** Problem strong · See it fail weak · Fix lines strong · behaviour · severity 4.
**Origin:** https://github.com/dotnet/aspnetcore/issues/65742 (PR #66262, RC2 fix #69388; community #33426).

**Repro:** Plain Virtualize: new rows pile up below the fold (the gap grows 51, 102, 153px). The JS workaround follows, then falls 72px short at a tall row, and stops following for good. `AnchorMode` on .NET 10 compiles silently, then returns a 500 ("no property matching AnchorMode").

**Inaccurate claims:**
- The JS failure is understated: it permanently stops following.
- The demo uses fixed 50px rows, so it can't show mixed heights.
- The version and the default (Start) are never stated.
- "Fixed in RC2" is cryptic.

**Fix plan:**
- Add a status strip (message count, rows in the DOM, Following/Paused) and mixed-height rows.
- Show .NET 10 and .NET 11 feeds side by side, optionally with the JS workaround.
- Rewrite "See it work" as predict/watch/explain.
- Name .NET 11, list the enum values, and promote the scroll-box requirement.
- Add the .NET 10 silent-compile/500 gotcha. Reword the RC2 note.
- Align the render-mode sentence with the badge.

### 25. web-worker-template: .NET in a Web Worker

**Verdict:** Problem strong · See it fail weak · Fix lines strong · ergonomics · severity 4.
**Origin:** https://github.com/dotnet/runtime/issues/95452 (template PR dotnet/aspnetcore#65037).

**Repro:** Totalling 300k lines on the UI thread took 7.8 s with 0 of about 78 clock ticks and a 7,781 ms frame gap. With Task.Delay(1) every 1,000 lines it took 9.3 s, with every tick and frame gaps of at most 48 ms. That workaround stays responsive but is about 17% slower.

**Inaccurate claims:**
- It implies .NET 11 changed the threading model; it only adds a template.
- "The page still stutters" overstates workaround 1.
- Says "four files" while the write-up lists five.
- The write-up's "a few megabytes" doesn't match the live 27.4 MB.
- It leaves out that the worker run is slightly slower.

**Fix plan:**
- Reframe as tooling: page code still freezes, and the template generates the plumbing.
- Add a tick counter ("ticked 0 of ~26") and keep each button's result side by side.
- Show a CSV preview and the result list, and label the byte[] → JSON boundary.
- Rewrite "See it work" as predict/watch/explain.
- Show the `dotnet new` command and the generated files. Say WebWorkerClient is your own source.
- Show the timeout and error behaviour, and the magic-string risk.
- Unfold the Good to know chips, including the WebAssembly-only requirement. Soften workaround 1's verdict.
