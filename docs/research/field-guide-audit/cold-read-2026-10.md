# Cold read of the walkthroughs, October 2026

Each /learn page was screenshotted top to bottom (`tools/guide-shots/learn-shots.mjs`) and read by a subagent that saw
only the screenshots. It scored: can you see the .NET 10 failure (STRONG: in a screenshot; CLICK: only a caption
says so), is .NET 10 code NEAR the first .NET 10 demo or FAR, and what confused it.

| Page | See it | .NET 10 code | Verdict | Main snags |
|---|---|---|---|---|
| async-form-validation | CLICK | FAR | WEAK | handler code only in Fig. 6; "Try 2's wrong message stays" unexplained |
| browser-options | CLICK | NEAR | OK | "jumps straight to 11" (11 of what?); config failure has no setup |
| cacheview | CLICK | NEAR | WEAK | broken demo labelled as if it works; "HybridCache does fix it" undercuts the problem |
| circuit-auto-pause | CLICK | FAR | WEAK | pause script only in Fig. 5; "paused 10 s, not 20, because it waited" reads backwards; .NET 10 section says mid-typing, demo says mid-post |
| displayname-component | STRONG | NEAR | OK | rewritten in #29 |
| environment-view | STRONG | FAR | OK | "workaround 2's line" before workarounds; "Fig. 5 uses both" vs its caption |
| label-and-input-ids | CLICK | NEAR | OK | "Don't give the input your own id", then an id; ✓ focus works beside ✗ |
| quickgrid-onrowclick | CLICK | FAR | OK | Fig. 4 line 39 says "New in .NET 11" on unchanged OpenAsync |
| quickgrid-url-paging | STRONG | NEAR | WEAK | "nothing to turn on" beside a fix box highlighting Sortable="true" |
| relative-navigation | CLICK | NEAR | WEAK | "One more thing… redirect" aside before the fix; a table row cut off |
| renderfragment-across-render-modes | STRONG | NEAR | OK | Fig. 1 route differs from Fig. 3; "What gets lost" reads as part of the fix |
| server-initiated-circuit-pause | CLICK | NEAR | WEAK | "Only the third option works" but it's marked ✗; "Nothing highlighted here is new" over a highlighted block |
| ssr-client-validation | CLICK | NEAR | WEAK | ".NET 10" badge on a .NET 11 page; "doesn't flash" vs "redraws the whole page" |
| supply-parameter-from-session | CLICK | NEAR | OK | Fig. 1 caption runs two failures together |
| tempdata-ssr | CLICK | FAR | OK | NavigateTo("orders?saved=") only in the .NET 10 section; cookie option ✓ though fakeable |
| validatable-type-stable | CLICK | FAR | CONFUSING | "All of this worked in .NET 10" undercuts Fig. 1; the fix box's using line broke .NET 10 builds |
| validation-localization | CLICK | NEAR | WEAK | fix box names a .resx the demo doesn't use; "one was missed, as often happens" |
| virtualize-anchor-mode | CLICK | FAR | WEAK | "Start is the default, which is why Fig. 1 doesn't follow" mixes .NET 10 and 11 |
| virtualize-csp | CLICK | NEAR | OK | 1,000 vs 1141 orders; "newest first" with ascending numbers |
| virtualize-scroll-to-item | STRONG | FAR | WEAK | caption says yellow end of #300 shows; screenshot shows #304 |
| virtualize-variable-height | CLICK | NEAR | WEAK | caption's measuring bar not visible; "keep ItemSize" vs "just a first guess" |
| wasm-culture-from-server | STRONG | FAR | OK | a row says en-US but shows German 1.234,56; Fig. 5 fully highlighted but "not needed" |
| wasm-env-vars-config | STRONG | FAR | OK | the server "passing" the variable needed a hand script, said much later |
| wasm-hosted-services | STRONG | FAR | CONFUSING | Fig. 1 first frame looks working (green "bell"); Fig. 2 title vs its table; "Workaround 2" before workarounds |
| web-worker-template | CLICK | FAR | WEAK | "behaves the same on .NET 10 and 11" vs a .NET 10 tag; failure and workaround in one demo |

CLICK is expected where a demo needs one click or a Play press; the fixes target FAR code, contradictions and
confusing framing.
