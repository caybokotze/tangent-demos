namespace Scheduling.Domain;

/// <summary>The base of every domain entity: an identity, and equality by that identity.</summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>The identity this entity keeps for its whole life.</summary>
    public TId Id { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && other.GetType() == GetType() && Id.Equals(other.Id);

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();
}
