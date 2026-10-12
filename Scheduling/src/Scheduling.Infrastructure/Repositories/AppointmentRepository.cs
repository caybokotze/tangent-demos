using System.Collections.Concurrent;
using Scheduling.Domain.Appointments;
using Scheduling.Domain.Repositories;
using Tangent;

namespace Scheduling.Infrastructure.Repositories;

/// <summary>
/// CONFORMING to `contract Repository`: sealed, behind <see cref="IAppointmentRepository"/>,
/// which lives in the Domain. In-memory and thread-safe — the demo needs no database.
/// </summary>
[Tangent(Contracts.Repository)]
public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly ConcurrentDictionary<Guid, Appointment> _appointments = new();
    private readonly ConcurrentQueue<AppointmentAuditEntry> _audit = new();

    /// <inheritdoc/>
    public Task<Appointment?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_appointments.TryGetValue(id, out Appointment? appointment) ? appointment : null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Appointment>> ForProviderAsync(
        Guid providerId,
        DateOnly day,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(
        [
            .. _appointments.Values
                .Where(appointment => appointment.ProviderId == providerId && OnDay(appointment, day))
                .OrderBy(appointment => appointment.Slot.Start)
        ]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Appointment>> ForDayAsync(DateOnly day, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(
        [
            .. _appointments.Values
                .Where(appointment => OnDay(appointment, day))
                .OrderBy(appointment => appointment.Slot.Start)
        ]);

    /// <inheritdoc/>
    public Task<Appointment> SaveAsync(Appointment appointment, string action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        _appointments[appointment.Id] = appointment;
        _audit.Enqueue(new AppointmentAuditEntry
        {
            AppointmentId = appointment.Id,
            Action = action,
            At = DateTimeOffset.UtcNow,
        });

        return Task.FromResult(appointment);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AppointmentAuditEntry>> AuditAsync(Guid appointmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppointmentAuditEntry>>(
            [.. _audit.Where(entry => entry.AppointmentId == appointmentId).OrderBy(entry => entry.At)]);

    private static bool OnDay(Appointment appointment, DateOnly day) =>
        DateOnly.FromDateTime(appointment.Slot.Start.UtcDateTime) == day;
}
