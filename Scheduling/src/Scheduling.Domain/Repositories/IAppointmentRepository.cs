using Scheduling.Domain.Appointments;

namespace Scheduling.Domain.Repositories;

/// <summary>The Domain's view of appointment storage. Implemented in Infrastructure.</summary>
public interface IAppointmentRepository
{
    /// <summary>The appointment with this id, or <c>null</c>.</summary>
    Task<Appointment?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Every appointment held by one provider on one UTC day.</summary>
    Task<IReadOnlyList<Appointment>> ForProviderAsync(Guid providerId, DateOnly day, CancellationToken cancellationToken = default);

    /// <summary>Every appointment on one UTC day, ordered by start.</summary>
    Task<IReadOnlyList<Appointment>> ForDayAsync(DateOnly day, CancellationToken cancellationToken = default);

    /// <summary>Stores the aggregate and appends one audit row describing what happened.</summary>
    Task<Appointment> SaveAsync(Appointment appointment, string action, CancellationToken cancellationToken = default);

    /// <summary>The audit trail of one appointment, oldest first.</summary>
    Task<IReadOnlyList<AppointmentAuditEntry>> AuditAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
