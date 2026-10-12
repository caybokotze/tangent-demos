using Scheduling.Domain;

namespace Scheduling.Infrastructure;

/// <summary>The real clock.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
