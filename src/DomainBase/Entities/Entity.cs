using System.Runtime.CompilerServices;

namespace DomainBase;

/// <summary>
/// Base class for entities in Domain-Driven Design.
/// Provides identity-based equality and common entity behavior.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier. Must be a value type.</typeparam>
public abstract class Entity<TId>
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    public Entity(TId id) => Id = id;

    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; }

    /// <summary>
    /// Determines whether two entity instances are equal.
    /// </summary>
    /// <param name="left">The left-hand entity.</param>
    /// <param name="right">The right-hand entity.</param>
    /// <returns>true if the entities are equal; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => left?.Equals(right) ?? right is null;

    /// <summary>
    /// Determines whether two entity instances are not equal.
    /// </summary>
    /// <param name="left">The left-hand entity.</param>
    /// <param name="right">The right-hand entity.</param>
    /// <returns>true if the entities are not equal; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// Entities are considered equal if they are of the same type and have the same ID.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (Id.Equals(default(TId)) || other.Id.Equals(default(TId))) return false;
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Returns a hash code for this entity.
    /// The hash code is based on the entity's type and ID.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>
    /// Returns a string that represents the current entity.
    /// </summary>
    /// <returns>A string in the format "TypeName: Id".</returns>
    public override string ToString() => $"{GetType().Name}: {Id}";
}
