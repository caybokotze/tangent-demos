using Scheduling.Domain.Appointments;

namespace Scheduling.Application.ViewModels;

/// <summary>What the API returns for one appointment.</summary>
public sealed record AppointmentViewModel
{
    /// <summary>The appointment id.</summary>
    public required Guid Id { get; init; }

    /// <summary>The provider who will see the patient.</summary>
    public required Guid ProviderId { get; init; }

    /// <summary>Who is being seen.</summary>
    public required string PatientName { get; init; }

    /// <summary>When it starts, UTC.</summary>
    public required DateTimeOffset StartsAt { get; init; }

    /// <summary>When it ends, UTC.</summary>
    public required DateTimeOffset EndsAt { get; init; }

    // VIOLATION (TNG0201): `contract ViewModel` declares `forbid using Scheduling.Domain.**`.
    // The wire format now depends on a domain enum: renaming or reordering AppointmentStatus
    // silently changes the API. The fix is `public required string Status { get; init; }` and
    // `Status = appointment.Status.ToString()` at the mapping site.
    /// <summary>Where it is in its life.</summary>
    public required AppointmentStatus Status { get; init; }
}
