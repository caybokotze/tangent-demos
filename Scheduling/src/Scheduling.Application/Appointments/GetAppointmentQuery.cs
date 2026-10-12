using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>One appointment by id.</summary>
public sealed record GetAppointmentQuery(Guid AppointmentId) : IRequest<AppointmentViewModel?>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class GetAppointmentQueryHandler : IRequestHandler<GetAppointmentQuery, AppointmentViewModel?>
{
    private readonly IAppointmentRepository _appointments;

    /// <summary>Creates the handler.</summary>
    public GetAppointmentQueryHandler(IAppointmentRepository appointments) => _appointments = appointments;

    /// <inheritdoc/>
    public async Task<AppointmentViewModel?> HandleAsync(
        GetAppointmentQuery request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var appointment = await _appointments.FindAsync(request.AppointmentId, cancellationToken);
        return appointment?.ToViewModel();
    }
}
