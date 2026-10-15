using Microsoft.AspNetCore.Components.Server.Circuits;

namespace NorthwindOps.Components.Guide.Demos.CircuitPause;

// Registered as a scoped CircuitHandler, so each circuit gets its own tracker.
// A component that injects it gets its own circuit, never another visitor's.
public sealed class CircuitTracker(CircuitBoard board) : CircuitHandler
{
    // xray:server-initiated-circuit-pause
    // Not new: a circuit handler has always been handed each Circuit when it opens,
    // before any of its components render. In .NET 11 a Circuit can ask its tab to pause.
    public Circuit? Circuit { get; private set; }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancel)
    {
        Circuit = circuit;
        board.Opened();
        return Task.CompletedTask;
    }
    // /xray:server-initiated-circuit-pause

    // A pause closes the circuit, just as closing the tab does. Resuming opens a new one.
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancel)
    {
        board.Closed();
        return Task.CompletedTask;
    }
}
