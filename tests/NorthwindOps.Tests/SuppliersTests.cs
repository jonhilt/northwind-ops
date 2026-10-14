using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthwindOps.Data;
using NorthwindOps.Features.Suppliers;

namespace NorthwindOps.Tests;

/// <summary>The Suppliers screen over HTTP: the list, the static SSR form and its posts, and the live form's shell.</summary>
public partial class SuppliersTests(AppFactory app) : IClassFixture<AppFactory>
{
    readonly HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // ── The list ──

    [Fact]
    public async Task List_headers_come_from_the_form_models_display_names()
    {
        var html = await GetHtmlAsync("/suppliers");

        Assert.Equal(["Supplier code", "Company name", "Main contact", "City", "Country", "Phone", "Status"], Headers(html));
        Assert.Contains("EXOT-01", html);
        Assert.DoesNotContain("<!--Blazor:", html); // static SSR, no circuit
    }

    [Fact]
    public async Task List_headers_follow_the_culture_in_the_query_string()
    {
        var html = await GetHtmlAsync("/suppliers?culture=de-DE");

        Assert.Equal(["Lieferantencode", "Firmenname", "Hauptansprechpartner", "Ort", "Land", "Telefon", "Status"], Headers(html));
        Assert.Contains("Neuer Lieferant", html);
    }

    [Fact]
    public async Task The_culture_switch_only_applies_to_supplier_pages()
    {
        var html = await GetHtmlAsync("/orders?culture=de-DE");

        Assert.Matches(@"\$[\d,]+\.\d\d", html); // still US dollars, not euros
    }

    // ── The static form ──

    [Fact]
    public async Task Labels_and_inputs_are_linked_by_generated_ids()
    {
        var html = await GetHtmlAsync("/suppliers/new");

        Assert.Contains("""<label for="Supplier_Company">Company name</label>""", html);
        Assert.Matches("""<input[^>]*id="Supplier_Company" name="Supplier.Company""", html);
        Assert.Contains("""<label for="Supplier_Address_City">City</label>""", html);
        Assert.Matches("""<input[^>]*id="Supplier_Address_City" name="Supplier.Address.City""", html);
        Assert.Matches("""<select[^>]*id="Supplier_Address_Country" name="Supplier.Address.Country""", html);
    }

    [Fact]
    public async Task Static_form_carries_client_validation_rules_for_every_field_including_nested_ones()
    {
        var rules = ClientRules(await GetHtmlAsync("/suppliers/new"));

        Assert.Equal(
            ["Supplier.Company", "Supplier.Website", "Supplier.Code", "Supplier.VatNumber", "Supplier.Contact", "Supplier.Email", "Supplier.Phone",
             "Supplier.Address.Street", "Supplier.Address.PostalCode", "Supplier.Address.City", "Supplier.Address.Country"],
            rules.Keys);
        Assert.Equal(["required", "suppliercode"], rules["Supplier.Code"].Select(r => r.Name));
        Assert.Equal(new Dictionary<string, string> { ["letters"] = "4", ["digits"] = "2" }, rules["Supplier.Code"][1].Params);
        Assert.Equal("Company name is required.", rules["Supplier.Company"][0].Message);

        // The uniqueness and VAT register checks are async, so they only ever run on the server.
        Assert.Equal(["regex"], rules["Supplier.VatNumber"].Select(r => r.Name));
    }

    [Fact]
    public async Task Client_validation_messages_are_localized_on_the_server()
    {
        var rules = ClientRules(await GetHtmlAsync("/suppliers/new?culture=de-DE"));

        Assert.Equal("Firmenname ist ein Pflichtfeld.", rules["Supplier.Company"][0].Message);
        Assert.Equal("Firmenname darf höchstens 40 Zeichen lang sein.", rules["Supplier.Company"][1].Message);
        // A member-specific key wins over the general ones.
        Assert.StartsWith("Die USt-IdNr. beginnt mit einem Ländercode", rules["Supplier.VatNumber"][0].Message);
        // SupplierCodeAttribute.FormatMessage fills in the template's {1}, {2} and {3}.
        Assert.Equal("Der Lieferantencode muss wie ABCD-01 aussehen: 4 Großbuchstaben, ein Bindestrich und 2 Ziffern.", rules["Supplier.Code"][1].Message);
    }

    [Fact]
    public async Task Contact_rows_get_rules_once_added()
    {
        var response = await PostFormAsync("/suppliers/new", new() { ["Supplier.Company"] = "Acme Foods", ["Intent"] = "add-contact" });
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("""value="Acme Foods""", html); // what was typed survives the round trip
        Assert.Contains("""<label for="Supplier_Contacts[0]_Name">Name</label>""", html);
        Assert.Equal(["required", "length"], ClientRules(html)["Supplier.Contacts[0].Name"].Select(r => r.Name));
        Assert.Equal(["email", "length"], ClientRules(html)["Supplier.Contacts[0].Email"].Select(r => r.Name));
    }

