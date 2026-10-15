using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace NorthwindOps.Worker;

// Made by `dotnet new blazorwebworker`: you add your own static methods here.
[SupportedOSPlatform("browser")]
public static partial class WorkerMethods
{
    // xray:web-worker-template
    // Not new: [JSExport] (from .NET 7) lets JavaScript call it, and the template's worker
    // script finds it by name. Only simple types cross: bytes in, a JSON string out.
    [JSExport]
    public static string TotalOrderLines(byte[] csv)
    // /xray:web-worker-template
    {
        var tally = new OrderLineTally();
        using var reader = new StreamReader(new MemoryStream(csv));
        reader.ReadLine(); // skip the header row
        while (reader.ReadLine() is { } line)
            tally.Add(line);
        return tally.ToJson();
    }
}
