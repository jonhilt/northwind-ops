---
id: browser-options
title: Configure the Blazor client from C# (WithBrowserOptions and ConfigureBrowser)
badge: BrowserOptions · client settings in C#
release: Preview 4 (final names in RC1)
mode: Interactive Server
sources: Components/Guide/Demos/Reconnect/PickList.razor, Components/Guide/Demos/Reconnect/PickListNet10.razor
docs: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md
summary: Client startup settings that used to need a hand-written Blazor.start() call, like how the browser reconnects to the server, are now set in C#. The server writes them into the page and blazor.web.js applies them, for the whole app with WithBrowserOptions or for one page with ConfigureBrowser.
---

## Try it

1. Open the pick list demo at `/demo/browser-options/pick-list`, or step through it in the Field Guide.
2. Press **Pick** on a line or two, then **Drop my Wi-Fi for 10 seconds**. That button closes this page's connection and, for 10 seconds, the server turns away this browser's attempts to rejoin. No other visitor is affected.
3. The reconnect dialog says "Rejoining the server", then "Rejoin failed... trying again in 2 seconds". Under the pick list, "Rejoin attempt … of 20" counts up. Both numbers came from C#: Blazor's own default is 30 attempts, the first 10 straight away, then 5 seconds apart, then 30 seconds apart.
4. When the 10 seconds are up, the next attempt rejoins and the dialog closes. Your picks are still there, because the circuit waited on the server.
5. View the page source. Before the pick list's `<!--Blazor:...-->` marker there's a `<!--Blazor-Configuration:...-->` comment. It's base64 JSON: `{...,"server":{"reconnectionMaxRetries":20,"reconnectionRetryIntervalMilliseconds":2000},...}`.

## The .NET 10 way

Reconnection settings only existed as JavaScript options. You turned off Blazor's automatic start in `App.razor` and started it yourself:

```razor
<script src="@Assets["_framework/blazor.web.js"]" autostart="false"></script>
<script>
    Blazor.start({
        circuit: {
            reconnectionOptions: {
                maxRetries: 20,
                retryIntervalMilliseconds: 2000
            }
        }
    });
</script>
```

Nothing checks those option names, so a misspelt one is silently ignored and you get the default. If the values belonged in configuration, you wrote them into the script with Razor (`maxRetries: @Config["Reconnect:Tries"]`). A missing setting then renders as `maxRetries: ,`, a syntax error, and because `autostart` is off, Blazor never starts at all. The same went for WebAssembly settings like `environment` and `applicationCulture`. A strict Content-Security-Policy also had to allow the inline script.

## How it works

`BrowserOptions` (in `Microsoft.AspNetCore.Components`) holds the serializable subset of Blazor's start options:

- `LogLevel`, for all render modes.
- `InteractiveServer`: `ReconnectionMaxRetries`, `ReconnectionRetryInterval` (a `TimeSpan`, sent as milliseconds) and `ReconnectionDialogId`, plus an `Extensions` dictionary that libraries use for their own keys.
- `StaticServer`: `PreserveDom` and `CircuitInactivityTimeout`.
- `InteractiveWebAssembly`: `EnvironmentName`, `ApplicationCulture` and `EnvironmentVariables`.

There are two ways to set them:

- **For every page:** `app.MapRazorComponents<App>().WithBrowserOptions(options => ...)`. The lambda runs once at startup, and the result is added to every Razor component endpoint's metadata. If you call `WithBrowserOptions` twice, the second call wins outright; the two aren't merged.
- **For one page:** `<ConfigureBrowser Options="..." />` in the page. It merges its non-null values into the request's options. The demo uses this, with a `static readonly` options object, so the rest of the site keeps Blazor's defaults.

`BrowserOptions.GetBrowserOptions(HttpContext)` returns the options for the current request: the endpoint's if `WithBrowserOptions` set some, otherwise a fresh instance.

While rendering, the server writes those options into the page as a `<!--Blazor-Configuration:...-->` comment holding base64 JSON. It goes just before the first Server, WebAssembly or Auto component marker. When `blazor.web.js` starts, it reads the comment and copies each value that's set over the matching start option, including any you passed to `Blazor.start()`. On an enhanced navigation it reads the new page's comment again, before updating the DOM, and applies the log level, the static-server settings and the reconnection settings in place. The WebAssembly environment name and variables are passed on to the WebAssembly runtime. (The `blazor.web.js` in RC2 and RC3 reads only those two back from the comment, so `ApplicationCulture` is written into the page but not applied: pass `applicationCulture` to `Blazor.start()` if you need it.)

Gotchas:

- **RC2 and RC3: `ConfigureBrowser` leaks when `WithBrowserOptions` is set.** `WithBrowserOptions` adds one options object to every page's endpoint. `GetBrowserOptions` hands out that object rather than a copy, and `ConfigureBrowser` merges into it. So once a visitor opens a page with `<ConfigureBrowser>`, every later request to every page carries that page's settings too, for every visitor. Without `WithBrowserOptions`, each request gets its own object and `ConfigureBrowser` stays on its page. This app doesn't call `WithBrowserOptions`: each demo page sets its own with `<ConfigureBrowser>`, and `GuideServerTests` checks that their settings stay on their pages.
- **Static pages get nothing.** A page with no interactive component has no marker, so no options are written. They reach the browser with the first page that has one.
- **Unset is not reset.** On an enhanced navigation only the values the new page sets are applied. Settings from an earlier page stay in force until a page sets them again.
- **Callbacks still need JavaScript.** The function form of the retry interval, `configureSignalR`, a custom `reconnectionHandler`, circuit handlers, `loadBootResource` and `configureRuntime` can't be serialized. They stay in `Blazor.start()` or a JavaScript initializer.
- **The first wait is capped.** With a fixed interval, `blazor.web.js` waits at most 3 seconds before the first attempt, however long the interval.
- **Hub settings are a different thing.** `BrowserOptions` configures the client. For the server's side of the connection, `AddInteractiveServerRenderMode(o => o.ConfigureConnection = d => ...)` (also new in .NET 11) gives typed access to the hub's `HttpConnectionDispatcherOptions`, like `ApplicationMaxBufferSize`, the transports or `CloseOnAuthenticationExpiration`. In .NET 10 you had to call `MapBlazorHub` yourself to reach them, and combining that with `AddInteractiveServerRenderMode` threw `AmbiguousMatchException`.
- **The demo's Wi-Fi drop is demo-only.** It closes the connection with `Blazor._internal.forceCloseConnection()`, an internal hook Blazor's own tests use, and a small middleware returns 503 for that browser's `/_blazor` requests while a 10-second cookie lives.
