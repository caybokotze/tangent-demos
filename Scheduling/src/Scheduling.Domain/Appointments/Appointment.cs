using Tangent;

namespace Scheduling.Domain.Appointments;

/// <summary>
/// One booking of one provider for one patient. CONFORMING to `contract Entity` and
/// `contract AggregateRoot`: created through a factory, changed only through behaviour,
/// and every command returns a result rather than <c>void</c>.
/// </summary>
[Tangent(Contracts.Entity)]
public sealed class Appointment : AggregateRoot<Guid>
{
    private Appointment(Guid id, Guid providerId, string patientName, TimeSlot slot)
        : base(id)
    {
        ProviderId = providerId;
        PatientName = patientName;
        Slot = slot;
        Status = AppointmentStatus.Scheduled;
    }

    /// <summary>The provider who will see the patient.</summary>
    public Guid ProviderId { get; }

    /// <summary>Who is being seen.</summary>
    public string PatientName { get; }

    /// <summary>When the appointment happens.</summary>
    public TimeSlot Slot { get; private set; }

    /// <summary>Where the appointment is in its life.</summary>
    public AppointmentStatus Status { get; private set; }

    /// <summary>Why it was called off, when it was.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Books a provider for a slot. The invariants: a patient must be named, the slot must be
    /// in the future, and it must not overlap anything already booked for that provider.
    /// </summary>
    public static Appointment Schedule(
        Guid providerId,
        string patientName,
        TimeSlot slot,
        IReadOnlyList<Appointment> existingForProvider,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientName);
        ArgumentNullException.ThrowIfNull(existingForProvider);
        ArgumentNullException.ThrowIfNull(clock);

        if (slot.Start < clock.UtcNow)
        {
            throw new SchedulingRuleViolationException($"Cannot book {slot.Start:O}: it is in the past.");
        }

        foreach (Appointment existing in existingForProvider)
        {
            if (existing.IsActive && existing.Slot.Overlaps(slot))
            {
                throw new SchedulingRuleViolationException(
                    $"Provider {providerId} is already booked from {existing.Slot.Start:O} to {existing.Slot.End:O}.");
            }
        }

        return new Appointment(Guid.NewGuid(), providerId, patientName, slot);
    }

    /// <summary>Moves the appointment to a new slot, under the same invariants as booking it.</summary>
    public Appointment Reschedule(TimeSlot slot, IReadOnlyList<Appointment> existingForProvider, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(existingForProvider);
        ArgumentNullException.ThrowIfNull(clock);

        if (!IsActive)
        {
            throw new SchedulingRuleViolationException($"Appointment {Id} is {Status} and cannot be rescheduled.");
        }

        if (slot.Start < clock.UtcNow)
        {
            throw new SchedulingRuleViolationException($"Cannot move to {slot.Start:O}: it is in the past.");
        }

        foreach (Appointment existing in existingForProvider)
        {
            if (existing.Id != Id && existing.IsActive && existing.Slot.Overlaps(slot))
            {
                throw new SchedulingRuleViolationException(
                    $"Provider {ProviderId} is already booked from {existing.Slot.Start:O} to {existing.Slot.End:O}.");
            }
        }

        Slot = slot;
        Status = AppointmentStatus.Rescheduled;
        return this;
    }

    /// <summary>Calls the appointment off, freeing its slot.</summary>
    public Appointment Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == AppointmentStatus.Cancelled)
        {
            throw new SchedulingRuleViolationException($"Appointment {Id} is already cancelled.");
        }

        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        return this;
    }

    /// <summary>True while the appointment still holds its slot.</summary>
    public bool IsActive => Status != AppointmentStatus.Cancelled;
}
