---
id: virtualize-csp
title: Virtualize works under a strict Content-Security-Policy
badge: Virtualize · CSP-safe
release: Preview 6
mode: Interactive Server
sources: Features/Support/SupportContentSecurityPolicy.cs, Program.cs, Components/Pages/Support/Support.razor
docs: https://learn.microsoft.com/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-11.0
summary: Virtualize no longer writes inline style attributes for its spacers, so it works under a Content-Security-Policy without 'unsafe-inline' for styles. The Support pages are served with style-src 'self' and a script-src with no inline scripts except the import map.
---

## Try it

1. Load this page directly (or reload it), open DevTools → Network, select the `support` document and look at its `Content-Security-Policy` response header. There's no `'unsafe-inline'` anywhere.
2. Open a long ticket and scroll through it: it's virtualized under that policy. The Console has no CSP errors.
3. Press **Test it in this tab**. The page tries to apply an inline style, the browser refuses it (that's the only CSP error you'll see in the Console, logged twice after a full page load: once for the header's policy and once for the identical `<meta>` one), and the virtualized thread's spacers report the heights they hold.
4. In the Elements panel, find a `data-blazor-virtualize-reserved-height` attribute just above or below the messages.

## The .NET 10 way

Virtualize rendered its spacers with an inline style, recalculated as you scrolled:

```html
<div style="height: 3600px; flex-shrink: 0;"></div>
```

Under `style-src 'self'` the browser refuses every one of those attributes, so the spacers collapse and the list loses its scroll range. A nonce can't help, because nonces don't cover style attributes. Hashes only do with `'unsafe-hashes'`, and you'd need one for every height a spacer could ever have. So the security policy had to give way:

```csharp
context.Response.Headers.ContentSecurityPolicy =
    "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self'";
```

`'unsafe-inline'` for styles lets injected markup restyle the page, for example hiding a warning or laying fake content over a real form. Security reviews flag it, and in regulated environments it can block a release. The alternative was not to use `Virtualize` (or a virtualized QuickGrid) at all.

## How it works

Virtualize now renders the height as data, `<div data-blazor-virtualize-reserved-height="3600">`, and its JavaScript applies it with `element.style.setProperty("height", "3600px")`. A CSP controls style attributes (in markup or set with `setAttribute`) and `<style>` blocks; it doesn't restrict properties set one at a time through the CSS Object Model. Placeholders use the same attribute, and so does the "loop breaker" (`data-blazor-virtualize-loop-breaker-transform`, applied as a `translateY`). Until Virtualize's script has started, the spacers have no height, which is fine in prerendered HTML.

How this app applies the policy, and why:

- **Scoped to /support, by an `IStartupFilter` middleware.** The other screens haven't been checked against `style-src 'self'` yet, and one stray `style="…"` would quietly break. Scoping it lets each screen opt in when it's clean.
- **A policy belongs to a document.** Enhanced navigation fetches the next page and patches it into the current document, so a header on that response is ignored. The page therefore also renders the policy in a `<meta http-equiv="Content-Security-Policy">` tag through `HeadContent`. Enhanced navigation adds it to `<head>`, and the browser enforces a CSP `<meta>` from the moment it's inserted. A `<meta>` policy can't carry `frame-ancestors`, so that one stays header-only.
- **Leaving is a full page load.** Once applied, a policy can't be taken off a document, even by removing the `<meta>` tag. So `CspCheck.razor.js`, loaded once the circuit starts, marks links that leave /support with `data-enhance-nav="false"` just before Blazor sees the click, and reloads after Back or Forward out of Support. The next screen gets a fresh document and its own policy.
- **`script-src` without `'unsafe-inline'`.** Blazor's scripts are files, but `<ImportMap />` renders an inline `<script type="importmap">`. The middleware hashes the same `ImportMapDefinition` the component renders (its `ToString()` is the exact JSON) and allows that one hash. Interactive Server needs no `'unsafe-eval'`.
- **`connect-src 'self'`** covers the circuit's WebSocket to the same host. Interactive Server already sends `frame-ancestors 'self'` by itself; this header replaces Blazor's, so it repeats that directive.
- **Watch for other inline styles.** Any component that renders `style="…"` is refused too, and Blazor's built-in reconnection UI injects a `<style>` element (this app uses its own `ReconnectModal`, styled from a stylesheet).
