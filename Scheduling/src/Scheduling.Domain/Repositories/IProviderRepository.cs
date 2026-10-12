using Scheduling.Domain.Providers;

namespace Scheduling.Domain.Repositories;

/// <summary>The Domain's view of provider storage. Implemented in Infrastructure.</summary>
public interface IProviderRepository
{
    /// <summary>The provider with this id, or <c>null</c>.</summary>
    Task<Provider?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Every registered provider, ordered by display name.</summary>
    Task<IReadOnlyList<Provider>> ListAsync(CancellationToken cancellationToken = default);
}
