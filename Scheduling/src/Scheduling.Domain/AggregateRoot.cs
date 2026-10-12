namespace Scheduling.Domain;

/// <summary>
/// An entity that is the consistency boundary for a cluster of objects: the only kind of
/// entity a repository hands out or accepts.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id)
        : base(id)
    {
    }
}
