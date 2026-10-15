namespace NorthwindOps.Components.Guide.Demos.WasmCulture;

public static class WasmCultureSetup
{
    /// <summary>Called from Program.cs. What makes WebAssembly start in the server's culture.</summary>
    public static void UseCultureFromServer(this WebApplicationBuilder builder)
    {
        // xray:wasm-culture-from-server
        // The switch. It's already on once AddLocalization() is registered, as it is in this app;
        // this says so outright. Set it to "false" and WebAssembly uses the browser's language again.
        builder.Configuration.AddInMemoryCollection([new("Components:UseCultureFromServer", "true")]);
        // /xray:wasm-culture-from-server
    }
}
