using Scheduling.Infrastructure.Repositories;

namespace Scheduling.Api.Diagnostics;

/// <summary>
/// Tells the health endpoint which providers this instance was seeded with, so the demo's
/// first <c>curl</c> has ids to use.
/// </summary>
public static class SeedInspector
{
    // VIOLATION (TNG0202): project.tan says `Api -> Application, Domain`, and this reads two
    // constants straight out of Infrastructure. It is how layering usually breaks in practice:
    // not in a controller, but in a small helper next to one, in a project whose file already
    // references Infrastructure because Program.cs legitimately composes it.
    // The fix is to move the seed ids behind the Domain (a `SeedProviders` constant class in
    // Scheduling.Domain.Providers) or to read them through IProviderRepository.ListAsync().
    /// <summary>The ids of the providers the in-memory store is seeded with.</summary>
    public static IReadOnlyList<Guid> SeededProviderIds => [ProviderRepository.AdaId, ProviderRepository.BenId];
}
