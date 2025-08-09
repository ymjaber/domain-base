namespace DomainBase;

/// <summary>
/// Base class for simple value objects that wrap a single value.
/// Provides automatic equality and hash code implementation based on the wrapped value.
/// </summary>
/// <typeparam name="TSelf">The concrete type inheriting from this value object.</typeparam>
/// <typeparam name="TValue">The type of the wrapped value. Must not be null.</typeparam>
public abstract class ValueObject<TSelf, TValue> : ValueObject<TSelf>
where TSelf : ValueObject<TSelf, TValue>
where TValue : notnull
{
    /// <summary>
    /// Initializes a new instance of the value object with the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    protected ValueObject(TValue value) => Value = value;

    /// <summary>
    /// Gets the wrapped value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Implicitly converts the value object to its wrapped value.
    /// </summary>
    /// <param name="valueObject">The value object to convert.</param>
    /// <returns>The wrapped value.</returns>
    public static implicit operator TValue(ValueObject<TSelf, TValue> valueObject) => valueObject.Value;

    /// <summary>
    /// Determines whether this value object is equal to another based on their wrapped values.
    /// </summary>
    /// <param name="other">The other value object to compare with.</param>
    /// <returns>true if the wrapped values are equal; otherwise, false.</returns>
    protected override bool EqualsCore(TSelf other) => Value.Equals(other.Value);

    /// <summary>
    /// Returns the hash code of the wrapped value.
    /// </summary>
    /// <returns>The hash code of the wrapped value.</returns>
    protected override int GetHashCodeCore() => Value.GetHashCode();

    /// <summary>
    /// Returns a string representation of the wrapped value.
    /// </summary>
    /// <returns>The string representation of the wrapped value.</returns>
    public override string ToString() => Value.ToString() ?? string.Empty;
}
