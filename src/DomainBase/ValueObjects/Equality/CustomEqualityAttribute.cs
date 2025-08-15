using System;

namespace DomainBase;

/// <summary>
/// Marks a property or field to use custom equality comparison methods.
/// When applied, the containing value object class must provide two static methods for this member:
/// <c>private static bool Equals_{MemberName}(in T value, in T otherValue)</c> and
/// <c>private static int GetHashCode_{MemberName}(in T value)</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class CustomEqualityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEqualityAttribute"/> class.
    /// </summary>
    public CustomEqualityAttribute() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEqualityAttribute"/> class with specified priority.
    /// </summary>
    /// <param name="order">The evaluation order for equality comparison. Lower values run earlier.</param>
    public CustomEqualityAttribute(int order)
    {
        Order = order;
    }
    
    /// <summary>
    /// Gets or sets the evaluation order for equality comparison.
    /// Lower values are evaluated first.
    /// Default is 0.
    /// </summary>
    public int Order { get; set; } = 0;
}