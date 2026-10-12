using Scheduling.Domain.Appointments;
using Scheduling.Domain.Providers;

namespace Scheduling.Domain.Services;

/// <summary>Turns "when is this provider free?" into a list of bookable slots.</summary>
public interface ISlotFinderService
{
    /// <summary>
    /// Every slot of <paramref name="duration"/> on <paramref name="day"/> that falls inside one of
    /// <paramref name="workingHours"/> and is allowed by the overlap policy, on a grid of
    /// <paramref name="duration"/> from each window's start.
    /// </summary>
    IReadOnlyList<TimeSlot> FindFreeSlots(
        IReadOnlyList<Availability> workingHours,
        IReadOnlyList<Appointment> existing,
        DateOnly day,
        TimeSpan duration);
}
