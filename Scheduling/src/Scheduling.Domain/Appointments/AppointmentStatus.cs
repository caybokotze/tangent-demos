namespace Scheduling.Domain.Appointments;

/// <summary>Where an appointment is in its short life.</summary>
public enum AppointmentStatus
{
    /// <summary>Booked and expected to happen.</summary>
    Scheduled = 0,

    /// <summary>Booked, then moved to a different slot at least once.</summary>
    Rescheduled = 1,

    /// <summary>Called off. The slot is free again.</summary>
    Cancelled = 2,
}
