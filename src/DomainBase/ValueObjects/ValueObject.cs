using System.Runtime.CompilerServices;

namespace DomainBase;

/// <summary>
/// Base class for value objects in Domain-Driven Design.
/// Value objects are immutable objects that are compared by value rather than identity.
/// </summary>
/// <typeparam name="TSelf">The concrete type inheriting from this value object.</typeparam>
public abstract class ValueObject<TSelf> : IEquatable<TSelf>
    where TSelf : ValueObject<TSelf>
{
    /// <summary>
    /// Determines whether two value object instances are equal.
    /// </summary>
    /// <param name="left">The first value object to compare.</param>
    /// <param name="right">The second value object to compare.</param>
    /// <returns>true if the value objects are equal; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ValueObject<TSelf>? left, ValueObject<TSelf>? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// Determines whether two value object instances are not equal.
    /// </summary>
    /// <param name="left">The first value object to compare.</param>
    /// <param name="right">The second value object to compare.</param>
    /// <returns>true if the value objects are not equal; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ValueObject<TSelf>? left, ValueObject<TSelf>? right) => !(left == right);

    /// <summary>
    /// Determines whether the specified object is equal to the current value object.
    /// Value objects are considered equal if they are of the same type and have equal values.
    /// </summary>
    /// <param name="obj">The object to compare with the current value object.</param>
    /// <returns>true if the specified object is equal to the current value object; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override bool Equals(object? obj)
    {
        return obj is TSelf other && Equals(other);
    }

    /// <summary>
    /// Determines whether the current value object is equal to another value object of the same type.
    /// </summary>
    /// <param name="other">The other value object to compare to.</param>
    /// <returns>true if equal; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool Equals(TSelf? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualsCore(other);
    }

    /// <summary>
    /// Returns a hash code for this value object.
    /// The hash code is based on the value object's type and its values.
    /// </summary>
    /// <returns>A hash code for the current value object.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(GetType(), GetHashCodeCore());

    /// <summary>
    /// When overridden in a derived class, determines whether the current value object
    /// is equal to another value object of the same type based on their values.
    /// </summary>
    /// <param name="other">The value object to compare with the current value object.</param>
    /// <returns>true if the value objects have equal values; otherwise, false.</returns>
    protected abstract bool EqualsCore(TSelf other);

    /// <summary>
    /// When overridden in a derived class, returns a hash code for the value object's values.
    /// </summary>
    /// <returns>A hash code for the value object's values.</returns>
    protected abstract int GetHashCodeCore();
}