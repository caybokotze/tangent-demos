using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain;
using Scheduling.Domain.Appointments;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>Books a provider for a patient.</summary>
public sealed record BookAppointmentCommand(
    Guid ProviderId,
    string PatientName,
    DateTimeOffset StartsAt,
    int DurationMinutes) : IRequest<AppointmentViewModel>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, AppointmentViewModel>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IProviderRepository _providers;
    private readonly IClock _clock;
    private readonly IUserContext _user;

    /// <summary>Creates the handler.</summary>
    public BookAppointmentCommandHandler(
        IAppointmentRepository appointments,
        IProviderRepository providers,
        IClock clock,
        IUserContext user)
    {
        _appointments = appointments;
        _providers = providers;
        _clock = clock;
        _user = user;
    }

    /// <inheritdoc/>
    public async Task<AppointmentViewModel> HandleAsync(BookAppointmentCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = await _providers.FindAsync(request.ProviderId, cancellationToken)
            ?? throw new SchedulingRuleViolationException($"No provider {request.ProviderId} is registered.");

        var slot = new TimeSlot(request.StartsAt, request.StartsAt.AddMinutes(request.DurationMinutes));
        var day = DateOnly.FromDateTime(request.StartsAt.UtcDateTime);

        if (provider.WorkingHoursOn(day.DayOfWeek).All(window => !window.Covers(slot)))
        {
            throw new SchedulingRuleViolationException($"{provider.DisplayName} does not work {slot.Start:O} to {slot.End:O}.");
        }

        var existing = await _appointments.ForProviderAsync(request.ProviderId, day, cancellationToken);
        var appointment = Appointment.Schedule(request.ProviderId, request.PatientName, slot, existing, _clock);

        await _appointments.SaveAsync(appointment, $"booked by {_user.UserName}", cancellationToken);
        return appointment.ToViewModel();
    }
}