    [Fact]
    public async Task Saving_waits_for_the_async_checks_and_reports_a_taken_code()
    {
        var response = await PostFormAsync("/suppliers/new", ValidSupplier(code: "EXOT-01", vat: "GB123456780"));
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Supplier code is already used by another supplier."], Messages(html, "Supplier.Code"));
        Assert.Equal(["The EU VAT register has no record of this VAT number."], Messages(html, "Supplier.VatNumber"));
    }

    [Fact]
    public async Task Server_messages_are_localized_too()
    {
        var response = await PostFormAsync("/suppliers/new?culture=de-DE", ValidSupplier(code: "EXOT-01"));
        var html = await ReadHtmlAsync(response);

        Assert.Equal(["Lieferantencode wird bereits von einem anderen Lieferanten verwendet."], Messages(html, "Supplier.Code"));
    }

    [Fact]
    public async Task Nested_address_and_contacts_are_validated_on_the_server()
    {
        var form = ValidSupplier(code: "NEST-01");
        form.Remove("Supplier.Address.Street");
        form["Supplier.Contacts[0].Email"] = "not-an-email";

        var html = await ReadHtmlAsync(await PostFormAsync("/suppliers/new", form));

        Assert.Equal(["Street is required."], Messages(html, "Supplier.Address.Street"));
        Assert.Equal(["Name is required."], Messages(html, "Supplier.Contacts[0].Name"));
        Assert.Equal(["Email isn't a valid email address."], Messages(html, "Supplier.Contacts[0].Email"));
        Assert.Empty(Messages(html, "Supplier.Company"));
    }

    [Fact]
    public async Task A_check_that_fails_is_reported_as_faulted_and_nothing_is_saved()
    {
        var response = await PostFormAsync("/suppliers/new", ValidSupplier(code: "GREK-01", vat: "EL123456789"));
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("the VAT register didn't answer", html);
        Assert.False(await SupplierExistsAsync("GREK-01"));
    }

    [Fact]
    public async Task Blank_optional_fields_pass_on_the_server_as_they_do_in_the_browser()
    {
        // A static form posts an empty input as "", which [Phone], [Url] and [EmailAddress] reject;
        // the browser's rules skip empty values, so the server has to treat blank as null too.
        var form = ValidSupplier(code: "BLNK-01");
        form["Supplier.Phone"] = "";
        form["Supplier.Website"] = "";
        form["Supplier.Contacts[0].Name"] = "Sam Patel";
        form["Supplier.Contacts[0].Email"] = "";

        var response = await PostFormAsync("/suppliers/new", form);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        await using var db = await DbAsync();
        var saved = await db.Set<Supplier>().SingleAsync(s => s.Code == "BLNK-01");
        Assert.Null(saved.Phone);
    }

    [Fact]
    public async Task A_valid_supplier_is_saved_with_its_contacts_and_the_list_highlights_it()
    {
        var form = ValidSupplier(code: "ACME-01", vat: "GB123456789");
        form["Supplier.Contacts[0].Name"] = "Sam Patel";
        form["Supplier.Contacts[0].Role"] = "Accounts";

        var response = await PostFormAsync("/suppliers/new", form);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.Matches(@"/suppliers\?added=\d+$", location);

        await using var db = await DbAsync();
        var saved = await db.Set<Supplier>().SingleAsync(s => s.Code == "ACME-01");
        Assert.Equal(SupplierStatus.Active, saved.Status);
        Assert.Equal("Leeds", saved.City);
        Assert.Equal("Sam Patel", (await db.Set<SupplierContact>().SingleAsync(c => c.SupplierId == saved.Id)).Name);

        var list = await GetHtmlAsync(location);
        Assert.Contains("Saved Acme Foods.", list);
        Assert.Contains("""class="just-added""", list);
    }

