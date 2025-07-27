namespace DomainBase;

/// <summary>
/// Base class for creating enumeration types with behavior.
/// Provides a type-safe alternative to enums with additional functionality.
/// </summary>
public abstract class Enumeration : IComparable
{
    /// <summary>
    /// Gets the name of the enumeration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the ID of the enumeration.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Enumeration"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="name">The name.</param>
    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    /// <summary>
    /// Returns the name of the enumeration.
    /// </summary>
    /// <returns>The enumeration name.</returns>
    public override string ToString() => Name;

    /// <summary>
    /// Determines whether the specified object is equal to the current enumeration.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>true if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration otherValue)
        {
            return false;
        }

        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    /// <summary>
    /// Returns the hash code for this enumeration.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Compares the current enumeration with another enumeration.
    /// </summary>
    /// <param name="obj">The enumeration to compare with.</param>
    /// <returns>A value indicating the relative order.</returns>
    public int CompareTo(object? obj) => obj is Enumeration other ? Id.CompareTo(other.Id) : 1;

    /// <summary>
    /// Determines whether two enumeration instances are equal.
    /// </summary>
    public static bool operator ==(Enumeration? left, Enumeration? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two enumeration instances are not equal.
    /// </summary>
    public static bool operator !=(Enumeration? left, Enumeration? right) => !(left == right);

    /// <summary>
    /// Determines whether the left enumeration is less than the right enumeration.
    /// </summary>
    public static bool operator <(Enumeration? left, Enumeration? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left enumeration is less than or equal to the right enumeration.
    /// </summary>
    public static bool operator <=(Enumeration? left, Enumeration? right) =>
        left is null || left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left enumeration is greater than the right enumeration.
    /// </summary>
    public static bool operator >(Enumeration? left, Enumeration? right) =>
        left is not null && left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left enumeration is greater than or equal to the right enumeration.
    /// </summary>
    public static bool operator >=(Enumeration? left, Enumeration? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}