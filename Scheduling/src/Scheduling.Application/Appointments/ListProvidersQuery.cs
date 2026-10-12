using Scheduling.Application.Abstractions;
using Scheduling.Application.ViewModels;
using Scheduling.Domain.Repositories;

namespace Scheduling.Application.Appointments;

/// <summary>Every registered provider.</summary>
public sealed record ListProvidersQuery : IRequest<IReadOnlyList<ProviderViewModel>>;

/// <summary>CONFORMING to `contract Handler`.</summary>
public sealed class ListProvidersQueryHandler : IRequestHandler<ListProvidersQuery, IReadOnlyList<ProviderViewModel>>
{
    private readonly IProviderRepository _providers;

    /// <summary>Creates the handler.</summary>
    public ListProvidersQueryHandler(IProviderRepository providers) => _providers = providers;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProviderViewModel>> HandleAsync(
        ListProvidersQuery request,
        CancellationToken cancellationToken = default)
    {
        var providers = await _providers.ListAsync(cancellationToken);
        return [.. providers.Select(provider => new ProviderViewModel(provider.Id, provider.DisplayName, provider.Speciality))];
    }
}
