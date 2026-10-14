# Blazor in .NET 11: feature catalogue

Research snapshot taken 2026-10-13 from the official release notes (Preview 1 through RC1, plus the RC2 draft), Microsoft Learn, dotnet/aspnetcore PRs and issues, and community coverage. Every API was checked against SDK 11.0.100-preview.7 and the 11.0.0-rc.1 packages. The structured data is in `net11-blazor-catalogue.json`.

## Release status

As of 2026-10-13, .NET 11 Previews 1-7 and RC1 have shipped. Dates: P1 2026-02-10, P2 03-10, P3 04-14, P4 05-12, P5 06-09, P6 07-14, P7 08-11, RC1 2026-09-08. RC1 is ASP.NET Core 11.0.0-rc.1.26425.128 with a go-live license, and releases.json still lists it as the latest release. RC2 has been built (11.0.0-rc.2.26475.137; aspnetcore branch release/11.0-rc2, frozen 2026-09-24) but is not published yet: there is no tag and no packages on nuget.org. Its release notes are a draft on the dotnet/core branch release-notes/11.0-rc2 (open draft PR #10601; ASP.NET Core notes merged into that branch via #10610), so it will probably ship on Patch Tuesday, 2026-10-13. RC2's Blazor changes are small: BasePath moves to the Microsoft.AspNetCore.Components namespace; QuickGrid AnchorMode and ItemComparer are no longer experimental (ASP0030); IValidationMessageFormatter is replaced by ValidationAttribute.FormatMessage; validation of C# union cases is added; client-side validation of custom attributes and Virtualize End anchoring are fixed; AddRazorRuntimeCompilation overloads are obsolete (ASPDEPR003). The RC2 branch also drops Components.AI (it now ships out of band from main) and DeviceBoundSessions. The Blazor public API files on release/11.0 (GA) are identical to RC2, so the API looks frozen. GA is expected 2026-11-10 at .NET Conf. .NET 11 is STS, supported until 2028-11-09. Learn's What's new page reflects RC1 and has several stale samples: UseCultureFromServer, IValidationMessageFormatter, ASP0030 on QuickGrid, DeviceBoundSessions and Components.AI.

## Themes

### Forms that validate anywhere

The biggest .NET 11 Blazor story for LOB developers: instant validation in static SSR, async (database) validation, metadata-driven labels and app-wide localization.

- [Label component and auto-generated input ids](#label-and-input-ids) (solid, Preview 1)
- [DisplayName component](#displayname-component) (minor, Preview 1)
- [Client-side validation for static SSR forms](#ssr-client-validation) (headline, Preview 5)
- [Async form validation (ValidateAsync, pending/faulted state, async DataAnnotations)](#async-form-validation) (headline, Preview 5)
- [Built-in localization of validation messages and display names](#validation-localization) (solid, Preview 5)
- [[ValidatableType]/[SkipValidation] no longer experimental (nested validation)](#validatable-type-stable) (minor, Preview 6/7)
- [InputFile raises OnChange on picker cancel](#inputfile-cancel) (minor, Preview 1)
- [Form binding and input parsing fixes](#form-binding-fixes) (minor, Preview 6 / RC1)

### Static SSR without a circuit

Static SSR gains the MVC-era tools it lacked (TempData, Session, fragment caching) and can now mix freely with interactive islands. Many screens no longer need a circuit at all.

- [TempData for static SSR (ITempData and [SupplyParameterFromTempData])](#tempdata-ssr) (headline, Preview 2)
- [[SupplyParameterFromSession] for static SSR](#supply-parameter-from-session) (solid, Preview 5)
- [Public API for custom [SupplyParameterFrom*] attributes](#custom-cascading-suppliers) (minor, Preview 4)
- [CacheView: cache rendered SSR subtrees](#cacheview) (headline, Preview 7)
- [Pass ChildContent/RenderFragment across render-mode boundaries](#renderfragment-across-render-modes) (headline, Preview 5)

### Data grids and large lists

QuickGrid and Virtualize get the features LOB list screens need: bookmarkable SSR paging and sorting, row clicks, variable-height rows, live-tail anchoring, jump-to-row, and CSP-safe rendering.

- [QuickGrid sorting and paging in static SSR (URL-based)](#quickgrid-ssr-url-sort-paging) (headline, Preview 5)
- [QuickGrid OnRowClick](#quickgrid-onrowclick) (solid, Preview 1)
- [QuickGrid InitialItemIndex, ScrollToItemAsync, AnchorMode, ItemComparer](#quickgrid-virtualization-apis) (solid, Preview 7)
- [Virtualize: variable-height items and a stable viewport](#virtualize-variable-height) (headline, Preview 3)
- [Virtualize AnchorMode and ItemComparer (live feeds and logs)](#virtualize-anchor-mode) (solid, Preview 4)
- [Virtualize InitialItemIndex and ScrollToItemAsync](#virtualize-scroll-to-item) (solid, Preview 6)
- [Virtualize is CSP-compliant (no inline styles)](#virtualize-csp) (solid, Preview 6)
- [EF Core 11 features for LOB grids, reports and Blazor Server](#ef-core-11) (solid, Previews 1-7)

### Interactive Server: lighter, longer-lived circuits

Circuits pause when tabs go idle, drain cleanly before deploys, pick up new claims live, and are configured in C# rather than JavaScript.

- [Auto-pause Interactive Server circuits on hidden tabs](#circuit-auto-pause) (headline, Preview 7)
- [Server-initiated circuit pause (Circuit.RequestCircuitPauseAsync)](#server-initiated-circuit-pause) (solid, Preview 4)
- [Configure the Blazor client from C# (WithBrowserOptions / ConfigureBrowser)](#browser-options) (solid, Preview 4 bits)
- [ServerComponentsEndpointOptions.ConfigureConnection](#configure-connection) (minor, Preview 1)
- [Interactive Server circuits pick up refreshed authentication](#circuit-auth-refresh) (solid, RC1)
- [Interactive Server under Native AOT (narrow) and CircuitOptions.JsonTypeInfoResolvers](#interactive-server-native-aot) (minor, RC1)
- [Reliability fixes and clearer diagnostics](#reliability-and-diagnostics) (minor, Preview 1 - RC1)
- [Runtime-async in the ASP.NET Core shared framework](#runtime-async) (minor, Preview 4)

### WebAssembly, Aspire and observability

Standalone WASM gets a real host (Gateway, YARP, no CORS), Aspire and OTel service defaults, hosted services, runtime config and Web Workers for CPU-heavy work.

- [Blazor Gateway host for standalone WebAssembly (replaces DevServer)](#blazor-gateway) (headline, Preview 5)
- [Aspire.Hosting.Blazor: WASM apps and the Gateway in the AppHost](#aspire-hosting-blazor) (solid, Aspire 13.4)
- [WebAssembly OpenTelemetry: component metrics/tracing and service-defaults template](#wasm-otel-service-defaults) (solid, Preview 1)
- [IHostedService support in Blazor WebAssembly](#wasm-hosted-services) (solid, Preview 1)
- [Environment variables flow into WebAssembly IConfiguration](#wasm-env-vars-config) (solid, Preview 1)
- [Blazor Web Worker template (blazorwebworker)](#web-worker-template) (solid, Preview 2)
- [Prerendered WebAssembly keeps the server's culture](#wasm-culture-from-server) (solid, Preview 5)
- [Smaller WASM publish and lighter Auto-mode first visit](#wasm-load-perf) (minor, Preview 4)
- [Native OpenTelemetry HTTP semantic conventions in ASP.NET Core hosting](#native-otel-semconv) (minor, Preview 2)

### App shell, navigation and security

Small wins for the app shell (environment banners, sub-path hosting, relative links) plus secure-by-default CSRF and typed authorization attributes.

- [EnvironmentView component (was EnvironmentBoundary)](#environment-view) (solid, Preview 1)
- [BasePath component](#base-path) (solid, Preview 1)
- [Relative navigation (RelativeToCurrentUri)](#relative-navigation) (solid, Preview 1)
- [NavigationManager.GetUriWithFragment (was GetUriWithHash)](#get-uri-with-fragment) (minor, Preview 1)
- [HEAD support and enhanced-navigation fixes](#nav-fixes-and-head) (minor, Preview 1)
- [Automatic cross-origin (CSRF) protection; UseAntiforgery optional](#automatic-csrf) (solid, Preview 6)
- [IAuthorizationRequirementData honoured by AuthorizeView/AuthorizeRouteView](#authorization-requirement-data) (solid, Preview 7)
- [Passkeys in the Blazor Identity template: inferred names and stricter sign-in](#passkey-template) (minor, Preview 2)
- [Identity template switches to SignalR auth refresh](#identity-template-auth-refresh) (minor, Merged to main)
- [SqlClient 7: Entra ID auth needs Microsoft.Data.SqlClient.Extensions.Azure](#sqlclient-entra-extension) (minor, Preview 3/4)

### Developer productivity and upgrading

New analyzers, leaner templates, C# 15 in components, and the upgrade checklist (removed and obsoleted APIs).

- [New Blazor analyzers BL0010-BL0016](#blazor-analyzers) (solid, Preview 1)
- [Blazor Web App and item template updates](#template-updates) (solid, Preview 1 - RC2)
- [Removal of obsolete Blazor APIs (source breaking)](#obsolete-apis-removed) (minor, Preview 1)
- [C# union types in Blazor](#csharp-unions-in-blazor) (solid, Preview 6)
- [C# 15 language features useful in components](#csharp-15-language) (solid, Previews 1-7; stable in RC1)
- [IComponentPropertyActivator (custom [Inject] population)](#component-property-activator) (minor, Preview 1)
- [JS interop and startup polish](#js-interop-polish) (minor, Preview 1)
- [MathML namespace support in interactive rendering](#mathml) (minor, Preview 1)
- [Blazor Hybrid: @rendermode no-op and BlazorWebView caching](#blazor-hybrid-updates) (minor, Preview 6)
- [dotnet watch, Hot Reload and static web assets improvements](#dotnet-watch-tooling) (minor, Previews 1-6)

### Previews and experiments (not stable in .NET 11)

Worth showing with an 'experimental' badge, or with explicit caveats: prerelease-only packages, opt-in runtimes, and items cut after RC1.

- [Blazor AI components (Microsoft.AspNetCore.Components.AI)](#blazor-ai-components) (solid, RC1)
- [Media components: Image, Video, FileDownload](#media-components) (minor, Preview 1 bits)
- [Microsoft.AspNetCore.Components.Testing (Playwright E2E library)](#components-testing) (minor, Preview 4 code / Preview 5 package)
- [Compile-time ~/ asset-path expansion in .razor files](#asset-path-expansion) (minor, RC1)
- [Blazor WebAssembly on CoreCLR (opt-in) and Emscripten 6 toolchain](#coreclr-on-wasm) (minor, Preview 1)
- [Device Bound Session Credentials (DBSC)](#device-bound-sessions) (minor, RC1)

## Features

<a id="label-and-input-ids"></a>
### Label component and auto-generated input ids

**Release:** Preview 1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

Label<TValue> renders an accessible <label> whose text comes from [Display(Name)] or [DisplayName], falling back to the property name. It can wrap the input or point at it with for=. Built-in Input* components now emit an id derived from the bound expression via InputBase.IdAttributeValue (periods become '_', e.g. Model_ProductionDate); an explicit id still wins.

**Before (.NET 10):** In .NET 10 you hard-coded label text and gave each input an id by hand, because the Input* components rendered no id.

**LOB use case:** Large customer and order forms with localizable labels driven by model metadata, which pass accessibility audits and give Playwright tests stable ids.

**Demo idea:** Click a label to focus its input. Change [Display(Name)] once and every label updates. Show the generated for and id attributes in dev tools.

**APIs:** `Label<TValue>`, `Label.For`, `InputBase<TValue>.IdAttributeValue`, `DisplayAttribute`, `DisplayNameAttribute`

```razor
<Label For="() => Model.CompanyName">
    <InputText @bind-Value="Model.CompanyName" />
</Label>

<Label For="() => Model.CreditLimit" />
<InputNumber @bind-Value="Model.CreditLimit" />
@* <label for="Model_CreditLimit"> + <input id="Model_CreditLimit"> *@
```

> Source notes: The P1 notes show a simplified id="Name". PR #65058 and the source derive the id from the full expression, with '.' replaced by '_'. Learn documents Label only in the input-components article, not in What's new. Auto-generated ids change the rendered markup and can break CSS or test selectors.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/64821, https://github.com/dotnet/aspnetcore/pull/65058, https://learn.microsoft.com/aspnet/core/blazor/forms/input-components?view=aspnetcore-11.0#label-component

<a id="displayname-component"></a>
### DisplayName component

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

DisplayName<TValue> renders a property's display name: DisplayAttribute.Name first, then DisplayNameAttribute, then the property name. It is Blazor's equivalent of MVC's Html.DisplayNameFor and supports localization through [Display(ResourceType=...)].

**Before (.NET 10):** Blazor had no DisplayNameFor, so column headers were hard-coded or read with reflection helpers.

**LOB use case:** Table and grid headers and read-only detail views that reuse the same localized names as the edit forms.

**Demo idea:** A product table whose <th> headers come from DisplayName. Switch culture and watch the headers change.

**APIs:** `DisplayName<TValue>`, `DisplayName.For`, `DisplayAttribute.Name`

```razor
<th><DisplayName For="() => product.Name" /></th>
<th><DisplayName For="() => product.ReleaseDate" /></th>
```

Sources: https://github.com/dotnet/aspnetcore/pull/64636, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md, https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11

<a id="ssr-client-validation"></a>
### Client-side validation for static SSR forms

**Release:** Preview 5 (reshaped in Preview 7; custom-attribute fix in RC2) · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

Static SSR EditForms that contain DataAnnotationsValidator now validate in the browser with no circuit or WASM. The server emits rule metadata and blazor.web.js enforces it, for enhanced and non-enhanced forms, with the same CSS classes as interactive validation. Built-in rules cover Required, StringLength, Range, RegularExpression, EmailAddress, Compare and others. Custom attributes add rules through IClientValidationRuleProvider plus a JS validator. It is on by default.

**Before (.NET 10):** In .NET 8-10, SSR forms only validated on the server after a full POST. Instant feedback needed an interactive render mode, or MVC with jQuery unobtrusive validation.

**LOB use case:** High-traffic registration, expense and service-request forms served as cheap static SSR that still flag errors as the user tabs through.

**Demo idea:** Open the network tab on a static page: errors appear on blur with zero requests. A custom [StartsWith] rule runs in JS, and a formnovalidate 'Save draft' button skips validation.

**APIs:** `DataAnnotationsValidator.DisableClientValidation`, `RazorComponentsServiceOptions.DisableClientValidation`, `IClientValidationRuleProvider`, `ClientValidationRule`, `Blazor.formValidation.addValidator`, `formnovalidate`

```razor
<EditForm Model="Model" FormName="reg" Enhance OnValidSubmit="Save">
    <DataAnnotationsValidator />  @* client rules on by default *@
    <InputText @bind-Value="Model!.Email" />
    <ValidationMessage For="() => Model!.Email" />
</EditForm>
// custom rule
public IEnumerable<ClientValidationRule> GetClientValidationRules() =>
    [new("startswith", new Dictionary<string, string> { ["prefix"] = Prefix })];
```

> Source notes: The Preview 5 names EnableClientValidation and IClientValidationAdapter became DisableClientValidation (default false) and IClientValidationRuleProvider in Preview 7. Preview 7 also broke passkey login, which was fixed in the same release.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/forms/validation-client-side?view=aspnetcore-11.0, https://github.com/dotnet/aspnetcore/pull/66441, https://github.com/dotnet/aspnetcore/pull/69026

<a id="async-form-validation"></a>
### Async form validation (ValidateAsync, pending/faulted state, async DataAnnotations)

**Release:** Preview 5 (Blazor), Preview 6 (async DataAnnotations in the BCL), Preview 7 (API reshaped; Validate obsoleted) · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

EditForm now awaits async validators end to end before OnValidSubmit, in every render mode. Per-field async checks are tracked, and a newer check cancels the one it supersedes. IsValidationPending and IsValidationFaulted, plus the 'pending' and 'faulted' CSS classes, drive spinners. DataAnnotationsValidator runs the new BCL AsyncValidationAttribute and IAsyncValidatableObject. EditContext.Validate() is [Obsolete] and throws if async work is registered.

**Before (.NET 10):** In .NET 10, EditContext validation was synchronous only. Uniqueness checks needed hand-rolled ValidationMessageStore code and fire-and-forget tasks, with no pending state, and submit didn't wait for them.

**LOB use case:** Check that a customer code, SKU or VAT number is unique or registered against the database or an external API while the user types, and block Save until the check finishes.

**Demo idea:** A 'new supplier' form backed by a deliberately slow service: show 'Checking...', a disabled Save button, cancellation as the user retypes, and a faulted state on timeout.

**APIs:** `EditContext.ValidateAsync`, `EditContext.RegisterAsyncFieldValidator`, `ValidationRequestedEventArgs.AddAsyncValidator`, `EditContext.IsValidationPending`, `EditContext.IsValidationFaulted`, `AsyncValidationAttribute`, `IAsyncValidatableObject`, `Validator.ValidateObjectAsync`, `EditContext.Validate (Obsolete)`

```razor
public sealed class UniqueSkuAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? v, ValidationContext c) =>
        throw new InvalidOperationException("Async only");
    protected override async Task<ValidationResult?> IsValidAsync(object? v, ValidationContext c, CancellationToken ct)
        => await c.GetRequiredService<ISkuService>().ExistsAsync((string?)v, ct)
            ? new ValidationResult("SKU already exists") : ValidationResult.Success;
}
<button disabled="@editContext.IsValidationPending()">Save</button>
if (await editContext.ValidateAsync()) { await Save(); } // was Validate()
```

> Source notes: Sources disagree on the final registration API names. Learn and the release/11.0 PublicAPI give RegisterAsyncFieldValidator and AddAsyncValidator (learn-docs, github-prs, community-adjacent). notes-p5-rc read PR #67323 as TrackFieldValidation, AddValidationTask(Func<CancellationToken,Task>) and IsAsync. This entry prefers Learn and the PublicAPI; check against the RC2 SDK. The Preview 5 notes' AddValidationTask(field, task, cts) and OnValidationRequestedAsync no longer exist.

Sources: https://learn.microsoft.com/aspnet/core/blazor/forms/validation-advanced?view=aspnetcore-11.0#asynchronous-validation, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/libraries.md, https://github.com/dotnet/aspnetcore/pull/66526, https://github.com/dotnet/aspnetcore/pull/67323

<a id="validation-localization"></a>
### Built-in localization of validation messages and display names

**Release:** Preview 5 (separate package); built in from Preview 7; resource-key conventions RC1; FormatMessage RC2 · **Status:** shipped · **Significance:** solid · **Verified:** renamed

With AddLocalization() and AddValidation(), Microsoft.Extensions.Validation localizes error messages and [Display] names for Blazor forms and Minimal APIs alike, and for SSR client-side validation messages too. An explicit ErrorMessage acts as a resource key. Otherwise the lookup tries {Type}_{Member}_{Attribute}_Error, then {Type}_{Attribute}_Error, then {Attribute}_Error. ValidationOptions.LocalizerProvider points the lookup at a shared resource. Any registered IStringLocalizerFactory, for example one backed by a database, takes over.

**Before (.NET 10):** In .NET 10 you set ErrorMessageResourceType/ResourceName or [Display(ResourceType)] on every attribute. There was no IStringLocalizer pipeline and nothing shared with Minimal APIs.

**LOB use case:** A multi-country order-entry app translates every [Required] message once (RequiredAttribute_Error), and the same text appears in forms and API 400 responses.

**Demo idea:** An en/fr/de culture switcher over one form, with a side panel showing the resource-key fallback chain.

**APIs:** `AddLocalization`, `AddValidation`, `ValidationOptions.LocalizerProvider`, `IStringLocalizerFactory`, `ValidationAttribute.FormatMessage(string format, string name)`, `[ValidatableType]`

```razor
builder.Services.AddLocalization();
builder.Services.AddValidation(o =>
    o.LocalizerProvider = (_, f) => f.Create(typeof(ValidationMessages)));

[ValidatableType]
public class CustomerModel
{
    [Required, Display(Name = "CustomerName")] // CustomerModel_Name_RequiredAttribute_Error ... RequiredAttribute_Error
    public string? Name { get; set; }
}
```

> Verification: There is no ValidationAttribute.FormatMessage. The real API is Microsoft.Extensions.Validation.IValidationMessageFormatter.FormatMessage(CultureInfo culture, string messageTemplate, string displayName), which a custom ValidationAttribute implements (p7). ValidationOptions.LocalizerProvider : Func<Type,IStringLocalizerFactory,IStringLocalizer> is new in p7. AddLocalization, AddValidation, IStringLocalizerFactory and [ValidatableType] already existed. Note that ValidationOptions.MessageKeyProvider and ValidationMessageKeyContext were in p7 but are removed in RC1.

> Source notes: The API churned across previews. AddValidationLocalization<T> and its package were removed in Preview 7, MessageKeyProvider in RC1, and IValidationMessageFormatter in RC2 (replaced by overriding ValidationAttribute.FormatMessage). Learn still shows IValidationMessageFormatter.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md, https://github.com/dotnet/core/blob/release-notes/11.0-rc2/release-notes/11.0/preview/rc2/aspnetcore.md, https://learn.microsoft.com/aspnet/core/fundamentals/validation?view=aspnetcore-11.0#localize-validation-messages, https://github.com/dotnet/aspnetcore/pull/69116

<a id="validatable-type-stable"></a>
### [ValidatableType]/[SkipValidation] no longer experimental (nested validation)

**Release:** Preview 6/7 (union-case validation in RC2) · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

ValidatableTypeAttribute and SkipValidationAttribute are no longer experimental, so the ASP0029 suppression can go. With AddValidation(), DataAnnotationsValidator validates nested objects and collection items. The low-level resolver API was reshaped: IValidatableInfo was split into IValidatableTypeInfo, IValidatablePropertyInfo and IValidatableParameterInfo, and ValidateContext.AddValidationError was added. New analyzers ASP0033-ASP0038 flag unsupported configurations.

**Before (.NET 10):** In .NET 10 these attributes were [Experimental] (ASP0029) and the resolver types were public abstract classes.

**LOB use case:** Validate an Order, its OrderLines and its Address as one graph in production code without suppressing warnings.

**Demo idea:** An order form where an invalid quantity on line 3 is flagged.

**APIs:** `ValidatableTypeAttribute`, `SkipValidationAttribute`, `AddValidation`, `IValidatableTypeInfo`, `ValidateContext.AddValidationError`, `ASP0029`

```razor
builder.Services.AddValidation();

[ValidatableType]
public class Order
{
    [Required] public string? Customer { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}
```

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/forms/validation?view=aspnetcore-11.0

<a id="inputfile-cancel"></a>
### InputFile raises OnChange on picker cancel

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

InputFile listens for the HTML 'cancel' event. When the user reopens the picker and cancels, OnChange fires with zero files, so the app's state matches the browser. InputFile also gains the standard protected virtual Dispose(bool) pattern. Neither change is in the release notes.

**Before (.NET 10):** Cancelling the dialog cleared the input in the browser but raised no OnChange, so the UI kept showing a stale file.

**LOB use case:** Attachment upload forms where the selected-file summary and the Upload button must stay accurate.

**Demo idea:** Pick a file, reopen the picker, cancel: the file list empties.

**APIs:** `InputFile.OnChange`, `InputFileChangeEventArgs.FileCount`, `InputFile.Dispose(bool)`

Sources: https://github.com/dotnet/aspnetcore/pull/64772, https://github.com/dotnet/aspnetcore/pull/64771

<a id="form-binding-fixes"></a>
### Form binding and input parsing fixes

**Release:** Preview 6 / RC1 · **Status:** shipped · **Significance:** minor · **Verified:** not-api-checkable

Empty SSR form fields bound to nullable TimeSpan, Guid, Int128, UInt128 or Half now bind as null (#66625). InputNumber accepts scientific notation (RC1). Bug fixes also cover DataAnnotationsValidator throwing AmbiguousMatchException on hidden members, and validation summary list semantics.

**Before (.NET 10):** An empty optional field produced "The value '' is not valid" errors.

**LOB use case:** Optional GUID or duration fields in admin forms.

**APIs:** `[SupplyParameterFromForm]`, `InputNumber<TValue>`, `DataAnnotationsValidator`

> Verification: These are behavioural fixes. [SupplyParameterFromForm], InputNumber<TValue> and DataAnnotationsValidator all exist unchanged in .NET 10, p7 and RC1.

> Source notes: github-prs lists 'parse errors use [Display] names' (#68667/#68688) as RC1. learn-docs and notes-p5-rc found it carries milestone 12.0-preview1 and is not in release/11.0, so treat it as .NET 12.

Sources: https://github.com/dotnet/aspnetcore/pull/66625, https://github.com/dotnet/aspnetcore/pull/67988, https://github.com/dotnet/aspnetcore/pull/68667

<a id="tempdata-ssr"></a>
### TempData for static SSR (ITempData and [SupplyParameterFromTempData])

**Release:** Preview 2 (ITempData); Preview 4 ([SupplyParameterFromTempData]); value types restricted in RC1 · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

AddRazorComponents() registers TempData automatically. Components get it as a cascading ITempData (an IDictionary<string, object?> with Get, Peek, Keep and Keep(key)), or bind one value with [SupplyParameterFromTempData]. Setting that property writes the value through to the next request. The default store is a chunked cookie encrypted with Data Protection; RazorComponentsServiceOptions.TempDataProviderType = SessionStorage switches to server session. The Identity template uses it for status messages. TempData is not supplied in interactive render modes.

**Before (.NET 10):** Blazor had no TempData. Post-Redirect-Get flash messages relied on query strings or custom cookies; the Identity template shipped its own status-message cookie.

**LOB use case:** Show 'Order 1042 saved' once on the list page after an SSR form POST and redirect.

**Demo idea:** An SSR edit form saves and redirects, a flash banner shows once, and it's gone after a refresh. Show it side by side with the hand-rolled .NET 10 cookie version.

**APIs:** `ITempData`, `SupplyParameterFromTempDataAttribute`, `RazorComponentsServiceOptions.TempDataProviderType`, `TempDataProviderType.SessionStorage`, `RazorComponentsServiceOptions.TempDataCookie`

```razor
@code {
    [SupplyParameterFromTempData] public string? StatusMessage { get; set; }

    void Save()
    {
        StatusMessage = "Order saved";
        Nav.NavigateTo("/orders", forceLoad: true);
    }
}
```

> Source notes: The Preview 2 notes name a SessionStorageTempDataProvider, but in the API it is internal and selected through TempDataProviderType; it uses server-side HttpContext.Session. From RC1, values are limited to simple types (int, bool, string, Guid, DateTime, enums, their nullables and simple collections), per github-prs. Learn's list of supported types is broader and may be stale.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview2/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#temporary-data-persistence, https://github.com/dotnet/aspnetcore/pull/64749, https://github.com/dotnet/aspnetcore/pull/65306

<a id="supply-parameter-from-session"></a>
### [SupplyParameterFromSession] for static SSR

**Release:** Preview 5 (streaming-SSR cookie fix in Preview 6; throws without session from Preview 7) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

Binds an SSR component property to an HTTP session value, serialized with System.Text.Json, and writes it back before the response is sent. It requires AddSession() and UseSession(), plus sticky sessions when load balanced. Under streaming SSR the session cookie is issued before streaming begins. If session middleware is missing under static SSR, it throws InvalidOperationException.

**Before (.NET 10):** You took the cascading HttpContext and called Session.GetString/SetString with your own serialization.

**LOB use case:** Multi-step SSR wizards (purchase order, onboarding, claims intake) that keep the current step and a draft ID without hidden fields.

**Demo idea:** A three-step SSR wizard whose step survives a refresh. Inspect the .AspNetCore.Session cookie.

**APIs:** `SupplyParameterFromSessionAttribute`, `SupplyParameterFromSessionAttribute.Name`, `AddSession`, `UseSession`

```razor
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
app.UseSession();

[SupplyParameterFromSession(Name = "checkout-step")]
public int CurrentStep { get; set; }
```

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#session-data-persistence, https://github.com/dotnet/aspnetcore/pull/65184

<a id="custom-cascading-suppliers"></a>
### Public API for custom [SupplyParameterFrom*] attributes

**Release:** Preview 4 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

The [SupplyParameterFromTempData] PR made CascadingParameterSubscription public and added IServiceCollection.TryAddCascadingValueSupplier<TAttribute>(factory). Apps and libraries can now define their own attribute, derived from CascadingParameterAttributeBase, using the same plumbing as TempData and Session.

**Before (.NET 10):** You had to implement the internal-leaning ICascadingValueSupplier, or use CascadingValue components.

**LOB use case:** A [SupplyParameterFromTenant] or [SupplyParameterFromClaim("department")] attribute shared across a multi-tenant app.

**Demo idea:** A [SupplyParameterFromTenant] attribute on several pages. Label it 'undocumented API'.

**APIs:** `CascadingParameterSubscription`, `TryAddCascadingValueSupplier<TAttribute>`, `CascadingParameterAttributeBase`

```razor
public sealed class SupplyParameterFromTenantAttribute : CascadingParameterAttributeBase { }
builder.Services.TryAddCascadingValueSupplier<SupplyParameterFromTenantAttribute>(sp =>
    (state, attr, info) => new TenantSubscription(sp));
```

> Source notes: The API is in the release/11.0 PublicAPI files, but no release notes or docs mention it. community-adjacent therefore marked its status unclear.

Sources: https://github.com/dotnet/aspnetcore/pull/65306, https://github.com/dotnet/aspnetcore/blob/release/11.0/src/Components/Components/src/PublicAPI.Unshipped.txt

<a id="cacheview"></a>
### CacheView: cache rendered SSR subtrees

**Release:** Preview 7 (HEAD support and optimizations in RC2) · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

<CacheView> caches the rendered HTML of a server-rendered subtree. On a hit, its children are not instantiated. It supports ExpiresAfter, ExpiresOn and ExpiresSliding, VaryByQuery, Route, Header, Cookie, User and Culture, a free-form VaryBy, CacheKey and Enabled. Storage is in-memory (CacheViewSizeLimit, default 100 MB), or HybridCache automatically when one is registered. Components declare whether they are safe to cache with [CacheBehavior] and [CacheCondition]: AntiforgeryToken and HeadOutlet always re-render, AuthorizeView needs VaryByUser, QuickGrid needs VaryByQuery, and Virtualize throws. Caching is skipped for non-GET requests and streaming SSR.

**Before (.NET 10):** MVC had the <cache> tag helper, but Blazor only had whole-page OutputCache or hand-rolled IMemoryCache plus MarkupString.

**LOB use case:** Expensive KPI tiles, product summaries or reference-data panels that otherwise run heavy EF queries on every request.

**Demo idea:** A deliberately slow KPI tile with timestamps inside and outside the CacheView: only the outer one changes on reload. Put an uncached QuickGrid inside to show the helpful exception.

**APIs:** `CacheView`, `CacheView.VaryByQuery`, `CacheView.VaryByUser`, `CacheView.ExpiresAfter`, `CacheBehaviorAttribute`, `CacheConditionAttribute`, `CacheVaryBy`, `RazorComponentsServiceOptions.CacheViewSizeLimit`, `AddHybridCache`

```razor
<CacheView ExpiresAfter="TimeSpan.FromMinutes(10)"
           VaryByQuery="region" VaryByCulture="true">
    <SalesSummary />
</CacheView>

[CacheBehavior(CacheBehavior.Throw)]
[CacheCondition(CacheVaryBy.User)]
public sealed class MyOrders : ComponentBase { }
```

> Source notes: Learn states a 30-second default lifetime and no sliding expiration with HybridCache; the release notes don't cover either. The feature was called CacheBoundary during design.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/state-management/cacheview-component?view=aspnetcore-11.0, https://github.com/dotnet/aspnetcore/pull/65772, https://github.com/dotnet/aspnetcore/pull/68972

<a id="renderfragment-across-render-modes"></a>
### Pass ChildContent/RenderFragment across render-mode boundaries

**Release:** Preview 5 · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

A component with @rendermode can now take ChildContent, or other non-generic RenderFragment parameters such as @Body, from a static SSR parent. Blazor prerenders the fragment, captures its render tree and rehydrates it inside the interactive component. This makes interactive layouts and wrappers possible in per-page-interactivity apps. The supporting RenderTreeBuilder.SetAttributeValue API is [Experimental("ASP0032")] since RC1.

**Before (.NET 10):** In .NET 8-10 this threw 'Cannot pass the parameter ... RenderFragment cannot be serialized'. You moved markup inside the interactive component or used the Routes/sections workarounds.

**LOB use case:** Interactive Card, Tabs, Collapsible or Modal wrappers, or an interactive shell or menu, around static SSR content.

**Demo idea:** An InteractiveServer collapsible panel wrapping static markup. Show the .NET 10 exception next to the working .NET 11 page.

**APIs:** `RenderFragment`, `ChildContent`, `@rendermode`, `RenderTreeBuilder.SetAttributeValue (ASP0032)`

```razor
<CollapsiblePanel @rendermode="InteractiveServer">
    <p>Static SSR content projected into an interactive component.</p>
</CollapsiblePanel>
```

> Source notes: Learn doesn't headline this; it only hides the old limitation for 11.0 and later. Generic RenderFragment<T> and cascading values are not covered.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md, https://github.com/dotnet/aspnetcore/issues/52768, https://github.com/dotnet/aspnetcore/pull/66528, https://github.com/dotnet/aspnetcore/pull/68230

<a id="wasm-culture-from-server"></a>
### Prerendered WebAssembly keeps the server's culture

**Release:** Preview 5 (public option replaced by config in Preview 6/7) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-rc1

The server's CurrentCulture and CurrentUICulture are persisted during prerendering and applied in WASM before satellite assemblies load, so prerendered and hydrated output format dates and currency the same way. It is controlled by the Components:UseCultureFromServer configuration value. When that is unset, it is on only if an IStringLocalizerFactory is registered, for example by AddLocalization().

**Before (.NET 10):** The WASM client picked its culture independently, so dates and numbers could flip format between prerender and hydration.

**LOB use case:** A de-DE user sees '1.234,56 EUR' consistently on InteractiveAuto and WebAssembly invoice pages.

**Demo idea:** A culture picker over a WASM-rendered invoice total, with no flicker on hydration.

**APIs:** `Components:UseCultureFromServer (configuration)`, `AddLocalization`, `InteractiveWebAssemblyBrowserOptions.ApplicationCulture`

```razor
// appsettings.json
{ "Components": { "UseCultureFromServer": true } }
// Program.cs
builder.Services.AddLocalization();
```

> Verification: The config key string "Components:UseCultureFromServer" is in Microsoft.AspNetCore.Components.WebAssembly.Server.dll in p7 and RC1, and not in 10. The listed InteractiveWebAssemblyBrowserOptions.ApplicationCulture exists only in RC1. In p7 the same property is WebAssemblyBrowserOptions.ApplicationCulture.

> Source notes: WebAssemblyComponentsOptions.UseCultureFromServer was removed by PR #67367, merged 2026-06-29. learn-docs dates the removal to Preview 6 by merge date; the official notes record it in Preview 7. Learn's What's new and globalization articles still show the removed API.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/63144, https://github.com/dotnet/aspnetcore/pull/67367

<a id="quickgrid-ssr-url-sort-paging"></a>
### QuickGrid sorting and paging in static SSR (URL-based)

**Release:** Preview 5 (QueryParameterNameOptions in Preview 7) · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

Sort and page state now lives in the query string (?page=2&sort=Name&direction=asc). Sortable headers and the Paginator render as <a> links, so they work in static SSR with no circuit or WASM, and support bookmarks, back/forward and open-in-new-tab. QueryParameterNameOptions, with an optional prefix, lets several grids share a page. Sorting is matched on the column Title, so renaming a Title breaks saved URLs.

**Before (.NET 10):** In .NET 8-10, sort and page state was in memory behind <button @onclick>, so it only worked with interactivity and left no URL to share.

**LOB use case:** Read-mostly order, invoice or audit lists served as cheap SSR, where users share 'page 3 sorted by amount' links.

**Demo idea:** Two grids on one static page, one prefixed 'cities_'. Sort and page both, paste the URL into another browser, and show DevTools with no WebSocket.

**APIs:** `QuickGrid<TGridItem>.QueryParameterNameOptions`, `QueryParameterNameOptions`, `Paginator`, `PaginationState`, `Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting`

```razor
<QuickGrid Items="orders" Pagination="p1">
    <PropertyColumn Property="o => o.Total" Sortable="true" />
</QuickGrid>
<Paginator State="p1" />
<QuickGrid Items="cities" Pagination="p2"
    QueryParameterNameOptions="@(new QueryParameterNameOptions("cities_"))">...</QuickGrid>
```

> Source notes: This is a breaking markup change: <button> becomes <a>, so styles targeting button.col-title need updating. Set the AppContext switch to false to restore buttons.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid?view=aspnetcore-11.0#pagination-modes, https://github.com/dotnet/aspnetcore/pull/65451, https://github.com/dotnet/aspnetcore/pull/67733

<a id="quickgrid-onrowclick"></a>
### QuickGrid OnRowClick

**Release:** Preview 1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

OnRowClick is an EventCallback<TGridItem> that receives the clicked row's item. When set, rows get the row-clickable class, which gives a pointer cursor. Rows aren't focusable, so Learn advises also providing a keyboard-accessible link; use @onclick:stopPropagation on controls inside cells.

**Before (.NET 10):** You needed a TemplateColumn with a button or link per row, or JS click handlers.

**LOB use case:** Master/detail screens: click an order row to open its details page or a side drawer.

**Demo idea:** An orders grid where a row click opens a detail drawer, and 'Back' returns to the same URL-paged page.

**APIs:** `QuickGrid<TGridItem>.OnRowClick`, `row-clickable`, `RowClass`

```razor
<QuickGrid Items="orders" OnRowClick="@((Order o) => Nav.NavigateTo($"/orders/{o.Id}"))">
    <PropertyColumn Property="o => o.Number" />
</QuickGrid>
```

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/64602, https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid?view=aspnetcore-11.0

<a id="quickgrid-virtualization-apis"></a>
### QuickGrid InitialItemIndex, ScrollToItemAsync, AnchorMode, ItemComparer

**Release:** Preview 7 (AnchorMode/ItemComparer non-experimental in RC2) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

With Virtualize="true", QuickGrid forwards InitialItemIndex and ScrollToItemAsync to its inner Virtualize. ScrollToItemAsync throws if virtualization is off or the grid hasn't rendered yet. QuickGrid also exposes AnchorMode (default Start) and ItemComparer, so live grids can pin to the newest rows. Those two were [Experimental("ASP0030")] in Preview 7 and RC1; RC2 removes the attribute.

**Before (.NET 10):** A virtualized QuickGrid always opened at the top, couldn't be scrolled from code, and drifted when rows were prepended.

**LOB use case:** A 100k-row ledger that jumps to a searched transaction, or a live trades blotter pinned to the newest rows.

**Demo idea:** A deep link /products?row=4200 opens the grid at that row, with a 'go to row' box.

**APIs:** `QuickGrid<TGridItem>.InitialItemIndex`, `QuickGrid<TGridItem>.ScrollToItemAsync`, `QuickGrid<TGridItem>.AnchorMode`, `QuickGrid<TGridItem>.ItemComparer`, `ASP0030`

```razor
<QuickGrid @ref="grid" Items="rows" Virtualize="true" InitialItemIndex="500"
           AnchorMode="VirtualizeAnchorMode.End">...</QuickGrid>
<button @onclick="() => grid.ScrollToItemAsync(0)">Top</button>
```

> Source notes: Learn (RC1 content) still says AnchorMode and ItemComparer require opting in to ASP0030. RC2 (#69354) removes that requirement.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://github.com/dotnet/core/blob/release-notes/11.0-rc2/release-notes/11.0/preview/rc2/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/69354

<a id="virtualize-variable-height"></a>
### Virtualize: variable-height items and a stable viewport

**Release:** Preview 3 (variable heights), Preview 4 (scroll anchoring) · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

Virtualize<TItem> measures rendered items and keeps a running average height, so ItemSize is only an initial estimate. Content above the viewport can change height without a visible jump: native CSS scroll anchoring is used where supported, with a ResizeObserver fallback for <table> layouts and Safari. The default OverscanCount rises from 3 to 15; QuickGrid stays at 3. There are no API changes.

**Before (.NET 10):** In .NET 10, Virtualize sized its spacers from an average of the rows it had rendered, but didn't keep content steady when rows above the viewport changed height, so variable-height rows gave jumpy scrolling. Native scroll anchoring was disabled.

**LOB use case:** Ticket threads, audit trails and activity feeds with multi-line entries of varying height, including expandable rows.

**Demo idea:** 10,000 random-length messages. Expand cards above the viewport and the item under the cursor stays put.

**APIs:** `Virtualize<TItem>`, `Virtualize.ItemSize`, `Virtualize.OverscanCount`, `overflow-anchor`, `ResizeObserver`

```razor
<Virtualize Items="messages" Context="m">
    <article class="message-card"><h4>@m.Author</h4><p>@m.Text</p></article>
</Virtualize>
```

> Source notes: The OverscanCount change isn't on the breaking-changes page, and learn-docs dates it to Preview 4 rather than Preview 3. Community testing on RC1 (abp.io) found that RefreshDataAsync resets the height average and that deleting rows above the viewport still shifts content.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview3/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/64964, https://github.com/dotnet/aspnetcore/pull/65951, https://abp.io/community/articles/blazor-net-11-virtualize-anchormode-csp-performance-hdpestha

<a id="virtualize-anchor-mode"></a>
### Virtualize AnchorMode and ItemComparer (live feeds and logs)

**Release:** Preview 4 (renamed and comparer default in Preview 7; End-anchoring fix in RC2) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

AnchorMode takes a [Flags] VirtualizeAnchorMode: None, Start (the default) or End, and Start|End combines them. Start keeps prepended items in view for feeds. End auto-follows appends for chat and log tails, until the user scrolls away. ItemComparer correlates items across loads and defaults to EqualityComparer<TItem>.Default from Preview 7. Analyzer BL0011 flags an ItemsProvider used without one.

**Before (.NET 10):** Prepends and appends shifted the viewport, so tail-follow behaviour needed JS interop.

**LOB use case:** A live order-events log or support chat pinned to the newest entries, and a notifications feed with new items at the top.

**Demo idea:** A simulated feed appending every 500 ms with AnchorMode=End. Scroll up to pause following; return to the bottom to resume.

**APIs:** `Virtualize<TItem>.AnchorMode`, `VirtualizeAnchorMode.Start`, `VirtualizeAnchorMode.End`, `VirtualizeAnchorMode.None`, `Virtualize<TItem>.ItemComparer`, `BL0011`

```razor
<Virtualize Items="events" AnchorMode="VirtualizeAnchorMode.End" ItemComparer="byId" Context="e">
    <LogRow Event="e" />
</Virtualize>
@code {
    static readonly IEqualityComparer<OrderEvent> byId =
        EqualityComparer<OrderEvent>.Create((a, b) => a?.Id == b?.Id, e => e.Id.GetHashCode());
}
```

> Source notes: VirtualizeAnchorMode.Beginning was renamed Start in Preview 7 (#67313). Docs that use AnchorMode="Start" don't compile on RC1; use VirtualizeAnchorMode.Start (abp.io). The Preview 4 changes.json omits the backport, although the notes and API include it.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/components/virtualization?view=aspnetcore-11.0, https://github.com/dotnet/aspnetcore/pull/66521, https://github.com/dotnet/aspnetcore/pull/69388

<a id="virtualize-scroll-to-item"></a>
### Virtualize InitialItemIndex and ScrollToItemAsync

**Release:** Preview 6 (renamed in Preview 7) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

InitialItemIndex opens the list at an item on the first interactive render, without a flash of item 0. ScrollToItemAsync(int, CancellationToken) jumps to any item. Indexes are clamped, the last call wins, and a user scroll takes precedence. Calling it before the first interactive render throws InvalidOperationException.

**Before (.NET 10):** There was no API, and JS scrollTop guesses based on ItemSize broke with variable heights.

**LOB use case:** 'Go to invoice #' in a 50,000-row list, or reopen a list where the user left off.

**Demo idea:** A 'Go to row' box over a 100k-row virtualized ledger.

**APIs:** `Virtualize<TItem>.InitialItemIndex`, `Virtualize<TItem>.ScrollToItemAsync`

```razor
<Virtualize @ref="list" Items="products" InitialItemIndex="500">
    <div>@context.Name</div>
</Virtualize>
<button @onclick="() => list.ScrollToItemAsync(0)">Back to top</button>
```

> Source notes: Preview 6 named these InitialIndex and ScrollToIndexAsync.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/components/virtualization?view=aspnetcore-11.0#scroll-to-a-specific-item, https://github.com/dotnet/aspnetcore/pull/66753, https://github.com/dotnet/aspnetcore/pull/67312

<a id="virtualize-csp"></a>
### Virtualize is CSP-compliant (no inline styles)

**Release:** Preview 6 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

Spacer and placeholder heights are written to data-blazor-virtualize-reserved-height and applied through the CSS Object Model instead of inline style attributes. A strict style-src 'self' CSP therefore no longer breaks Virtualize or a virtualized QuickGrid.

**Before (.NET 10):** Inline style spacers were blocked by strict CSPs, forcing 'unsafe-inline' or no virtualization.

**LOB use case:** Regulated or enterprise apps whose security policy forbids unsafe-inline.

**Demo idea:** Serve the app with Content-Security-Policy: style-src 'self' and show a virtualized grid with a clean console.

**APIs:** `data-blazor-virtualize-reserved-height`, `Virtualize<TItem>`

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/66680

<a id="circuit-auto-pause"></a>
### Auto-pause Interactive Server circuits on hidden tabs

**Release:** Preview 7 · **Status:** shipped · **Significance:** headline · **Verified:** not-found

The opt-in Microsoft.AspNetCore.Components.Server.AutoPause package pauses a circuit after the tab has been hidden for HiddenDelay (default 2 minutes), persisting state and freeing server memory. It defers or vetoes the pause while text inputs are dirty or focused, media is playing, Picture-in-Picture or Web Locks are active, or interop or renders are in flight. Apps can add their own veto with an onCircuitPausing JS circuit handler. It is a normal shipping package.

**Before (.NET 10):** Idle tabs held circuits until disconnect timeouts, so you wrote visibilitychange JS that called Blazor.pauseCircuit() and handled the edge cases yourself.

**LOB use case:** An internal ERP where hundreds of staff leave tabs open all day, cutting per-idle-user server memory without losing half-typed forms.

**Demo idea:** A dashboard of active and paused circuit counts. Hide the tab and watch it pause after 30 s; type in a field first and the pause is deferred.

**APIs:** `Microsoft.AspNetCore.Components.Server.AutoPause`, `AddAutoPause`, `AutoPauseBrowserOptions.HiddenDelay`, `AutoPauseBrowserOptions.Enabled`, `onCircuitPausing`

```razor
<PackageReference Include="Microsoft.AspNetCore.Components.Server.AutoPause" />

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithBrowserOptions(o => o.AddAutoPause(p => p.HiddenDelay = TimeSpan.FromSeconds(30)));
```

> Verification: None of these exist in p7 or RC1, in .NET API or strings: AutoPause, AddAutoPause, AutoPauseBrowserOptions, HiddenDelay, or Microsoft.AspNetCore.Components.Server.AutoPause. The JS has no hidden-tab auto-pause either: visibilitychange is only used for the reconnect retry, as in .NET 10. onCircuitPausing exists, but as a circuit handler hook for pauses.

> Source notes: Auto-pause isn't guaranteed on mobile when the whole app is backgrounded (Learn).

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#automatic-circuit-pause-on-tab-inactivity, https://github.com/dotnet/aspnetcore/pull/67098

<a id="server-initiated-circuit-pause"></a>
### Server-initiated circuit pause (Circuit.RequestCircuitPauseAsync)

**Release:** Preview 4 (Task<bool> + CancellationToken in Preview 6/7) · **Status:** shipped · **Significance:** solid · **Verified:** renamed

Server code can ask a connected client to start the graceful pause flow, which persists [PersistentState] and frees the circuit, for example to drain a node before a deployment. It returns false if the circuit is disposed or disconnected. There's no public registry, so track Circuit instances in a CircuitHandler. Clients can defer the pause through the onPauseRequested option in Blazor.start.

**Before (.NET 10):** .NET 10 added pause and resume, but only the client could trigger them, so restarts dropped every circuit.

**LOB use case:** Zero-downtime deployments: users keep half-filled forms across a node restart.

**Demo idea:** An admin 'Drain this node' button. Restart the server and the counter or form state survives.

**APIs:** `Circuit.RequestCircuitPauseAsync(CancellationToken)`, `CircuitHandler`, `[PersistentState]`, `onPauseRequested`, `IHostApplicationLifetime.ApplicationStopping`

```razor
appLifetime.ApplicationStopping.Register(() =>
{
    var pauses = tracker.Circuits.Select(c => c.RequestCircuitPauseAsync());
    Task.WhenAll(pauses).Wait(TimeSpan.FromSeconds(10));
});
```

> Verification: Circuit.RequestCircuitPauseAsync(CancellationToken) : Task<bool> is new in p7. There is no onPauseRequested in JS. The server sends "JS.RequestPause", and the client then runs handleServerInitiatedPause, which calls the onCircuitPausing(abortSignal) callbacks registered through Blazor.start({circuitHandlers:[...]}) (p7). CircuitHandler, [PersistentState] and IHostApplicationLifetime already existed.

> Source notes: The Preview 7 notes say it arrived in Preview 6, but the Preview 4 notes document it (#66455). The return type was ValueTask<bool> in Preview 4 and became Task<bool> with an optional CancellationToken later.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/state-management/server?view=aspnetcore-11.0#server-triggered-circuit-pause, https://github.com/dotnet/aspnetcore/pull/66455, https://github.com/dotnet/aspnetcore/pull/67045

<a id="browser-options"></a>
### Configure the Blazor client from C# (WithBrowserOptions / ConfigureBrowser)

**Release:** Preview 4 bits (undocumented); documented Preview 6; final names RC1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-rc1

Client startup settings that used to need Blazor.start() JS are now set in C# on MapRazorComponents. The server serializes them into the page and blazor.web.js applies them across Server, WebAssembly and Auto, re-reading them on every enhanced navigation. Settings include LogLevel; InteractiveServer reconnection (max retries, retry interval, dialog id); StaticServer (PreserveDom, CircuitInactivityTimeout); and InteractiveWebAssembly (EnvironmentName, ApplicationCulture, EnvironmentVariables). A <ConfigureBrowser> component sets them per page.

**Before (.NET 10):** You set autostart="false" and wrote Blazor.start({ circuit: { reconnectionOptions }, webAssembly: { environment } }) by hand in App.razor.

**LOB use case:** Tune reconnection for flaky warehouse Wi-Fi, and push an OTEL or API endpoint into WASM from server config, without editing JS.

**Demo idea:** The old App.razor Blazor.start block next to the C# version. Kill the server and watch the reconnect UI honour the configured retries.

**APIs:** `WithBrowserOptions`, `BrowserOptions`, `InteractiveServerBrowserOptions`, `StaticServerBrowserOptions`, `InteractiveWebAssemblyBrowserOptions`, `ConfigureBrowser`, `BrowserOptions.GetBrowserOptions(HttpContext)`

```razor
app.MapRazorComponents<App>()
    .WithBrowserOptions(o =>
    {
        o.LogLevel = LogLevel.Warning;
        o.InteractiveServer.ReconnectionMaxRetries = 10;
        o.InteractiveServer.ReconnectionRetryInterval = TimeSpan.FromSeconds(1.5);
        o.StaticServer.PreserveDom = true;
        o.InteractiveWebAssembly.EnvironmentVariables["API_ENDPOINT"] = apiUrl;
    });
```

> Verification: In p7: RazorComponentsEndpointConventionBuilder.WithBrowserOptions(Action<BrowserOptions>), BrowserOptions, InteractiveServerBrowserOptions and ConfigureBrowser, but the other names differ. RC1 has StaticServerBrowserOptions, InteractiveWebAssemblyBrowserOptions, BrowserOptions.StaticServer / InteractiveServer / InteractiveWebAssembly, and static BrowserOptions.GetBrowserOptions(HttpContext). p7 has SsrBrowserOptions, WebAssemblyBrowserOptions, BrowserOptions.Ssr / Server / WebAssembly, and an extension httpContext.GetBrowserOptions() (BrowserOptionsHttpContextExtensions). ConfigureBrowser.HttpContext was removed in RC1.

> Source notes: The names changed twice. Preview 4 had WithBrowserConfiguration and BrowserConfiguration. Preview 6 had WithBrowserOptions with Server, Ssr and WebAssembly. RC1 renamed those to InteractiveServer, StaticServer and InteractiveWebAssembly, and GetBrowserOptions became static. The Preview 6 notes say Preview 4 introduced it, though the Preview 4 notes don't mention it. Learn reference docs wait on RC2. Researchers assigned it to server-circuits or rendering-components.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/67337, https://github.com/dotnet/aspnetcore/pull/67918

<a id="configure-connection"></a>
### ServerComponentsEndpointOptions.ConfigureConnection

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

AddInteractiveServerRenderMode(o => o.ConfigureConnection = ...) gives typed access to the Blazor hub's HttpConnectionDispatcherOptions, including CloseOnAuthenticationExpiration, AllowStatefulReconnects, ApplicationMaxBufferSize and the transports.

**Before (.NET 10):** You dug the options out of endpoint metadata. Combining MapBlazorHub with AddInteractiveServerRenderMode threw AmbiguousMatchException.

**LOB use case:** Close circuits when the auth cookie expires, for compliance, or raise buffers for data-heavy grids and large pastes.

**Demo idea:** Pair it with the auth-refresh demo, using a short cookie lifetime.

**APIs:** `ServerComponentsEndpointOptions.ConfigureConnection`, `HttpConnectionDispatcherOptions`, `CloseOnAuthenticationExpiration`

```razor
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode(o =>
       o.ConfigureConnection = d =>
       {
           d.CloseOnAuthenticationExpiration = true;
           d.ApplicationMaxBufferSize = 1024 * 1024;
       });
```

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/63825, https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-11.0

<a id="circuit-auth-refresh"></a>
### Interactive Server circuits pick up refreshed authentication

**Release:** RC1 (SignalR auth refresh in Preview 6/7) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-rc1

Built on the new SignalR authentication refresh (a /refresh endpoint, with the token lifetime in the negotiate response), which the Blazor hub and client turn on automatically. After a refresh, Blazor updates the circuit's authentication state and raises AuthenticationStateChanged. AuthorizeView and other consumers then re-render with the new roles and claims, with no reconnect or reload.

**Before (.NET 10):** A circuit kept the ClaimsPrincipal it started with. RevalidatingServerAuthenticationStateProvider could only poll and sign the user out, and expired bearer tokens dropped the connection.

**LOB use case:** An admin grants the 'Approver' role and the user's open app shows the Approve buttons without losing in-progress work.

**Demo idea:** Two windows: an admin toggles a role, and the user's AuthorizeView flips live with no reconnect banner.

**APIs:** `AuthenticationStateProvider.AuthenticationStateChanged`, `AuthorizeView`, `HttpConnectionDispatcherOptions.EnableAuthenticationRefresh`, `HubConnection.RefreshAuthenticationAsync`

```razor
<AuthorizeView Roles="Approver">
    <Authorized><button @onclick="Approve">Approve</button></Authorized>
    <NotAuthorized>Read-only</NotAuthorized>
</AuthorizeView>
@* re-renders automatically after an auth refresh *@
```

> Verification: The SignalR primitives are in p7: HttpConnectionDispatcherOptions.EnableAuthenticationRefresh and HubConnection.RefreshAuthenticationAsync(CancellationToken). But only the RC1 Microsoft.AspNetCore.Components.Server.dll uses them (IConnectionAuthenticationRefreshFeature, set_EnableAuthenticationRefresh, OnAuthenticationRefreshedAsync); the p7 Components.Server.dll has no auth-refresh code. RC1 also changes OnAuthenticationRefresh to Func<AuthenticationRefreshContext,Task<bool>>, and adds MaximumAuthenticationExpiration and the HubConnection AuthenticationRefreshed / AuthenticationRefreshFailed events.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/68221, https://github.com/dotnet/aspnetcore/pull/68593

<a id="automatic-csrf"></a>
### Automatic cross-origin (CSRF) protection; UseAntiforgery optional

**Release:** Preview 6 (scope narrowed in Preview 7) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

Apps built with WebApplication.CreateBuilder reject unsafe cross-site browser requests using the Sec-Fetch-Site and Origin headers. Since Preview 7 this only applies to endpoints that require antiforgery, which includes Blazor SSR forms. Blazor Web App templates no longer call app.UseAntiforgery(). Opt out per page with [RequireAntiforgeryToken(false)], app-wide with the DisableCsrfProtection config key, or replace ICsrfProtection. Allow legitimate cross-origin callers through a CORS policy with AllowCredentials.

**Before (.NET 10):** In .NET 8-10, Blazor Web Apps had to call app.UseAntiforgery() in the right pipeline position and relied on tokens only. Omitting it caused 'anti-forgery metadata but no middleware' errors.

**LOB use case:** Less pipeline ceremony and no token-expiry errors across tabs, while SSR forms stay protected.

**Demo idea:** A Program.cs diff, plus a form post from another origin that gets 400 until the origin is allowed through CORS.

**APIs:** `ICsrfProtection`, `IAntiforgeryValidationFeature`, `DisableCsrfProtection (config key)`, `RequireAntiforgeryTokenAttribute`, `UseAntiforgery`

```razor
var app = builder.Build();
app.MapStaticAssets();
// app.UseAntiforgery();  // optional in .NET 11; keep it for token-based defence
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

> Source notes: This is a breaking change (Learn): the Razor Components endpoint no longer validates tokens itself, and generates them only when UseAntiforgery ran. Without it, forms carry no token and protection is header-based. If both run, the token result wins.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/blazor-server-side-rendering-deferred-cross-site-request-forgery-protection.md, https://andrewlock.net/exploring-the-dotnet-11-preview-6-automatic-csrf-protection-based-on-fetch-metadata-http-headers/, https://github.com/dotnet/aspnetcore/pull/67082

<a id="authorization-requirement-data"></a>
### IAuthorizationRequirementData honoured by AuthorizeView/AuthorizeRouteView

**Release:** Preview 7 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

AuthorizeView, AuthorizeRouteView, MVC filters and SignalR hubs now apply IAuthorizeData, AuthorizationPolicy and IAuthorizationRequirementData metadata consistently, through a shared AuthorizationPolicy.CombineAsync overload. Typed attributes that carry their own requirements now work on Blazor pages without registering named policies.

**Before (.NET 10):** Blazor routing authorization was built around IAuthorizeData, meaning [Authorize] roles and policies. Requirement-data attributes needed named policies registered in AddAuthorization.

**LOB use case:** A [MinimumApprovalLevel(3)] or [RequireDepartment("Finance")] attribute protects a Blazor page and the matching API with identical semantics.

**Demo idea:** Switch user claims on a page decorated with @attribute [RequireDepartment("Finance")].

**APIs:** `IAuthorizationRequirementData`, `AuthorizationPolicy.CombineAsync`, `AuthorizeView`, `AuthorizeRouteView`

```razor
public sealed class MinimumApprovalLevelAttribute(int level) : Attribute, IAuthorizationRequirementData
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new ApprovalLevelRequirement(level)];
}
@page "/approvals"
@attribute [MinimumApprovalLevel(3)]
```

> Source notes: Exactly which metadata forms Blazor ignored before .NET 11 is inferred, not stated in the notes.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://learn.microsoft.com/aspnet/core/security/authorization/iard?view=aspnetcore-11.0

<a id="passkey-template"></a>
### Passkeys in the Blazor Identity template: inferred names and stricter sign-in

**Release:** Preview 2 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

The template's PasskeyAuthenticators.cs maps AAGUIDs to friendly names (Windows Hello, iCloud Keychain, Google Password Manager, 1Password, Bitwarden). Unknown authenticators go to the rename page. SignInManager.PasskeySignInAsync now runs PreSignInCheck and returns LockedOut or NotAllowed for locked-out or unconfirmed users, which is a breaking change.

**Before (.NET 10):** In .NET 10 users always named passkeys by hand, and passkey sign-in bypassed confirmation and lockout checks.

**LOB use case:** Passwordless staff sign-in that respects account-confirmation policy.

**Demo idea:** Register a passkey with Windows Hello and see it named automatically.

**APIs:** `PasskeyAuthenticators`, `SignInManager<TUser>.PasskeySignInAsync`, `PreSignInCheck`

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview2/aspnetcore.md, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/passkey-signin-enforces-confirmation-lockout.md, https://github.com/dotnet/aspnetcore/pull/65343

<a id="identity-template-auth-refresh"></a>
### Identity template switches to SignalR auth refresh

**Release:** Merged to main (12.0-preview1); release/11.0 backport open · **Status:** unclear · **Significance:** minor · **Verified:** not-found

This replaces IdentityRevalidatingAuthenticationStateProvider with SignalR authentication refresh (MaximumAuthenticationExpiration plus CloseOnAuthenticationExpiration), so sign-out and security-stamp changes propagate into the circuit. The backport PR #68895 was still open on 2026-10-13, and the RC2 template still uses the revalidating provider.

**Before (.NET 10):** The template polls the security stamp every 30 minutes.

**LOB use case:** Revoke access promptly after a password change.

**APIs:** `IdentityRevalidatingAuthenticationStateProvider`, `CloseOnAuthenticationExpiration`

> Verification: Both the p7 and RC1 BlazorWeb templates still register IdentityRevalidatingAuthenticationStateProvider, a RevalidatingServerAuthenticationStateProvider with a 30-minute RevalidationInterval. Neither template contains CloseOnAuthenticationExpiration, EnableAuthenticationRefresh or ConfigureConnection, so the switch has not shipped in either.

> Source notes: learn-docs lists it as .NET 12. github-prs notes the backport might still make GA.

Sources: https://github.com/dotnet/aspnetcore/pull/68663, https://github.com/dotnet/aspnetcore/pull/68895

<a id="environment-view"></a>
### EnvironmentView component (was EnvironmentBoundary)

**Release:** Preview 1 (renamed in Preview 6) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

<EnvironmentView Include=... Exclude=...> renders its child content only when the hosting environment matches. Names are comma-separated and case-insensitive, with the same semantics as MVC's <environment> tag helper. It works in Server and WebAssembly; IHostEnvironment is now injectable in WASM too. Preview 7 fixed WASM picking up the wrong environment after enhanced navigation.

**Before (.NET 10):** You injected IWebHostEnvironment or IWebAssemblyHostEnvironment (different types per host) and wrote @if checks.

**LOB use case:** A 'STAGING - test data' banner, debug panels or seed-data buttons that must never show in Production.

**Demo idea:** Switch ASPNETCORE_ENVIRONMENT, or the WASM environment through BrowserOptions, and watch the banner appear and disappear.

**APIs:** `EnvironmentView`, `EnvironmentView.Include`, `EnvironmentView.Exclude`, `IHostEnvironment`

```razor
<EnvironmentView Include="Development,Staging">
    <div class="alert alert-warning">Pre-production: test data only</div>
</EnvironmentView>
<EnvironmentView Exclude="Production"><DiagnosticsPanel /></EnvironmentView>
```

> Source notes: It shipped as EnvironmentBoundary in Previews 1-5 (renamed by #67369).

Sources: https://github.com/dotnet/aspnetcore/pull/64616, https://github.com/dotnet/aspnetcore/pull/67369, https://learn.microsoft.com/aspnet/core/blazor/fundamentals/environments?view=aspnetcore-11.0

<a id="base-path"></a>
### BasePath component

**Release:** Preview 1 (namespace moved in RC2) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

<BasePath /> renders <base href> from the request's actual path base (NavigationManager.BaseUri), so one build works under /, /hr or /finance, regardless of how the prefix is cased. The Blazor Web App template uses it in place of <base href="/" />. Sub-path hosting still needs UsePathBase on the server. Standalone WASM keeps a static <base>.

**Before (.NET 10):** App.razor hard-coded <base href="/" />, which had to be edited for each sub-path deployment.

**LOB use case:** Intranet apps behind a reverse proxy or IIS virtual directory, such as https://intranet/hr/.

**Demo idea:** Run under app.UsePathBase("/hr") and show links and assets resolving with no markup change.

**APIs:** `BasePath`, `NavigationManager.BaseUri`, `UsePathBase`

```razor
<head>
    <BasePath />
    <ResourcePreloader />
    <HeadOutlet />
</head>
```

> Source notes: In RC2 it moves from Microsoft.AspNetCore.Components.Endpoints to Microsoft.AspNetCore.Components (#69143), which can break explicit @using lines written against RC1. Learn doesn't mention the move yet.

Sources: https://github.com/dotnet/aspnetcore/pull/64590, https://github.com/dotnet/aspnetcore/pull/69143, https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/app-base-path?view=aspnetcore-11.0, https://github.com/dotnet/core/blob/release-notes/11.0-rc2/release-notes/11.0/preview/rc2/aspnetcore.md

<a id="relative-navigation"></a>
### Relative navigation (RelativeToCurrentUri)

**Release:** Preview 1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

NavigationOptions.RelativeToCurrentUri and NavLink.RelativeToCurrentUri resolve an href against the current page path instead of the app base URI. NavLink resolves it during rendering, so it also works in SSR.

**Before (.NET 10):** NavigateTo("x") and NavLink always resolved against <base href>, so sibling links had to be built from NavigationManager.Uri by hand.

**LOB use case:** A nested customer workspace (/customers/42/orders linking to /customers/42/invoices), or wizard steps.

**Demo idea:** Tabs as relative NavLinks inside /customers/42/. Show the generated hrefs.

**APIs:** `NavigationOptions.RelativeToCurrentUri`, `NavLink.RelativeToCurrentUri`, `NavigationManager.NavigateTo`

```razor
Nav.NavigateTo("invoices", new NavigationOptions { RelativeToCurrentUri = true });
<NavLink href="invoices" RelativeToCurrentUri="true">Invoices</NavLink>
```

Sources: https://github.com/dotnet/aspnetcore/pull/64670, https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-11.0

<a id="get-uri-with-fragment"></a>
### NavigationManager.GetUriWithFragment (was GetUriWithHash)

**Release:** Preview 1 (renamed in Preview 6) · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

An extension method that builds the current URI with a #fragment. It replaces any existing fragment and handles non-root base hrefs correctly.

**Before (.NET 10):** You concatenated Navigation.Uri and '#id' yourself, which broke with existing fragments or a sub-path base.

**LOB use case:** Jump-to-section links on long policy, contract or report pages.

**Demo idea:** A sticky table of contents on a long record page.

**APIs:** `NavigationManagerExtensions.GetUriWithFragment`

```razor
<a href="@Nav.GetUriWithFragment("terms")">Jump to terms</a>
```

> Source notes: Previews 1-5 called it GetUriWithHash(string hash) (renamed by #67368).

Sources: https://github.com/dotnet/aspnetcore/pull/64615, https://github.com/dotnet/aspnetcore/pull/67368

<a id="nav-fixes-and-head"></a>
### HEAD support and enhanced-navigation fixes

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** not-api-checkable

Razor component endpoints now answer HEAD with 200 and no body, where they used to return 405. Enhanced navigation no longer scrolls to the top before content renders. data-permanent preserves an element's attributes. NavigateTo works with ssr.disableDomPreservation. 404 re-execution works with OnNavigateAsync. ResourcePreloader now emits preload links only on full page loads, which is a breaking change.

**Before (.NET 10):** Uptime monitors probing with HEAD got 405, enhanced navigation flashed a jump to the top, and preload links were re-merged on every navigation.

**LOB use case:** Load-balancer and uptime probes, and smoother navigation in SSR apps with persistent sidebars.

**Demo idea:** curl -I against a page: 405 on .NET 10, 200 on .NET 11.

**APIs:** `MapRazorComponents (HEAD)`, `data-permanent`, `ResourcePreloader`, `Router.OnNavigateAsync`

> Verification: These are behavioural. MapRazorComponents, ResourcePreloader, data-permanent and Router.OnNavigateAsync all exist unchanged since .NET 10.

Sources: https://github.com/dotnet/aspnetcore/pull/64613, https://github.com/dotnet/aspnetcore/pull/64054, https://github.com/dotnet/aspnetcore/pull/64622, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/blazor-enhanced-nav-preloading-disabled.md

<a id="blazor-gateway"></a>
### Blazor Gateway host for standalone WebAssembly (replaces DevServer)

**Release:** Preview 5 (infrastructure in Preview 4; YARP proxy Preview 6; CLI tool and DevServer deprecation Preview 7) · **Status:** shipped · **Significance:** headline · **Verified:** confirmed-p7

Microsoft.AspNetCore.Components.Gateway is a lightweight ASP.NET Core host for standalone WASM in development and production, with a Microsoft.AspNetCore.Components.Gateway.Cli .NET tool. It serves static assets with SPA fallback, so deep links survive a refresh. A built-in YARP reverse proxy keeps /api calls same-origin, so no CORS is needed. It supports service discovery, HTTPS/HSTS, health checks, telemetry, Aspire and multiple client apps. The blazorwasm template uses it, the DevServer package is deprecated, and UseWebAssemblyDebugging is obsolete (ASPDEPR011) because IDEs attach the debugger automatically.

**Before (.NET 10):** DevServer was a dev-only static file server. Deep links needed rewrite rules, a separate API needed CORS, and launchSettings needed inspectUri.

**LOB use case:** A WASM front end over line-of-business REST APIs, with the same host in dev and prod and no CORS policy to keep in sync.

**Demo idea:** Remove the API's CORS policy and show /api/orders still working through the Gateway. Refresh on /orders/42 and the page loads.

**APIs:** `Microsoft.AspNetCore.Components.Gateway`, `Microsoft.AspNetCore.Components.Gateway.Cli`, `StaticWebAssetSpaFallbackEnabled`, `ReverseProxy (YARP config)`, `UseWebAssemblyDebugging (ASPDEPR011)`

```razor
<PropertyGroup><StaticWebAssetSpaFallbackEnabled>true</StaticWebAssetSpaFallbackEnabled></PropertyGroup>
<PackageReference Include="Microsoft.AspNetCore.Components.Gateway" PrivateAssets="all" />
// launchSettings.json environmentVariables
"ReverseProxy__Routes__api__ClusterId": "backend",
"ReverseProxy__Routes__api__Match__Path": "/api/{**catch-all}",
"ReverseProxy__Clusters__backend__Destinations__d1__Address": "http://localhost:5100/"
```

> Source notes: learn-docs dates it to Preview 4 (PR #65982 merged 2026-04-20). notes-p1-p4 found the Preview 4 template still used DevServer, and the official notes introduce it in Preview 5. The Gateway is configured through environment variables or the command line, not the app's appsettings.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/65982

<a id="wasm-hosted-services"></a>
### IHostedService support in Blazor WebAssembly

**Release:** Preview 1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

WebAssemblyHost starts registered IHostedService and BackgroundService implementations in RunAsync, after component state is restored, and stops them on dispose. This also unblocks libraries such as OpenTelemetry that rely on hosted services. WASM is still single-threaded, so use it for cooperative async polling, not CPU-bound work.

**Before (.NET 10):** AddHostedService registrations were silently ignored in WASM, so polling lived in component timers.

**LOB use case:** Refresh reference data or poll for notifications and approvals, independent of the current page.

**Demo idea:** A layout notification badge fed by a hosted service, which keeps updating as you navigate.

**APIs:** `IHostedService`, `BackgroundService`, `AddHostedService`, `WebAssemblyHost.RunAsync`

```razor
builder.Services.AddHostedService<ReferenceDataRefresher>();

public sealed class ReferenceDataRefresher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var t = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await t.WaitForNextTickAsync(ct)) { /* refresh */ }
    }
}
```

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/63814

<a id="wasm-env-vars-config"></a>
### Environment variables flow into WebAssembly IConfiguration

**Release:** Preview 1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-rc1

WebAssemblyHostBuilder.CreateDefault now calls AddEnvironmentVariables(). Variables passed to the WASM runtime, for example from WithBrowserOptions, the Gateway or Aspire, appear in IConfiguration and override appsettings.json keys with the same name. This is a documented behavioural breaking change.

**Before (.NET 10):** WASM configuration came only from wwwroot/appsettings*.json, and env vars were visible only through Environment.GetEnvironmentVariable.

**LOB use case:** Deploy one WASM build to dev, test and prod with API and OTEL endpoints injected at runtime.

**Demo idea:** Change API_ENDPOINT through server config, with no rebuild, and show the client reading the new value.

**APIs:** `WebAssemblyHostBuilder.CreateDefault`, `IConfiguration`, `InteractiveWebAssemblyBrowserOptions.EnvironmentVariables`

```razor
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var api = builder.Configuration["API_ENDPOINT"]; // from env vars, no extra setup
```

> Verification: The p7 WebAssembly.dll calls AddEnvironmentVariables; 10 does not. The listed InteractiveWebAssemblyBrowserOptions.EnvironmentVariables (IDictionary<string,string>) is the RC1 name. In p7 it is WebAssemblyBrowserOptions.EnvironmentVariables. WebAssemblyHostBuilder.CreateDefault and IConfiguration are unchanged.

Sources: https://github.com/dotnet/aspnetcore/pull/64578, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/wasm-env-vars-in-configuration.md

<a id="wasm-otel-service-defaults"></a>
### WebAssembly OpenTelemetry: component metrics/tracing and service-defaults template

**Release:** Preview 1 (metrics/tracing), Preview 4 (template); OpenTelemetry 1.19 post-RC1 · **Status:** shipped · **Significance:** solid · **Verified:** renamed

When metrics support is enabled (the Meter.IsSupported switch), the WASM host registers the same ComponentsMetrics and ComponentsActivitySource as the server. A new template generates an Aspire-style service-defaults library: AddBlazorClientServiceDefaults() wires up OpenTelemetry logs, metrics and traces over OTLP, service discovery, and standard HTTP resilience. Through the Gateway, telemetry uses a same-origin /_otlp path.

**Before (.NET 10):** Component telemetry was server-only. WASM clients set up OTel, CORS on the dashboard, and resilience by hand.

**LOB use case:** End-to-end traces from a button click in the browser, through the API, to the database in the Aspire dashboard, when diagnosing slow screens.

**Demo idea:** An Aspire dashboard trace that starts in the WASM client.

**APIs:** `AddBlazorClientServiceDefaults`, `dotnet new blazor-wasm-servicedefaults`, `ComponentsMetrics`, `ComponentsActivitySource`, `System.Diagnostics.Metrics.Meter.IsSupported`

```razor
// dotnet new blazor-wasm-servicedefaults -o MyApp.ServiceDefaults
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.AddBlazorClientServiceDefaults();
await builder.Build().RunAsync();
```

> Verification: The template short name is blazorwasm-servicedefaults, not blazor-wasm-servicedefaults (it is in p7 and RC1 and was not in 10). AddBlazorClientServiceDefaults(this WebAssemblyHostBuilder) is code generated by the template, not a framework API. It adds the meters "Microsoft.AspNetCore.Components" and ".Lifecycle" and the source "Microsoft.AspNetCore.Components". ComponentsMetrics and ComponentsActivitySource are internal types. The public API is AddComponentsMetrics/AddComponentsTracing, which existed in 10, and is wired into WebAssembly.dll from p7. Meter.IsSupported is the AppContext switch "System.Diagnostics.Metrics.Meter.IsSupported", not a public property; the Components and WebAssembly DLLs reference it from p7.

> Source notes: github-prs gives the template short name as blazorwasm-servicedefaults; the notes, Learn and Aspire docs use blazor-wasm-servicedefaults. Check with dotnet new list. The <MetricsSupport> MSBuild property for enabling metrics was inferred by notes-p1-p4, not documented.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://learn.microsoft.com/aspnet/core/blazor/tooling?view=aspnetcore-11.0#service-defaults-library-for-blazor-webassembly-apps, https://github.com/dotnet/aspnetcore/pull/64737, https://github.com/dotnet/aspnetcore/pull/64807, https://aspire.dev/dashboard/enable-browser-telemetry/blazor-webassembly/

<a id="aspire-hosting-blazor"></a>
### Aspire.Hosting.Blazor: WASM apps and the Gateway in the AppHost

**Release:** Aspire 13.4 (2026-06-01), alongside .NET 11 Preview 5 · **Status:** experimental · **Significance:** solid · **Verified:** confirmed-rc1

Aspire's preview Blazor hosting integration provides AddBlazorWasmProject, AddBlazorGateway and WithBlazorClientApp. The Gateway forwards service references and config to the WASM client and proxies its telemetry. The APIs carry the experimental diagnostic ASPIREBLAZOR001.

**Before (.NET 10):** Standalone WASM had no first-class AppHost resource, so URLs and CORS were wired by hand.

**LOB use case:** Run a WASM front end plus several APIs, orchestrated and observed locally with one F5.

**Demo idea:** An AppHost resource graph showing gateway, WASM app and API, with live traces.

**APIs:** `AddBlazorWasmProject`, `AddBlazorGateway`, `WithBlazorClientApp`, `ASPIREBLAZOR001`

```razor
var api = builder.AddProject<Projects.OrdersApi>("api");
var app = builder.AddBlazorWasmProject("app", "../Client/Client.csproj").WithReference(api);
builder.AddBlazorGateway("gateway").WithExternalHttpEndpoints().WithBlazorClientApp(app);
```

> Verification: This is an out-of-band Aspire package, Aspire.Hosting.Blazor; the first version is 13.5.4-preview.1.26464.4, after RC1, and the latest is 13.6.1-preview.1.26506.6. In BlazorGatewayExtensions: AddBlazorWasmProject<TProject>(builder, name), AddBlazorWasmApp, AddBlazorGateway(builder, name), and WithBlazorClientApp(gateway, wasmApp, ...) on Project and DotnetProject resources. It is marked [Experimental("ASPIREBLAZOR001")].

> Source notes: Debugging WASM launched from the Aspire dashboard (#64891) and the remaining Aspire integration epic (#64888) moved to .NET 12.

Sources: https://aspire.dev/integrations/dotnet/blazor-hosting/, https://aspire.dev/whats-new/aspire-13-4/

<a id="web-worker-template"></a>
### Blazor Web Worker template (blazorwebworker)

**Release:** Preview 2 (as webworker); renamed and extended in Preview 4 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

dotnet new blazorwebworker scaffolds WebWorkerClient plus JS glue, so static [JSExport] methods run on a second .NET runtime in a Web Worker and the WASM UI stays responsive. CreateAsync, InvokeAsync and InvokeVoidAsync accept timeouts and CancellationTokens. Create the worker once and reuse it, since each one boots a full runtime. Only primitives, strings and byte[] cross the boundary directly, so serialize anything else to JSON. Methods are found by string name, and workers have no DI or DOM.

**Before (.NET 10):** You followed a long manual guide, or used the community BlazorWorker library, and CPU-heavy work froze the single UI thread.

**LOB use case:** Client-side CSV or Excel import parsing, large report aggregation or PDF generation without freezing the UI.

**Demo idea:** A spinner keeps animating while 1M rows aggregate in a worker; run the same work on the UI thread and it freezes.

**APIs:** `dotnet new blazorwebworker`, `WebWorkerClient.CreateAsync`, `WebWorkerClient.InvokeAsync<TResult>`, `WebWorkerClient.InvokeVoidAsync`, `[JSExport]`

```razor
await using var worker = await WebWorkerClient.CreateAsync(JS, cancellationToken: ct);
var json = await worker.InvokeAsync<string>(
    "MyApp.Worker.WorkerMethods.Aggregate", [csvText], cancellationToken: ct);
```

> Source notes: Fixes in Preview 3 and 4 were needed for published, fingerprinted apps (#65885). Real WASM multithreading is still unsupported, so workers are the supported route.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/66070, https://andrewlock.net/exploring-the-dotnet-11-preview-1-running-background-tasks-in-blazor-with-web-workers, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/blazor/blazor-with-dotnet-on-web-workers.md

<a id="wasm-load-perf"></a>
### Smaller WASM publish and lighter Auto-mode first visit

**Release:** Preview 4 (trimming), Preview 6 (Auto prefetch) · **Status:** shipped · **Significance:** minor · **Verified:** not-api-checkable

ComponentsMetrics, ComponentsActivitySource and HotReloadManager sit behind [FeatureSwitchDefinition] switches, so trimmed apps that don't use OTel or hot reload drop that code. On a first visit, Interactive Auto uses dotnet.download() to prefetch WASM assets into the HTTP cache without starting the runtime while a Server circuit is active (#66656). An out-of-process WASM renderer exists, but it is internal and opt-in through an environment variable only.

**Before (.NET 10):** Telemetry and hot-reload code survived trimming, and Auto mode started the full WASM runtime on the first visit.

**LOB use case:** Faster first loads for WASM and Auto apps on slow corporate networks and low-end laptops.

**Demo idea:** Compare _framework payload sizes between .NET 10 and .NET 11 publishes.

**APIs:** `FeatureSwitchDefinitionAttribute`, `System.Diagnostics.Metrics.Meter.IsSupported`, `MetadataUpdater.IsSupported`, `dotnet.download()`

> Verification: The perf changes are behavioural. The listed APIs all predate 11: FeatureSwitchDefinitionAttribute (.NET 9+), MetadataUpdater.IsSupported, and dotnet.download() (in the 10.0.2 Mono dotnet.js). Meter.IsSupported is an AppContext switch; the Components and WebAssembly DLLs reference it from p7.

Sources: https://github.com/dotnet/aspnetcore/pull/65901, https://github.com/dotnet/aspnetcore/pull/65903, https://github.com/dotnet/aspnetcore/pull/66656

<a id="coreclr-on-wasm"></a>
### Blazor WebAssembly on CoreCLR (opt-in) and Emscripten 6 toolchain

**Release:** Preview 1 (foundations), Preview 5 (Blazor opt-in), Preview 7 (Emscripten 6.0.3) · **Status:** experimental · **Significance:** minor · **Verified:** confirmed-p7

A Blazor WASM project can run on CoreCLR (an interpreter plus R2R) instead of Mono by setting <UseMonoRuntime>false</UseMonoRuntime>. Mono stays the default in .NET 11, and AOT and native builds still need Mono. Separately, the wasm-tools workload moves to Emscripten 6.0.3, and Preview 3 adds more direct marshalling of float[] and Span<float>.

**Before (.NET 10):** Through .NET 10, Blazor WASM ran only on Mono.

**LOB use case:** Early evaluation against the future runtime only; not for production LOB apps on .NET 11.

**Demo idea:** An experimental build with a runtime badge showing Mono or CoreCLR.

**APIs:** `UseMonoRuntime`, `wasm-tools`, `WebCIL`, `globalThis.getDotnetRuntime(0).INTERNAL.GetDotNetRuntimeHeap()`

```razor
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <UseMonoRuntime>false</UseMonoRuntime>
</PropertyGroup>
```

> Source notes: ironsoftware claims native multithreading and 64-bit memory; the release notes don't support this. The runtime epic (dotnet/runtime#121511) puts stability and the transition in the .NET 12 timeframe.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/runtime.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/runtime.md, https://github.com/dotnet/runtime/issues/121511

<a id="blazor-analyzers"></a>
### New Blazor analyzers BL0010-BL0016

**Release:** Preview 1 (BL0010), Preview 4 (BL0011), Preview 7 (BL0012-BL0016) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

All seven are warnings by default. BL0010: use InvokeVoidAsync instead of InvokeAsync<object>. BL0011: Virtualize with an ItemsProvider needs an ItemComparer. BL0012: unnecessary StateHasChanged (code fix). BL0013: GetAuthenticationStateAsync without subscribing to AuthenticationStateChanged. BL0014: for-loop variable captured in a closure or RenderFragment. BL0015: non-public [JSInvokable] (code fix). BL0016: JS interop call outside try/catch. Upgraded apps will see new warnings.

**Before (.NET 10):** These classic mistakes, such as every Remove button deleting the last row, compiled silently.

**LOB use case:** Catch the for-loop closure bug in a line-items table, and stale auth state, during CI.

**Demo idea:** An 'analyzer gallery' page of anti-patterns with their IDE squiggles and code fixes.

**APIs:** `BL0010`, `BL0011`, `BL0012`, `BL0013`, `BL0014`, `BL0015`, `BL0016`

```razor
@for (var i = 0; i < lines.Count; i++)
{
    <button @onclick="() => Remove(i)">Remove</button> @* BL0014 *@
}
await JS.InvokeAsync<object>("console.log", "x"); // BL0010
[JSInvokable] private void OnResize() { }           // BL0015
```

> Source notes: Learn's What's new documents only BL0010. learn-docs dates BL0012-BL0016 to Previews 6-7 by merge date; the notes say Preview 7. BL0017-BL0019 exist only on main (.NET 12), and JS interop analyzer revisions (#69072, #65233) are still open.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://github.com/dotnet/aspnetcore/blob/release/11.0/src/Components/Analyzers/src/DiagnosticDescriptors.cs, https://github.com/dotnet/aspnetcore/pull/64623

<a id="template-updates"></a>
### Blazor Web App and item template updates

**Release:** Preview 1 - RC2 (cumulative) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-rc1

Template changes across the release:
- <BasePath /> replaces the hard-coded <base href>.
- No app.UseAntiforgery() or UseWebAssemblyDebugging().
- NavMenu uses a collocated NavMenu.razor.js module instead of inline onclick, so it works under a strict CSP.
- The Weather page uses [PersistentState(AllowUpdates = true)], so there is no 'Loading...' flash.
- The template calls EditContext.ValidateAsync.
- Identity pages use [SupplyParameterFromTempData].
- Visual Studio offers 'Enable container support'.
- The standalone WASM template uses the Gateway.
- RC1 adds a code-behind option to the item template.
- RC2 brings Bootstrap 5.3.8 and accessibility fixes.

**Before (.NET 10):** The .NET 10 templates hard-coded the base href, required UseAntiforgery, used an inline-JS NavMenu that broke strict CSP, a custom status-message cookie and the DevServer, and flashed 'Loading...' after prerender.

**LOB use case:** New LOB projects start CSP-friendly, ready for sub-path hosting, and without prerender flicker.

**Demo idea:** A diff of dotnet new blazor output (Program.cs and App.razor), .NET 10 vs .NET 11.

**APIs:** `<BasePath />`, `NavMenu.razor.js`, `[PersistentState(AllowUpdates = true)]`, `use-code-behind`, `supportsDocker`

> Verification: Already in the p7 BlazorWeb template: <BasePath /> in App.razor, NavMenu.razor.js referenced from NavMenu.razor, [PersistentState(AllowUpdates = true)] in Weather.razor, and supportsDocker in ide.host.json (new for BlazorWeb in 11, absent in 10). The RazorComponent item template's use-code-behind option (with Component1.razor.cs) exists only in RC1.

Sources: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/migration/100-to-110/includes/blazor.md, https://github.com/dotnet/aspnetcore/pull/63828, https://github.com/dotnet/aspnetcore/pull/67989, https://github.com/dotnet/aspnetcore/pull/63815

<a id="obsolete-apis-removed"></a>
### Removal of obsolete Blazor APIs (source breaking)

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** renamed

Removed: Router.PreferExactMatches; an old ResourceAsset constructor; AddDataAnnotationsValidation, and EnableDataAnnotationsValidation(EditContext) without IServiceProvider; RemoteBrowserFileStreamOptions; WebEventCallbackFactoryEventArgsExtensions; the WebRenderer.RendererId init accessor; JSInteropMethods.NotifyLocationChanged(string, bool). From WebAssembly.Authentication: SignOutSessionStateManager, the logger-less RemoteAuthenticationService constructor, and the legacy AccessTokenResult constructor and RedirectUrl (use InteractiveRequestUrl).

**Before (.NET 10):** These compiled with [Obsolete] warnings in .NET 10.

**LOB use case:** The upgrade checklist for long-lived apps started on .NET 5-8, especially WASM apps using authentication.

**Demo idea:** An upgrade-notes slide listing the compile errors and their replacements.

**APIs:** `Router.PreferExactMatches`, `EnableDataAnnotationsValidation`, `SignOutSessionStateManager`, `AccessTokenResult.InteractiveRequestUrl`, `RemoteBrowserFileStreamOptions`

> Verification: Removed in p7 relative to 10: Router.PreferExactMatches; EnableDataAnnotationsValidation(EditContext) (the overload without IServiceProvider) and AddDataAnnotationsValidation; SignOutSessionStateManager; RemoteBrowserFileStreamOptions; WebEventCallbackFactoryEventArgsExtensions; and WebRenderer.RendererId. AccessTokenResult.InteractiveRequestUrl was not removed and is still present and non-obsolete in p7 and RC1. What was removed is AccessTokenResult.RedirectUrl and the 3-argument AccessTokenResult constructor.

Sources: https://github.com/dotnet/aspnetcore/pull/62755, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/blazor-obsolete-apis-removed.md

<a id="csharp-unions-in-blazor"></a>
### C# union types in Blazor

**Release:** Preview 6 (Blazor support), Preview 7 (Razor literal attributes), RC1 (C# 15 stable), RC2 (union validation) · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-rc1

C# unions work as component parameters (including prerendered root parameters), JS interop arguments and results, and persisted component state, all through System.Text.Json. Since Preview 7, Razor converts a literal attribute to a union's string case (Message="Saved"). RC2 adds validation of union cases. Unions are not supported for route parameters, [SupplyParameterFromQuery] or [SupplyParameterFromForm]. Cases with the same JSON shape need [JsonUnion] classifiers to round-trip, which matters for persisted state and Auto mode.

**Before (.NET 10):** Components exposed parameter pairs such as Text plus TextContent, or used OneOf-style libraries or object parameters with runtime type checks.

**LOB use case:** A Toast or Field component whose Hint takes either plain text or rich markup through one type-safe parameter.

**Demo idea:** <Toast Message="Saved 3 items." /> and <Toast Message="@richFragment" /> using one parameter, rendered with an exhaustive switch.

**APIs:** `union (C# 15)`, `[Parameter]`, `JsonUnionAttribute`, `JsonUnionTypeStructuralClassifier`, `[PersistentState]`

```razor
public union ToastMessage(string, RenderFragment);

[Parameter, EditorRequired] public ToastMessage Message { get; set; }

<Toast Message="Saved 3 items." />
<Toast Message="@(@<b>Order #42 shipped</b>)" />
```

> Verification: I compiled 'public union Pet(Cat, Dog);' on p7, which needs <LangVersion>preview</LangVersion>, and on RC1 with the default LangVersion. JsonUnionAttribute (TypeClassifier) is in p7, but deserialisation throws 'ambiguous for union type' without a classifier. JsonUnionTypeStructuralClassifier exists only in RC1; [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))] round-trips correctly on RC1. [Parameter] and [PersistentState] are unchanged.

> Source notes: notes-p5-rc marks this experimental (LangVersion preview). community-adjacent reports C# 15 is stable and the default for net11.0 from RC1. heise reported runtime errors with union parameters in Preview 7. The templated child-element syntax for a union that includes RenderFragment is unsupported (roslyn#85657, Backlog), and full union support (#64599) moved to .NET 12.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/release-notes/aspnetcore-11/includes/csharp-unions.md, https://andrewlock.net/exploring-the-dotnet-11-preview-7-the-pain-of-serializing-unions-and-closed-class-hierarchies-with-system-text-json/

<a id="csharp-15-language"></a>
### C# 15 language features useful in components

**Release:** Previews 1-7; stable in RC1 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

C# 15 is the default for net11.0 from RC1. It adds unions, closed class hierarchies (exhaustive switches, plus STJ InferClosedTypePolymorphism), collection-expression constructor arguments ([with(comparer), ..items]), extension indexers, and labeled break/continue.

**Before (.NET 10):** In C# 14 there were no unions or closed hierarchies, so switch expressions over workflow states weren't exhaustive.

**LOB use case:** Document workflow states (Draft, Submitted, Approved) and Result(Success, ValidationFailed, NotFound) handled exhaustively in components.

**Demo idea:** A status badge component whose switch stops compiling when a new state is added.

**APIs:** `union`, `closed`, `JsonSerializerOptions.InferClosedTypePolymorphism`, `with(...) collection element`

```razor
public closed record class OrderState;
public record class Draft : OrderState;
public record class Approved(string By) : OrderState;

HashSet<string> codes = [with(StringComparer.OrdinalIgnoreCase), .. input];
```

> Source notes: Andrew Lock notes that closed-hierarchy inference rejects multi-level hierarchies, so you still need [JsonDerivedType].

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/csharp.md, https://andrewlock.net/exploring-the-dotnet-11-preview-2-dotnet-gets-union-types/, https://andrewlock.net/exploring-the-dotnet-11-preview-4-closed-class-hierarchies/

<a id="component-property-activator"></a>
### IComponentPropertyActivator (custom [Inject] population)

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

A public interface for customizing how [Inject] properties are populated. The default implementation caches per component type, supports keyed services ([Inject(Key = ...)]), invalidates on Hot Reload, and is annotated for trimming and AOT.

**Before (.NET 10):** Only IComponentActivator (construction) was pluggable, and property injection was internal.

**LOB use case:** Third-party DI containers such as Autofac, or tenant-aware or audited injection.

**Demo idea:** A decorator activator that logs every injected service on a diagnostics page.

**APIs:** `IComponentPropertyActivator`, `GetActivator(Type)`, `[Inject(Key = ...)]`

```razor
public sealed class AuditingActivator(IComponentPropertyActivator inner) : IComponentPropertyActivator
{
    public Action<IServiceProvider, IComponent> GetActivator(Type t)
    {
        var a = inner.GetActivator(t);
        return (sp, c) => { Console.WriteLine($"Injecting {t.Name}"); a(sp, c); };
    }
}
```

Sources: https://github.com/dotnet/aspnetcore/pull/64595, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md

<a id="js-interop-polish"></a>
### JS interop and startup polish

**Release:** Preview 1 (WebView DisposeAsync fix in Preview 6) · **Status:** shipped · **Significance:** minor · **Verified:** not-api-checkable

blazor.server.js and blazor.webassembly.js now accept blazor.web.js's nested { circuit, webAssembly, ssr } options shape. IJSObjectReference.DisposeAsync swallows JSDisconnectedException once the circuit or WebView is gone. JS can return DOM elements to .NET as ElementReference. Blazor.registerCustomEventType now throws when the event name equals browserEventName, which is a breaking change.

**Before (.NET 10):** Each script took a different options shape, every module wrapper needed try/catch in DisposeAsync, and same-name custom events silently fired twice.

**LOB use case:** Less boilerplate in components that wrap JS chart or map libraries, and a startup snippet shared between legacy Server and Web App projects.

**APIs:** `Blazor.start`, `IJSObjectReference.DisposeAsync`, `JSDisconnectedException`, `ElementReference`, `Blazor.registerCustomEventType`

```razor
Blazor.start({
  circuit: { reconnectionOptions: { retryIntervalMilliseconds: [0, 2000, 10000] } }
}); // now valid for blazor.server.js too
```

> Verification: Blazor.start, IJSObjectReference.DisposeAsync, JSDisconnectedException, ElementReference and Blazor.registerCustomEventType all exist unchanged in 10.0.2, p7 and RC1, so the polish is behavioural.

Sources: https://github.com/dotnet/aspnetcore/pull/64629, https://github.com/dotnet/aspnetcore/pull/64773, https://github.com/dotnet/aspnetcore/pull/63542, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/blazor-custom-event-name-collision.md

<a id="mathml"></a>
### MathML namespace support in interactive rendering

**Release:** Preview 1 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

Interactive rendering now creates <math>, <mrow>, <mi> and other MathML elements with createElementNS and the MathML namespace, the same way it handles SVG, so dynamically rendered formulas display correctly.

**Before (.NET 10):** Interactively rendered MathML was created as plain HTML elements and didn't render as math.

**LOB use case:** Finance, insurance or engineering apps showing live pricing or amortization formulas.

**Demo idea:** An amortization formula that re-renders as the inputs change.

**APIs:** `<math>`, `document.createElementNS`

```razor
<math><mrow><mi>P</mi><mo>=</mo><mfrac><mi>rL</mi><mrow><mn>1</mn><mo>-</mo><msup><mrow><mo>(</mo><mn>1</mn><mo>+</mo><mi>r</mi><mo>)</mo></mrow><mrow><mo>-</mo><mi>n</mi></mrow></msup></mrow></mfrac></mrow></math>
```

Sources: https://github.com/dotnet/aspnetcore/pull/64632

<a id="blazor-hybrid-updates"></a>
### Blazor Hybrid: @rendermode no-op and BlazorWebView caching

**Release:** Preview 6 (rendermode), Preview 7/RC1 (MAUI) · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-rc1

WebViewRenderer treats per-component @rendermode as a no-op instead of throwing NotSupportedException, so shared Razor class libraries work in MAUI, WPF and WinForms Hybrid apps. In RC1, MAUI's BlazorWebView.StaticContentCacheControlProvider can opt local static assets into caching. Preview 7 allows replacing BlazorWebView's platform handler.

**Before (.NET 10):** Components that declared @rendermode threw in Hybrid apps, and all local content was served no-store.

**LOB use case:** One component library shared between a Blazor Web App and a MAUI field-service app.

**APIs:** `WebViewRenderer`, `BlazorWebView.StaticContentCacheControlProvider`, `AddMauiBlazorWebView`

> Verification: BlazorWebView.StaticContentCacheControlProvider (Func<BlazorWebViewStaticContentRequest,string>) and IBlazorWebView.StaticContentCacheControlProvider exist only in Microsoft.AspNetCore.Components.WebView.Maui 11.0.0-rc.1.26451.6, not in 11.0.0-preview.7.26406.9 or 10.0.110. WebViewRenderer is an internal type, so the @rendermode no-op is behavioural. AddMauiBlazorWebView already existed.

Sources: https://github.com/dotnet/aspnetcore/pull/65876, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/dotnetmaui.md

<a id="reliability-and-diagnostics"></a>
### Reliability fixes and clearer diagnostics

**Release:** Preview 1 - RC1 · **Status:** shipped · **Significance:** minor · **Verified:** not-api-checkable

Persisted component state now survives re-executed endpoints and enhanced navigation (RC1). SectionOutlet and SectionContent log misconfiguration warnings, including across render modes (RC1). Circuit initialization errors are clearer when a CircuitHandler dependency can't be resolved. Blazor Server logs event dispatch at Debug level. QuickGrid gives clear errors for mismatched GridSort types. The auto-reload after a server restart and the OwningComponentBase dispose fix were also backported to .NET 10.

**Before (.NET 10):** [PersistentState] values could be lost, causing a double load or flicker, and several misconfigurations failed silently.

**LOB use case:** Faster troubleshooting in large apps, and no data re-query after hydration when moving between dashboard pages.

**APIs:** `[PersistentState]`, `SectionOutlet`, `SectionContent`, `CircuitHandler`, `GridSort<TGridItem>`

> Verification: [PersistentState], SectionOutlet, SectionContent, CircuitHandler and GridSort<TGridItem> all already exist in .NET 10, and the fixes are behavioural.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/67977, https://github.com/dotnet/aspnetcore/pull/64635

<a id="interactive-server-native-aot"></a>
### Interactive Server under Native AOT (narrow) and CircuitOptions.JsonTypeInfoResolvers

**Release:** RC1 (JS-invoked Task fix in RC2) · **Status:** experimental · **Significance:** minor · **Verified:** confirmed-rc1

Narrow fixes let the Aspire Dashboard run as an Interactive Server app under Native AOT. CircuitOptions.JsonTypeInfoResolvers composes source-generated JSON contexts into circuit serialization; it is [Experimental("ASPNETCORE9004")] and hidden from IntelliSense. Blazor still uses reflection for component discovery, so this does not claim general trimming or AOT support.

**Before (.NET 10):** Interactive Server wasn't viable under Native AOT.

**LOB use case:** Niche: small AOT-published internal admin tools.

**APIs:** `CircuitOptions.JsonTypeInfoResolvers`, `ASPNETCORE9004`

> Verification: CircuitOptions.JsonTypeInfoResolvers : IList<IJsonTypeInfoResolver> {get} is RC1-only and absent in p7. It is [Experimental("ASPNETCORE9004")] and [EditorBrowsable(Never)].

Sources: https://github.com/dotnet/aspnetcore/pull/68307, https://github.com/dotnet/aspnetcore/pull/69437

<a id="runtime-async"></a>
### Runtime-async in the ASP.NET Core shared framework

**Release:** Preview 4 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

Shared-framework-only ASP.NET Core libraries and the BCL are compiled with runtime-async on net11.0, so the runtime generates async state machines rather than the compiler. That means fewer allocations and cleaner stack traces for Blazor Server and SSR hosts. WASM and packages that also ship to WASM are excluded. User code can opt in with <Features>runtime-async=on</Features>.

**Before (.NET 10):** Compiler-generated async state machines everywhere, and noisy async stack traces.

**LOB use case:** Clearer exception logs and lower allocation in Blazor Server.

**APIs:** `runtime-async`, `<Features>runtime-async=on</Features>`

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/66200

<a id="native-otel-semconv"></a>
### Native OpenTelemetry HTTP semantic conventions in ASP.NET Core hosting

**Release:** Preview 2 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

The Microsoft.AspNetCore activity now carries OTel semantic-convention tags (http.request.method, url.path, http.response.status_code and others) by default, so OpenTelemetry.Instrumentation.AspNetCore is no longer required. Subscribe with AddSource("Microsoft.AspNetCore"). The SuppressActivityOpenTelemetryData switch turns it off. This is a behavioural breaking change.

**Before (.NET 10):** You needed the OpenTelemetry.Instrumentation.AspNetCore package to get these tags.

**LOB use case:** Simpler observability for Blazor Web App hosts feeding Aspire or Azure Monitor.

**APIs:** `AddSource("Microsoft.AspNetCore")`, `Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData`

```razor
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("Microsoft.AspNetCore").AddOtlpExporter());
```

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview2/aspnetcore.md, https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/http-activity-otel-semconv.md

<a id="ef-core-11"></a>
### EF Core 11 features for LOB grids, reports and Blazor Server

**Release:** Previews 1-7 · **Status:** shipped · **Significance:** solid · **Verified:** confirmed-p7

Query improvements: LINQ FullJoin (translated to FULL JOIN), MaxByAsync and MinByAsync, more translations, and redundant CASTs stripped so indexes are used. Tooling: a parameterless AddPooledDbContextFactory<T>() for Blazor Server's IDbContextFactory pattern, one-step 'dotnet ef database update --add' migrations, and analyzer EF1004 for ToAsyncEnumerable on IQueryable.

**Before (.NET 10):** EF Core 10 had no FullJoin, MaxBy or MinBy translation, AddPooledDbContextFactory needed an options lambda, and migrations took two steps.

**LOB use case:** Budget-vs-actual reconciliation reports, 'top customer' tiles, and faster IQueryable sources for QuickGrid.

**Demo idea:** A reconciliation page showing unmatched rows from both sides, fed into a QuickGrid with FullJoin.

**APIs:** `FullJoin`, `MaxByAsync`, `MinByAsync`, `AddPooledDbContextFactory<TContext>`, `dotnet ef database update --add`, `EF1004`

```razor
var rows = await db.Budgets.FullJoin(db.Actuals,
    b => b.AccountId, a => a.AccountId, (b, a) => new { b, a }).ToListAsync();
services.AddPooledDbContextFactory<AppDb>();
```

Sources: https://github.com/dotnet/EntityFramework.Docs/blob/main/entity-framework/core/what-is-new/ef-core-11.0/whatsnew.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/efcore.md

<a id="sqlclient-entra-extension"></a>
### SqlClient 7: Entra ID auth needs Microsoft.Data.SqlClient.Extensions.Azure

**Release:** Preview 3/4 · **Status:** shipped · **Significance:** minor · **Verified:** confirmed-p7

ASP.NET Core 11 brings in Microsoft.Data.SqlClient 7.x transitively, and it no longer bundles the Entra ID providers. Connection strings using Authentication=Active Directory* fail until you add Microsoft.Data.SqlClient.Extensions.Azure. The Individual Auth templates, including Blazor, add it.

**Before (.NET 10):** Earlier SqlClient versions included Active Directory authentication.

**LOB use case:** Any Azure-hosted Blazor LOB app using managed identity against Azure SQL; a common upgrade surprise.

**APIs:** `Microsoft.Data.SqlClient.Extensions.Azure`, `Authentication=Active Directory Default`

Sources: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/breaking-changes/11/sqlclient-azure-extensions-required.md, https://github.com/dotnet/aspnetcore/pull/66179

<a id="dotnet-watch-tooling"></a>
### dotnet watch, Hot Reload and static web assets improvements

**Release:** Previews 1-6 · **Status:** shipped · **Significance:** minor · **Verified:** not-api-checkable

Hot Reload picks up newly added package and project references without a restart. DOTNET_WATCH_AUTO_RELOAD_WS_PORT pins the browser-refresh port, which helps in containers. dotnet watch integrates with Aspire AppHosts, relaunches after a crash, and warns when Hot Reload is disabled. Static Web Assets gains Asset Groups, and Razor class libraries compiling TypeScript feed their output into fingerprinting and compression.

**Before (.NET 10):** Adding a reference forced a restart, the refresh port was random, and TypeScript in RCLs could break rebuilds.

**LOB use case:** A faster inner loop in dev containers, Aspire solutions and shared component libraries.

**APIs:** `DOTNET_WATCH_AUTO_RELOAD_WS_PORT`, `DOTNET_WATCH_AUTO_RELOAD_WS_HOSTNAME`, `Microsoft.TypeScript.MSBuild`, `MetadataUpdaterSupport`

```razor
export DOTNET_WATCH_AUTO_RELOAD_WS_HOSTNAME=0.0.0.0
export DOTNET_WATCH_AUTO_RELOAD_WS_PORT=6123
dotnet watch
```

> Verification: This is tooling. DOTNET_WATCH_AUTO_RELOAD_WS_PORT and _HOSTNAME already existed in the 10.0.102 dotnet-watch.dll; in p7 and RC1 they moved into Microsoft.DotNet.HotReload.Watch.dll. The MetadataUpdaterSupport MSBuild property already existed. Microsoft.TypeScript.MSBuild is not referenced in the SDK.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/sdk.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview3/sdk.md, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/sdk.md

<a id="blazor-ai-components"></a>
### Blazor AI components (Microsoft.AspNetCore.Components.AI)

**Release:** RC1 (prerelease); removed from the .NET 11 branch for RC2, now an out-of-band 0.x prerelease · **Status:** experimental · **Significance:** solid · **Verified:** confirmed-rc1

Building blocks for agentic UIs over any IChatClient, or over remote agents via AG-UI (AGUIChatClient):
- A streaming chat shell: ChatPage, AgentBoundary, MessageList and MessageInput.
- UIAgent, which maps streamed responses to content blocks.
- BlockRenderer<TBlock> for custom rendering, with typed tool blocks from a [ToolBlock] source generator.
- Frontend tools (RegisterUIAction) and Approve/Reject approval blocks.
- Shared and predictive state via UIAgent<TState>.
- Conversation persistence.

**Before (.NET 10):** Chat UIs were hand-built over IChatClient.GetStreamingResponseAsync, with custom tool-call, approval and state handling.

**LOB use case:** An 'ops copilot' that looks up orders as typed cards, asks for approval before cancel_order, and drafts a PO with a predictive diff the user accepts or rejects.

**Demo idea:** A support-assistant side panel with an order-lookup tool card and an approval gate. Badge it 'prerelease'.

**APIs:** `UIAgent`, `UIAgent<TState>`, `ChatPage`, `AgentBoundary`, `MessageList`, `MessageInput`, `BlockRenderer<TBlock>`, `FunctionApprovalBlock`, `[ToolBlock]`, `UIAgentOptions.RegisterUIAction`, `AGUIChatClient`

```razor
@inject IChatClient ChatClient
<ChatPage Agent="agent" Placeholder="Ask about an order...">
    <MessageListContent>
        <BlockRenderer TBlock="FunctionApprovalBlock" Context="a">
            <button @onclick="a.Approve">Approve @a.ToolName</button>
        </BlockRenderer>
    </MessageListContent>
</ChatPage>
@code { UIAgent agent = default!; protected override void OnInitialized() => agent = new UIAgent(ChatClient); }
```

> Verification: Microsoft.AspNetCore.Components.AI has no p7-build package. 11.0.0-preview.7.26425.128 (an RC1-build drop) has UIAgent, ChatPage, AgentBoundary, MessageList, MessageInput, BlockRenderer<TBlock> and UIAgentOptions. UIAgent<TState>, FunctionApprovalBlock, [ToolBlock] (ToolBlockAttribute) and UIAgentOptions.RegisterUIAction(AIFunction) exist only in the later 0.1.0-preview.1.26466.103 package. AGUIChatClient is not in this package; it lives in Microsoft.Agents.AI.AGUI.

> Source notes: The RC1 notes and Learn present it as a .NET 11 prerelease package. aspnetcore#69385 (2026-09-21) removed it from release/11.0; it now ships from main on the .NET 12 cadence as 0.1.0-preview.x, and API review (#68446) is still pending. Daniel Roth's devblogs post (2026-09-28) gives guidance.

Sources: https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md, https://devblogs.microsoft.com/dotnet/build-agentic-ui-blazor/, https://github.com/dotnet/aspnetcore/pull/68323, https://github.com/dotnet/aspnetcore/pull/69385

<a id="media-components"></a>
### Media components: Image, Video, FileDownload

**Release:** Preview 1 bits (undocumented); separate prerelease package from Preview 6 · **Status:** experimental · **Significance:** minor · **Verified:** confirmed-p7

Image, Video and FileDownload render from a MediaSource (a byte[] or Stream, a MIME type and a cache key). The data streams over JS interop into blob URLs and is cached in browser Cache Storage, with data-state loading/error styling and a --blazor-media-progress CSS variable. In Preview 6 they moved into the Microsoft.AspNetCore.Components.Media package, which will never ship a stable .NET 11 version (prerelease only). No release notes or Learn coverage.

**Before (.NET 10):** You built base64 data URLs, or JS interop with DotNetStreamReference and blob URLs.

**LOB use case:** Employee photos or scanned documents stored in SQL, and 'Export to Excel/PDF' downloads generated from a stream.

**Demo idea:** An optional 'preview' tile showing a varbinary product photo and a generated invoice download.

**APIs:** `Microsoft.AspNetCore.Components.Media.Image`, `Video`, `FileDownload`, `MediaSource`, `MediaContext`

```razor
@using Microsoft.AspNetCore.Components.Media
<Image Source="@(new MediaSource(photoBytes, "image/jpeg", $"emp-{id}"))" alt="Photo" />
<FileDownload Source="@(new MediaSource(stream, "application/pdf", "inv-42"))" FileName="invoice-42.pdf" Text="Download" />
```

> Source notes: Previews 1-5 used the namespace Microsoft.AspNetCore.Components.Web.Media inside Components.Web.

Sources: https://github.com/dotnet/aspnetcore/pull/63540, https://github.com/dotnet/aspnetcore/pull/67130, https://github.com/dotnet/aspnetcore/pull/68061

<a id="components-testing"></a>
### Microsoft.AspNetCore.Components.Testing (Playwright E2E library)

**Release:** Preview 4 code / Preview 5 package (prerelease only) · **Status:** experimental · **Significance:** minor · **Verified:** confirmed-rc1

A cross-render-mode E2E testing library built on Playwright, MSTest (Microsoft Testing Platform) and YARP. It provides ServerFactory, PageTest and UITest, plus WaitForBlazorAsync, WaitForInteractiveAsync and WaitForEnhancedNavigationAsync, tracing, and cross-process service overrides. Like Media, it will never be stabilized in .NET 11 and isn't in the release notes.

**Before (.NET 10):** bUnit for unit tests, and hand-rolled Playwright or Selenium harnesses with no 'wait for interactivity' primitive.

**LOB use case:** E2E regression suites for critical workflows across SSR, Server and WASM.

**APIs:** `PageTest`, `UITest`, `ServerFactory<T>`, `PlaywrightExtensions.WaitForInteractiveAsync`, `PlaywrightExtensions.WaitForBlazorAsync`

> Verification: Microsoft.AspNetCore.Components.Testing 11.0.0-preview.7.26425.128 (RC1 build) has Testing.Playwright.PageTest, UITest, UITestAttribute and Infrastructure.ServerFactory<T>, plus PlaywrightExtensions.WaitForInteractiveAsync(IPage, string) and WaitForBlazorAsync(IPage). The p7-build package (26381) has the Wait* extensions but no PageTest or UITest, and has ServerFixture<T> instead of ServerFactory<T>.

Sources: https://github.com/dotnet/aspnetcore/pull/65958, https://github.com/dotnet/aspnetcore/pull/68061

<a id="asset-path-expansion"></a>
### Compile-time ~/ asset-path expansion in .razor files

**Release:** RC1 (runtime attributes); compiler side expected in the RC2 SDK · **Status:** unclear · **Significance:** minor · **Verified:** not-found

The Razor compiler (language version 11.0) rewrites literal ~/ values into Assets["..."] fingerprinted URLs where the runtime opts in: img/script/source[src], link[href], video[src|poster], and string parameters marked [AssetPath]. New diagnostics are RZ10029 and RZ10030.

**Before (.NET 10):** You wrote <img src="@Assets["images/logo.png"]" /> for fingerprinted assets.

**LOB use case:** Terser markup in component libraries that keeps cache-busting.

**Demo idea:** Show only after verifying on the RC2 or GA SDK: <img src="~/images/logo.png"> renders a fingerprinted URL.

**APIs:** `AssetPathAttribute`, `AcceptsAssetPathAttribute`, `AssetPathAttributes`, `RZ10029`, `RZ10030`

```razor
<img src="~/images/logo.png" />  @* -> Assets[@"images/logo.png"] *@
[Parameter, AssetPath] public string? IconUrl { get; set; }
```

> Verification: The runtime attributes Microsoft.AspNetCore.Components.AssetPathAttribute, AcceptsAssetPathAttribute(element, attribute) and Microsoft.AspNetCore.Components.Web.AssetPathAttributes exist only in RC1. Neither the p7 nor the RC1 SDK Razor compiler contains RZ10029, RZ10030 or any AssetPath logic. A test build with the RC1 SDK left <img src="~/images/logo.png"> unexpanded in the generated code.

> Source notes: The RC1 notes filtered it out because the transform wasn't in the validated RC1 build. github-prs found the compiler change (roslyn#84796) in the VMR rc2 branch and expects it to work first in RC2 (merged-not-released). No published notes confirm it.

Sources: https://github.com/dotnet/aspnetcore/pull/68234, https://github.com/dotnet/roslyn/pull/84796, https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/rc1/aspnetcore.md

<a id="device-bound-sessions"></a>
### Device Bound Session Credentials (DBSC)

**Release:** RC1 (experimental); removed in RC2 · **Status:** cut-or-deferred · **Significance:** minor · **Verified:** confirmed-rc1

RC1 shipped an experimental Microsoft.AspNetCore.Authentication.DeviceBoundSessions package (AddDeviceBoundSession over cookie auth) that binds session refresh to a key held by the browser. RC2 removed it as incomplete (#69462/#69479), and it may return in .NET 12 (#66478).

**Before (.NET 10):** Not available.

**LOB use case:** Cookie-theft resistance for high-value admin apps; don't build a .NET 11 demo on it.

**APIs:** `Microsoft.AspNetCore.Authentication.DeviceBoundSessions`, `AddDeviceBoundSession`

> Verification: The only package is Microsoft.AspNetCore.Authentication.DeviceBoundSessions 0.11.0-rc.1.26425.128; there is no p7 version. It has namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions and DeviceBoundSessionExtensions.AddDeviceBoundSession(AuthenticationBuilder, string scheme[, string displayName][, Action<DeviceBoundSessionOptions>]). Its types are [Experimental("ASP0031")].

> Source notes: Learn (RC1 content) still documents it.

Sources: https://github.com/dotnet/core/blob/release-notes/11.0-rc2/release-notes/11.0/preview/rc2/aspnetcore.md, https://github.com/dotnet/aspnetcore/pull/69479

## Cut or deferred

- Device Bound Session Credentials: shipped experimental in RC1, removed in RC2 (#69479); possibly .NET 12 (#66478).
- Microsoft.AspNetCore.Components.AI: announced in the RC1 notes, removed from release/11.0 (#69385); now ships out of band as a 0.x prerelease from main on the .NET 12 cadence.
- Microsoft.AspNetCore.Components.Media (Image, Video, FileDownload) and Microsoft.AspNetCore.Components.Testing: prerelease-only by design, never stabilized for .NET 11 (#68061).
- Nested routing (#11212) and custom route constraints (#28938): struck from the .NET 11 roadmap, now .NET 12 Planning.
- Event debounce/throttle (#10522), state transitions/animations (#10389), @bind on contenteditable (#9974) and per-page JS logic (#52273): deferred.
- PWA option in the Blazor Web App template (#48935): moved to .NET 12 Planning.
- QuickGrid + EF Core improvements and SSR filtering (#58716), plus the broader built-in component improvements (#58819): deferred; only parts shipped.
- Blazor WebAssembly multithreading: still unsupported (#54365 open); Web Workers are the .NET 11 answer.
- Further Aspire integration (#64888) and debugging WASM launched from the Aspire dashboard (#64891): .NET 12 / Backlog.
- Blazor Server keep-alive/reconnection issues (#48724), SSR performance investigations (#65193) and Server stress-profiling tools (#27457): deferred.
- Full C# unions support (#64599) and union templated child-content syntax (roslyn#85657): .NET 12 / Backlog.
- On main only (.NET 12): analyzers BL0017-BL0019, input parse errors using [Display] names (#68667), restricting JS root component operations (#69240).
- Identity template switch to SignalR auth refresh (#68663): the release/11.0 backport (#68895) was still open on 2026-10-13.
- CoreCLR on WebAssembly stays opt-in and experimental in .NET 11; Mono remains the default. The out-of-process WASM renderer is internal infrastructure behind an env var. Converting Blazor interop to JSImport/JSExport was reverted in Preview 4.
- Backlog, not in .NET 11: prerender:false in child components (#55635), AuthenticationStateProvider in outgoing handlers (#52379), OIDC token refresh in Interactive Server (#55213), Microsoft Identity Platform option in the Blazor Web App template (#51202, still open).

## Breaking changes

- Obsolete Blazor APIs removed (P1, #62755): Router.PreferExactMatches, AddDataAnnotationsValidation and the EnableDataAnnotationsValidation(EditContext) overload, RemoteBrowserFileStreamOptions, WebEventCallbackFactoryEventArgsExtensions, the WebRenderer.RendererId init accessor, SignOutSessionStateManager, AccessTokenResult.RedirectUrl and legacy constructors.
- EditContext.Validate() is [Obsolete] (P7) in favour of ValidateAsync, and throws if async validators are registered.
- Built-in Input* components now render an id attribute automatically (P1), which can affect CSS and test selectors.
- QuickGrid sort and paging controls render as <a> links instead of <button> and use the query string (P5). Restyle a.col-title; renaming a column Title breaks bookmarked sort URLs; several grids on one page need QueryParameterNameOptions; the AppContext switch EnableUrlBasedQuickGridNavigationAndSorting=false restores buttons.
- Virtualize's default OverscanCount changes from 3 to 15 (P3; not on the breaking-changes page). QuickGrid stays at 3.
- WebAssemblyHostBuilder.CreateDefault loads environment variables into IConfiguration, overriding appsettings keys with the same name (P1).
- SSR antiforgery is deferred to middleware, and automatic CSRF protection is on (P6/P7). Templates drop UseAntiforgery; without it, the Razor Components endpoint no longer generates or validates tokens.
- Enhanced navigation no longer emits ResourcePreloader preload links (P1).
- Blazor.registerCustomEventType throws when the event name equals browserEventName (P1).
- Session-backed TempData and [SupplyParameterFromSession] throw InvalidOperationException in static SSR when session middleware is missing (P7). TempData and Session values are restricted to simple types (RC1).
- SignInManager.PasskeySignInAsync enforces PreSignInCheck (confirmation and lockout) (P2).
- UseWebAssemblyDebugging is [Obsolete] (ASPDEPR011) and the WebAssembly.DevServer package is deprecated in favour of the Blazor Gateway (P7).
- New analyzer warnings BL0010-BL0016 are on by default, so upgraded projects see new warnings, which break builds that treat warnings as errors.
- Microsoft.Extensions.Validation was reshaped: AddValidationLocalization and its package removed (P7); IValidatableInfo split into type/property/parameter interfaces with a new ValidationErrors shape (P7); MessageKeyProvider removed (RC1); IValidationMessageFormatter removed in favour of ValidationAttribute.FormatMessage (RC2).
- The BasePath component moves from Microsoft.AspNetCore.Components.Endpoints to Microsoft.AspNetCore.Components (RC2).
- WebAssemblyComponentsOptions.UseCultureFromServer and the AddInteractiveWebAssemblyComponents(options) overload were removed; use the Components:UseCultureFromServer configuration value instead (P6/P7).
- Preview-era renames that break early-preview code: EnvironmentBoundary became EnvironmentView; GetUriWithHash became GetUriWithFragment; InitialIndex and ScrollToIndexAsync became InitialItemIndex and ScrollToItemAsync; VirtualizeAnchorMode.Beginning became Start; WithBrowserConfiguration and BrowserConfiguration became WithBrowserOptions and BrowserOptions (sub-options .InteractiveServer, .StaticServer and .InteractiveWebAssembly); EnableClientValidation became DisableClientValidation; IClientValidationAdapter became IClientValidationRuleProvider; the webworker template became blazorwebworker.
- Microsoft.Data.SqlClient 7 (transitive) needs Microsoft.Data.SqlClient.Extensions.Azure for Entra ID ('Active Directory') authentication.
- The ASP.NET Core hosting Activity emits OTel semantic-convention tags by default (P2). Opt out with Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData.
- The remaining AddRazorRuntimeCompilation overloads are obsolete (ASPDEPR003, RC2). This affects mixed MVC/Razor Pages and Blazor apps.

## Verification notes

Everything was checked against real bits in $FOCUS_SCRATCH; nothing was installed globally.

**What I compared**
- **.NET 10 baseline:** the installed SDK 10.0.102 (packs and shared framework 10.0.2), plus 10.0.2 packages and the 10.0.2 templates.
- **Preview 7:** the installed SDK 11.0.100-preview.7.26381.103 (App.Ref, NETCore.App.Ref, the shared/Microsoft.AspNetCore.App implementation, the Razor compiler and the workload manifests), plus 11.0.0-preview.7.26381.103 packages.
- **RC1:** 11.0.0-rc.1.26425.128 packages, plus the full RC1 SDK tarball (dotnet-sdk-11.0.100-rc.1.26425.128-osx-arm64) extracted into scratch so I could test the Razor compiler and C# behaviour.

**Packages downloaded:** App.Ref, App.Runtime.osx-arm64, App.Internal.Assets (the blazor.web/server JS), Components.WebAssembly (+.Server, .Authentication, .DevServer), QuickGrid, Components.Analyzers, Components.WebView, Extensions.Validation, SignalR.Client.Core, Http.Connections.Client, NETCore.App.Ref, the browser-wasm runtime packs (Mono and CoreCLR), Web.ProjectTemplates/ItemTemplates 11.0, EF Core (+Relational, Analyzers, Design, dotnet-ef), Components.Gateway (+.Cli), Components.Testing, Components.Media, Components.AI, Authentication.DeviceBoundSessions, Components.WebView.Maui, Aspire.Hosting.Blazor, and Microsoft.Data.SqlClient (+.Extensions.Azure).

**How I checked**
- **Public API:** a System.Reflection.Metadata dumper, including Obsolete/Experimental attributes.
- **Implementation:** DLL string searches for config keys and AppContext switches.
- **JS:** greps of blazor.web/server/webassembly.js and dotnet.js.
- **Templates:** greps of the template content.
- **Compiler behaviour:** test compiles on p7 and RC1.

**Things that matter for the showcase**

1. **p7 → RC1 renames.** Code written against p7 will break on RC1:
   - SsrBrowserOptions → StaticServerBrowserOptions
   - WebAssemblyBrowserOptions → InteractiveWebAssemblyBrowserOptions
   - BrowserOptions.Ssr / Server / WebAssembly → .StaticServer / .InteractiveServer / .InteractiveWebAssembly
   - the httpContext.GetBrowserOptions() extension → static BrowserOptions.GetBrowserOptions(HttpContext)
   - ConfigureBrowser.HttpContext is removed
   - ValidationOptions.MessageKeyProvider and ValidationMessageKeyContext are removed
   - Testing: ServerFixture<T> → ServerFactory<T>

2. **RC1-only additions:**
   - AssetPath attributes (but no compiler support yet)
   - CircuitOptions.JsonTypeInfoResolvers (ASPNETCORE9004)
   - the ASP0032 tag on RenderTreeBuilder.SetAttributeValue
   - JsonUnionTypeStructuralClassifier and JsonPolymorphicAttribute.InferClosedTypePolymorphism
   - Blazor circuit auth-refresh wiring in Components.Server
   - HttpConnectionDispatcherOptions.MaximumAuthenticationExpiration
   - the HubConnection AuthenticationRefreshed / AuthenticationRefreshFailed events
   - the use-code-behind item template option
   - MAUI StaticContentCacheControlProvider
   - DeviceBoundSessions

3. **Out-of-band packages versioned "11.0.0-preview.7.26425.128":** Components.AI, Testing and Media come from the RC1 build, not p7. The richer AI surface is only in the later 0.1.0-preview.1.26466.103 (UIAgent<TState>, FunctionApprovalBlock, ToolBlock, RegisterUIAction).

4. **C# 15 on p7:** union, closed and with() need <LangVersion>preview</LangVersion>; RC1 enables them by default.

5. **Not found in p7 or RC1:** circuit auto-pause, the Identity template's switch to auth refresh, and compile-time ~/ expansion.

6. **Verdict convention:** where a feature mixes p7 and RC1 items, the verdict is the earliest build that has all of them. The evidence says which parts already work in p7.
