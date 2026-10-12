namespace Scheduling.Domain;

/// <summary>The only way the Domain is allowed to learn what time it is.</summary>
public interface IClock
{
    /// <summary>The current instant, in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
