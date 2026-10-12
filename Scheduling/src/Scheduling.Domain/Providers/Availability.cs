using Scheduling.Domain.Appointments;

namespace Scheduling.Domain.Providers;

/// <summary>
/// One weekly working window for a provider: "Tuesdays, 09:00 to 17:00". Times are UTC
/// wall-clock times; the day is the day of the week the window repeats on.
/// </summary>
public sealed class Availability : Entity<Guid>
{
    private Availability(Guid id, DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt)
        : base(id)
    {
        if (endsAt <= startsAt)
        {
            throw new SchedulingRuleViolationException($"A working window must end after it starts; got {startsAt} to {endsAt}.");
        }

        Day = day;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    /// <summary>The day of the week this window repeats on.</summary>
    public DayOfWeek Day { get; }

    /// <summary>When the provider starts working.</summary>
    public TimeOnly StartsAt { get; }

    // VIOLATION (TNG0107): `contract Entity` declares `properties { forbid public set }`.
    // Someone needed to stretch a provider's day and reached for the shortest edit: a public
    // setter. The fix is `private set` plus an `ExtendTo(TimeOnly)` behaviour on the entity.
    /// <summary>When the provider stops working.</summary>
    public TimeOnly EndsAt { get; set; }

    /// <summary>Opens a weekly working window.</summary>
    public static Availability On(DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt) =>
        new(Guid.NewGuid(), day, startsAt, endsAt);

    /// <summary>True when <paramref name="slot"/> falls entirely inside this window.</summary>
    public bool Covers(TimeSlot slot) =>
        slot.Start.UtcDateTime.DayOfWeek == Day &&
        TimeOnly.FromDateTime(slot.Start.UtcDateTime) >= StartsAt &&
        TimeOnly.FromDateTime(slot.End.UtcDateTime) <= EndsAt &&
        slot.Start.UtcDateTime.Date == slot.End.UtcDateTime.Date;
}
