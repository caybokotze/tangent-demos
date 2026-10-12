using System.Collections.Concurrent;
using Scheduling.Domain.Providers;
using Scheduling.Domain.Repositories;
using Tangent;

namespace Scheduling.Infrastructure.Repositories;

/// <summary>
/// CONFORMING to `contract Repository`. Seeded with the two providers the demo books against.
/// </summary>
[Tangent(Contracts.Repository)]
public sealed class ProviderRepository : IProviderRepository
{
    /// <summary>The id of the seeded GP, fixed so the README's curl commands keep working.</summary>
    public static readonly Guid AdaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>The id of the seeded physiotherapist.</summary>
    public static readonly Guid BenId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ConcurrentDictionary<Guid, Provider> _providers = new();

    /// <summary>Creates the repository and seeds it.</summary>
    public ProviderRepository()
    {
        foreach (Provider provider in Seed())
        {
            _providers[provider.Id] = provider;
        }
    }

    /// <inheritdoc/>
    public Task<Provider?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_providers.TryGetValue(id, out Provider? provider) ? provider : null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Provider>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Provider>>([.. _providers.Values.OrderBy(provider => provider.DisplayName, StringComparer.Ordinal)]);

    private static IEnumerable<Provider> Seed()
    {
        TimeOnly nine = new(9, 0);
        TimeOnly five = new(17, 0);
        TimeOnly one = new(13, 0);

        yield return Provider.Register(
            AdaId,
            "Ada Whitfield",
            "General practice",
            [
                Availability.On(DayOfWeek.Monday, nine, five),
                Availability.On(DayOfWeek.Tuesday, nine, five),
                Availability.On(DayOfWeek.Wednesday, nine, five),
                Availability.On(DayOfWeek.Thursday, nine, five),
                Availability.On(DayOfWeek.Friday, nine, one),
            ]);

        yield return Provider.Register(
            BenId,
            "Ben Okafor",
            "Physiotherapy",
            [
                Availability.On(DayOfWeek.Tuesday, one, five),
                Availability.On(DayOfWeek.Thursday, one, five),
                Availability.On(DayOfWeek.Saturday, nine, one),
            ]);
    }
}
