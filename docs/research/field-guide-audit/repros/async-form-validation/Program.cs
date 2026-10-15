using AsyncValidationRepro.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<SupplierDirectory>();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

/// <summary>The "database": supplier codes already in use. Lookups take 1.5 s, like the guide's demo.</summary>
public sealed class SupplierDirectory
{
    readonly List<string> codes = ["EXOT-01"];
    public IReadOnlyList<string> Codes { get { lock (codes) return codes.ToList(); } }
    public async Task<bool> IsCodeTakenAsync(string code, CancellationToken ct = default)
    {
        await Task.Delay(1500, ct);
        lock (codes) return codes.Contains(code);
    }
    public void Add(string code) { lock (codes) codes.Add(code); }
}
