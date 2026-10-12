using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain;
using Scheduling.Domain.Repositories;
using Scheduling.Domain.Services;

namespace Scheduling.Application.Appointments;

/// <summary>The free slots of one provider on one UTC day.</summary>
public sealed record FindAvailableSlotsQuery(Guid ProviderId, DateOnly Day, int DurationMinutes)
    : IRequest<IReadOnlyList<TimeSlotViewModel>>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class FindAvailableSlotsQueryHandler
    : IRequestHandler<FindAvailableSlotsQuery, IReadOnlyList<TimeSlotViewModel>>
{
    private readonly IProviderRepository _providers;
    private readonly IAppointmentRepository _appointments;
    private readonly ISlotFinderService _slotFinder;

    /// <summary>Creates the handler.</summary>
    public FindAvailableSlotsQueryHandler(
        IProviderRepository providers,
        IAppointmentRepository appointments,
        ISlotFinderService slotFinder)
    {
        _providers = providers;
        _appointments = appointments;
        _slotFinder = slotFinder;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TimeSlotViewModel>> HandleAsync(
        FindAvailableSlotsQuery request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = await _providers.FindAsync(request.ProviderId, cancellationToken)
            ?? throw new SchedulingRuleViolationException($"No provider {request.ProviderId} is registered.");

        var booked = await _appointments.ForProviderAsync(request.ProviderId, request.Day, cancellationToken);

        var free = _slotFinder.FindFreeSlots(
            provider.WorkingHoursOn(request.Day.DayOfWeek),
            booked,
            request.Day,
            TimeSpan.FromMinutes(request.DurationMinutes));

        return [.. free.Select(slot => new TimeSlotViewModel(slot.Start, slot.End))];
    }
}
