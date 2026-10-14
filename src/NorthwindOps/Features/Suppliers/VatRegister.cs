namespace NorthwindOps.Features.Suppliers;

/// <summary>
/// A stand-in for the EU's VIES VAT number service, so the demo doesn't call a real government API.
/// It's slow on purpose, rejects numbers ending in 0, and is "down" for Greek numbers (EL...),
/// so you can see what a failed check looks like.
/// </summary>
public sealed class VatRegister(TimeProvider clock)
{
    public static readonly TimeSpan ResponseTime = TimeSpan.FromSeconds(1.2);

    public async Task<bool> IsRegisteredAsync(string vatNumber, CancellationToken cancellationToken)
    {
        await Task.Delay(ResponseTime, clock, cancellationToken);

        if (vatNumber.StartsWith("EL", StringComparison.OrdinalIgnoreCase))
        {
            throw new TimeoutException("The VAT register didn't answer for Greece (EL).");
        }

        return !vatNumber.EndsWith('0');
    }
}
