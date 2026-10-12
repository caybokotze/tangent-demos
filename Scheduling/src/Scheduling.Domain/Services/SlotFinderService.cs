using Scheduling.Domain.Appointments;
using Scheduling.Domain.Providers;

namespace Scheduling.Domain.Services;

/// <summary>
/// CONFORMING to `contract DomainService`: sealed, behind <see cref="ISlotFinderService"/>,
/// and stateless — its only field is the readonly policy it was given.
/// </summary>
public sealed class SlotFinderService(IOverlapPolicy overlapPolicy) : ISlotFinderService
{
    private readonly IOverlapPolicy _overlapPolicy = overlapPolicy;

    /// <inheritdoc/>
    public IReadOnlyList<TimeSlot> FindFreeSlots(
        IReadOnlyList<Availability> workingHours,
        IReadOnlyList<Appointment> existing,
        DateOnly day,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(workingHours);
        ArgumentNullException.ThrowIfNull(existing);

        if (duration <= TimeSpan.Zero)
        {
            throw new SchedulingRuleViolationException($"An appointment must last longer than nothing; got {duration}.");
        }

        var free = new List<TimeSlot>();

        foreach (Availability window in workingHours)
        {
            if (window.Day != day.DayOfWeek)
            {
                continue;
            }

            var cursor = new DateTimeOffset(day.ToDateTime(window.StartsAt), TimeSpan.Zero);
            DateTimeOffset closes = new(day.ToDateTime(window.EndsAt), TimeSpan.Zero);

            while (cursor + duration <= closes)
            {
                var candidate = new TimeSlot(cursor, cursor + duration);
                if (_overlapPolicy.Allows(candidate, existing))
                {
                    free.Add(candidate);
                }

                cursor += duration;
            }
        }

        free.Sort(static (left, right) => left.Start.CompareTo(right.Start));
        return free;
    }
}
