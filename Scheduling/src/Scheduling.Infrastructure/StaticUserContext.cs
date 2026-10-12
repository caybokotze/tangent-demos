using Scheduling.Application.Abstractions;

namespace Scheduling.Infrastructure;

/// <summary>
/// The demo has no authentication, so every caller is the same person. A real deployment
/// swaps this for one that reads the current principal.
/// </summary>
public sealed class StaticUserContext(string userName) : IUserContext
{
    /// <inheritdoc/>
    public string UserName { get; } = userName;
}
