using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>Calls an appointment off.</summary>
public sealed record CancelAppointmentCommand(Guid AppointmentId, string Reason) : IRequest<AppointmentViewModel>;

/// <summary>
/// Breaks `contract Handler`'s constructor rule: one of its parameters is parked in a mutable
/// field, so nothing stops a later method from swapping the clock out mid-request.
/// </summary>
public sealed class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, AppointmentViewModel>
{
    private readonly IAppointmentRepository _appointments;

    // VIOLATION (TNG0301): `contract Handler` declares
    // `ctor { require each parameter assigned to private readonly field }`.
    // `clock` lands in a mutable field. The fix is one word: `readonly`.
    private IClock _clock;

    /// <summary>Creates the handler.</summary>
    public CancelAppointmentCommandHandler(IAppointmentRepository appointments, IClock clock)
    {
        _appointments = appointments;
        _clock = clock;
    }

    /// <inheritdoc/>
    public async Task<AppointmentViewModel> HandleAsync(
        CancelAppointmentCommand request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var appointment = await _appointments.FindAsync(request.AppointmentId, cancellationToken)
            ?? throw new SchedulingRuleViolationException($"No appointment {request.AppointmentId} exists.");

        appointment.Cancel(request.Reason);

        await _appointments.SaveAsync(appointment, $"cancelled at {_clock.UtcNow:O}", cancellationToken);
        return appointment.ToViewModel();
    }
}