    [Fact]
    public async Task Saving_in_german_keeps_the_language_on_the_way_back_to_the_list()
    {
        var response = await PostFormAsync("/suppliers/new?culture=de-DE", ValidSupplier(code: "DEUT-01"));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.EndsWith("&culture=de-DE", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task A_draft_skips_validation_but_needs_a_company_name()
    {
        var html = await ReadHtmlAsync(await PostFormAsync("/suppliers/new", new() { ["Supplier.Code"] = "nope", ["Intent"] = "draft" }));

        Assert.Equal(["A draft needs at least a company name."], Messages(html, "Supplier.Company"));
        Assert.Empty(Messages(html, "Supplier.Code"));
    }

    [Fact]
    public async Task A_draft_is_saved_without_a_code_that_is_already_taken()
    {
        var response = await PostFormAsync("/suppliers/new", new() { ["Supplier.Company"] = "Half Done Ltd", ["Supplier.Code"] = "EXOT-01", ["Intent"] = "draft" });

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("codeDropped=true", response.Headers.Location!.ToString());

        await using var db = await DbAsync();
        var draft = await db.Set<Supplier>().SingleAsync(s => s.Company == "Half Done Ltd");
        Assert.Equal(SupplierStatus.Draft, draft.Status);
        Assert.Null(draft.Code);
    }

    // ── The live form ──

    [Fact]
    public async Task Live_form_is_an_interactive_server_island_on_a_static_page()
    {
        var html = await GetHtmlAsync("/suppliers/new/live");

        Assert.Contains("""<!--Blazor:{"type":"server""", html);
        Assert.DoesNotContain("blazor-client-validation-data", html); // validated by the circuit, not by blazor.web.js
        Assert.Contains("""<label for="Supplier_Code">Supplier code</label>""", html);
    }

    // ── Async validation through EditContext, as the live form uses it ──

    [Fact]
    public async Task Editing_the_code_starts_a_pending_check_that_ends_in_a_message()
    {
        UseEnglish();
        using var scope = app.Services.CreateScope();
        var model = new SupplierForm { Code = "EXOT-01" };
        var form = new EditContext(model);
        using var validation = form.EnableDataAnnotationsValidation(scope.ServiceProvider);

        form.NotifyFieldChanged(form.Field(nameof(SupplierForm.Code)));

        Assert.True(form.IsValidationPending(() => model.Code));
        Assert.Contains("pending", form.FieldCssClass(() => model.Code));
        await WaitUntilAsync(() => !form.IsValidationPending(() => model.Code));
        Assert.Equal(["Supplier code is already used by another supplier."], form.GetValidationMessages(() => model.Code));
    }

    [Fact]
    public async Task A_check_that_throws_marks_the_field_faulted_rather_than_invalid()
    {
        using var scope = app.Services.CreateScope();
        var model = new SupplierForm { VatNumber = "EL123456789" };
        var form = new EditContext(model);
        using var validation = form.EnableDataAnnotationsValidation(scope.ServiceProvider);

        form.NotifyFieldChanged(form.Field(nameof(SupplierForm.VatNumber)));
        await WaitUntilAsync(() => !form.IsValidationPending(() => model.VatNumber));

        Assert.True(form.IsValidationFaulted(() => model.VatNumber));
        Assert.Contains("faulted", form.FieldCssClass(() => model.VatNumber));
        Assert.Empty(form.GetValidationMessages(() => model.VatNumber));
    }

    [Fact]
    public async Task ValidateAsync_walks_the_nested_graph()
    {
        UseEnglish();
        using var scope = app.Services.CreateScope();
        var model = new SupplierForm { Company = "Acme Foods", Code = "ABCD-12", Contact = "Jo", Email = "jo@acme.example" };
        model.Contacts.Add(new SupplierContactForm { Email = "nope" });
        var form = new EditContext(model);
        using var validation = form.EnableDataAnnotationsValidation(scope.ServiceProvider);

        Assert.False(await form.ValidateAsync());

        Assert.Contains("Street is required.", form.GetValidationMessages(new FieldIdentifier(model.Address, nameof(SupplierAddressForm.Street))));
        Assert.Contains("Name is required.", form.GetValidationMessages(new FieldIdentifier(model.Contacts[0], nameof(SupplierContactForm.Name))));
        Assert.False(form.IsValidationFaulted());
    }

    // ── Rules ──

    [Theory]
    [InlineData("EXOT-01", true)]
    [InlineData("", true)] // [Required] handles empty values
    [InlineData("exot-01", false)]
    [InlineData("EXO-01", false)]
    [InlineData("EXOT-1", false)]
    [InlineData("EXOT-00", false)]
    public void Supplier_codes_are_four_capitals_a_dash_and_two_digits(string code, bool valid)
    {
        Assert.Equal(valid, new SupplierCodeAttribute().IsValid(code));
    }

    [Fact]
    public void Supplier_code_messages_describe_the_configured_shape()
    {
        var attribute = new SupplierCodeAttribute { Letters = 3, Digits = 3 };

        Assert.Equal("Code must look like ABC-001.", attribute.FormatErrorMessage("Code"));
        Assert.Equal("Code: 3 letters, 3 digits, like ABC-001", attribute.FormatMessage("{0}: {1} letters, {2} digits, like {3}", "Code"));
    }

    [Fact]
    public void Async_attributes_refuse_to_validate_synchronously()
    {
        var context = new ValidationContext(new SupplierForm());

        Assert.Throws<NotSupportedException>(() => new UniqueSupplierCodeAttribute().GetValidationResult("ABCD-01", context));
    }

    [Fact]
    public void Every_english_text_has_a_german_translation()
    {
        var english = ResourceKeys(CultureInfo.InvariantCulture); // SupplierText.resx
        var german = ResourceKeys(CultureInfo.GetCultureInfo("de")); // SupplierText.de.resx

        Assert.NotEmpty(english);
        Assert.Equal(english.Keys.Order(), german.Keys.Order());
        Assert.Equal("Firmenname", german[nameof(SupplierText.Company)]);
    }

    // ── Helpers ──

    static Dictionary<string, string> ValidSupplier(string code, string? vat = null)
    {
        var form = new Dictionary<string, string>
        {
            ["Supplier.Company"] = "Acme Foods",
            ["Supplier.Code"] = code,
            ["Supplier.Contact"] = "Jo Bloggs",
            ["Supplier.Email"] = "jo@acme.example",
            ["Supplier.Address.Street"] = "1 Market Road",
            ["Supplier.Address.City"] = "Leeds",
            ["Supplier.Address.PostalCode"] = "LS1 4AB",
            ["Supplier.Address.Country"] = "UK",
        };
        if (vat is not null)
        {
            form["Supplier.VatNumber"] = vat;
        }
        return form;
    }

    async Task<HttpResponseMessage> PostFormAsync(string url, Dictionary<string, string> fields)
    {
        var page = await GetHtmlAsync(url);
        fields["__RequestVerificationToken"] = AntiforgeryToken().Match(page).Groups[1].Value;
        fields["_handler"] = "new-supplier";
        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    async Task<string> GetHtmlAsync(string url) => await ReadHtmlAsync(await client.GetAsync(url));

    static async Task<string> ReadHtmlAsync(HttpResponseMessage response) => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    async Task<NorthwindDb> DbAsync() => await app.Services.GetRequiredService<IDbContextFactory<NorthwindDb>>().CreateDbContextAsync();

    async Task<bool> SupplierExistsAsync(string code)
    {
        await using var db = await DbAsync();
        return await db.Set<Supplier>().AnyAsync(s => s.Code == code);
    }

    // Messages come from SupplierText.resx in whatever the current UI culture is.
    static void UseEnglish() => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

    static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < timeout, "Timed out waiting for validation to finish.");
            await Task.Delay(50);
        }
    }

