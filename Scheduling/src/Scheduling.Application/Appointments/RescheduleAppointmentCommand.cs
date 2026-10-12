using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain;
using Scheduling.Domain.Appointments;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>Moves an existing appointment to a new start time, keeping its length.</summary>
public sealed record RescheduleAppointmentCommand(Guid AppointmentId, DateTimeOffset StartsAt)
    : IRequest<AppointmentViewModel>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class RescheduleAppointmentCommandHandler
    : IRequestHandler<RescheduleAppointmentCommand, AppointmentViewModel>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IClock _clock;
    private readonly IUserContext _user;

    /// <summary>Creates the handler.</summary>
    public RescheduleAppointmentCommandHandler(IAppointmentRepository appointments, IClock clock, IUserContext user)
    {
        _appointments = appointments;
        _clock = clock;
        _user = user;
    }

    /// <inheritdoc/>
    public async Task<AppointmentViewModel> HandleAsync(
        RescheduleAppointmentCommand request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var appointment = await _appointments.FindAsync(request.AppointmentId, cancellationToken)
            ?? throw new SchedulingRuleViolationException($"No appointment {request.AppointmentId} exists.");

        TimeSlot moved = appointment.Slot.MoveTo(request.StartsAt);
        var day = DateOnly.FromDateTime(request.StartsAt.UtcDateTime);
        var existing = await _appointments.ForProviderAsync(appointment.ProviderId, day, cancellationToken);

        appointment.Reschedule(moved, existing, _clock);

        await _appointments.SaveAsync(appointment, $"rescheduled by {_user.UserName}", cancellationToken);
        return appointment.ToViewModel();
    }
}
