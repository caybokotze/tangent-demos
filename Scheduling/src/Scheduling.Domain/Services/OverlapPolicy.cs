using Scheduling.Domain.Appointments;
using Tangent;

namespace Scheduling.Domain.Services;

/// <summary>
/// The house rule: back-to-back is fine, overlapping is not, and a cancelled appointment
/// holds nothing. CONFORMING to `contract DomainService`.
/// </summary>
[Tangent(Contracts.DomainService)]
public sealed class OverlapPolicy : IOverlapPolicy
{
    /// <inheritdoc/>
    public bool Allows(TimeSlot candidate, IReadOnlyList<Appointment> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);

        foreach (Appointment appointment in existing)
        {
            if (appointment.IsActive && appointment.Slot.Overlaps(candidate))
            {
                return false;
            }
        }

        return true;
    }
}
