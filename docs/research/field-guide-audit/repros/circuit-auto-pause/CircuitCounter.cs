using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AutoPauseRepro;

public class CircuitCounter
{
    int open, opened, closed;
    public int Open => open;
    public int Opened => opened;
    public int Closed => closed;
    public void Up() { Interlocked.Increment(ref open); Interlocked.Increment(ref opened); }
    public void Down() { Interlocked.Decrement(ref open); Interlocked.Increment(ref closed); }
}

public class CountingHandler(CircuitCounter counter, ILogger<CountingHandler> log) : CircuitHandler
{
    public string? Id { get; private set; }
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        Id = circuit.Id; counter.Up();
        log.LogInformation("circuit {Id} opened, open now {Open}", circuit.Id[..6], counter.Open);
        return Task.CompletedTask;
    }
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        counter.Down();
        log.LogInformation("circuit {Id} closed, open now {Open}", circuit.Id[..6], counter.Open);
        return Task.CompletedTask;
    }
}
