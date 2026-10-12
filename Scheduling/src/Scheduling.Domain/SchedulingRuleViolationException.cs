namespace Scheduling.Domain;

/// <summary>
/// Thrown when a caller asks the Domain to do something its invariants forbid: booking in the
/// past, double-booking a provider, cancelling twice.
/// </summary>
public sealed class SchedulingRuleViolationException : Exception
{
    /// <summary>Creates the exception.</summary>
    public SchedulingRuleViolationException(string message)
        : base(message)
    {
    }
}