    static List<string> Headers(string html) => [.. HeaderText().Matches(html).Select(m => Regex.Replace(m.Groups[1].Value, "<[^>]+>", "").Trim())];

    static List<string> Messages(string html, string field) =>
        [.. ValidationMessage().Matches(html).Where(m => m.Groups["field"].Value == field && m.Groups["text"].Value.Length > 0).Select(m => m.Groups["text"].Value)];

    record ClientRule(string Name, string Message, Dictionary<string, string> Params);

    static Dictionary<string, List<ClientRule>> ClientRules(string decodedHtml)
    {
        var json = ClientRulesPayload().Match(decodedHtml).Groups[1].Value;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("fields").EnumerateArray().ToDictionary(
            field => field.GetProperty("name").GetString()!,
            field => field.GetProperty("rules").EnumerateArray().Select(rule => new ClientRule(
                rule.GetProperty("name").GetString()!,
                rule.GetProperty("message").GetString()!,
                rule.TryGetProperty("params", out var p) ? p.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString()!) : [])).ToList());
    }

    static Dictionary<string, string> ResourceKeys(CultureInfo culture)
    {
        var resources = new System.Resources.ResourceManager(typeof(SupplierText));
        var set = resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return set.Cast<System.Collections.DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="([^"]+)""")]
    private static partial Regex AntiforgeryToken();

    [GeneratedRegex("""<div class="col-header-content"[^>]*>(.*?)</div></th>""")]
    private static partial Regex HeaderText();

    [GeneratedRegex("""<div class="validation-message"(?: data-valmsg-for="(?<field>[^"]+)")?[^>]*>(?<text>[^<]*)</div>""")]
    private static partial Regex ValidationMessage();

    [GeneratedRegex("""<blazor-client-validation-data data-rules="(.*?)"></blazor-client-validation-data>""", RegexOptions.Singleline)]
    private static partial Regex ClientRulesPayload();
}
