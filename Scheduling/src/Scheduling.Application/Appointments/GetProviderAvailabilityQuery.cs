using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>The weekly working windows of one provider.</summary>
public sealed record GetProviderAvailabilityQuery(Guid ProviderId) : IRequest<IReadOnlyList<AvailabilityViewModel>>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class GetProviderAvailabilityQueryHandler
    : IRequestHandler<GetProviderAvailabilityQuery, IReadOnlyList<AvailabilityViewModel>>
{
    private readonly IProviderRepository _providers;

    /// <summary>Creates the handler.</summary>
    public GetProviderAvailabilityQueryHandler(IProviderRepository providers) => _providers = providers;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AvailabilityViewModel>> HandleAsync(
        GetProviderAvailabilityQuery request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = await _providers.FindAsync(request.ProviderId, cancellationToken)
            ?? throw new SchedulingRuleViolationException($"No provider {request.ProviderId} is registered.");

        return
        [
            .. provider.WorkingHours
                .OrderBy(window => window.Day)
                .ThenBy(window => window.StartsAt)
                .Select(window => new AvailabilityViewModel(provider.Id, window.Day.ToString(), window.StartsAt, window.EndsAt))
        ];
    }
}
