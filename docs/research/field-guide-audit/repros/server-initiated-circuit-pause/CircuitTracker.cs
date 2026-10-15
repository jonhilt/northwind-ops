using Microsoft.AspNetCore.Components.Server.Circuits;

namespace PauseRepro;

// Scoped CircuitHandler: one per circuit, so a component that injects it gets its own Circuit.
public sealed class CircuitTracker : CircuitHandler
{
    public Circuit? Circuit { get; private set; }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        Circuit = circuit;
        Console.WriteLine($"[repro] circuit {circuit.Id[..6]} opened");
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        Console.WriteLine($"[repro] circuit {circuit.Id[..6]} closed");
        return Task.CompletedTask;
    }
}
