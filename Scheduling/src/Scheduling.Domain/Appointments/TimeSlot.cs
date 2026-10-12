namespace Scheduling.Domain.Appointments;

/// <summary>
/// A half-open interval <c>[Start, End)</c> in UTC. A value object: two slots with the same
/// bounds are the same slot, and touching slots do not overlap.
/// </summary>
public readonly record struct TimeSlot
{
    /// <summary>Creates a slot, rejecting one that does not move forward in time.</summary>
    public TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new SchedulingRuleViolationException($"A time slot must end after it starts; got {start:O} to {end:O}.");
        }

        Start = start;
        End = end;
    }

    /// <summary>When the slot opens, inclusive.</summary>
    public DateTimeOffset Start { get; }

    /// <summary>When the slot closes, exclusive.</summary>
    public DateTimeOffset End { get; }

    /// <summary>How long the slot lasts.</summary>
    public TimeSpan Duration => End - Start;

    /// <summary>True when this slot and <paramref name="other"/> share any instant.</summary>
    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;

    /// <summary>The same-length slot starting at <paramref name="start"/>.</summary>
    public TimeSlot MoveTo(DateTimeOffset start) => new(start, start + Duration);

    /// <inheritdoc/>
    public override string ToString() => $"{Start:O}/{End:O}";
}
