using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>Every appointment on one UTC day.</summary>
public sealed record ListAppointmentsForDayQuery(DateOnly Day) : IRequest<IReadOnlyList<AppointmentViewModel>>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class ListAppointmentsForDayQueryHandler
    : IRequestHandler<ListAppointmentsForDayQuery, IReadOnlyList<AppointmentViewModel>>
{
    private readonly IAppointmentRepository _appointments;

    /// <summary>Creates the handler.</summary>
    public ListAppointmentsForDayQueryHandler(IAppointmentRepository appointments) => _appointments = appointments;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AppointmentViewModel>> HandleAsync(
        ListAppointmentsForDayQuery request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var appointments = await _appointments.ForDayAsync(request.Day, cancellationToken);
        return [.. appointments.Select(AppointmentMapping.ToViewModel)];
    }
}
