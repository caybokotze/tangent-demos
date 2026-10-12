using Scheduling.Domain.Appointments;

namespace Scheduling.Domain.Services;

/// <summary>Decides whether a candidate slot may be taken given what is already booked.</summary>
public interface IOverlapPolicy
{
    /// <summary>True when nothing active in <paramref name="existing"/> touches <paramref name="candidate"/>.</summary>
    bool Allows(TimeSlot candidate, IReadOnlyList<Appointment> existing);
}
