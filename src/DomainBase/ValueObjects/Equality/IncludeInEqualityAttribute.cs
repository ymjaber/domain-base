using System;

namespace DomainBase;

/// <summary>
/// Marks a property or field to be included in value object equality comparison using default equality.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class IncludeInEqualityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeInEqualityAttribute"/> class.
    /// </summary>
    public IncludeInEqualityAttribute() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeInEqualityAttribute"/> class with specified priority.
    /// </summary>
    /// <param name="order">The evaluation order for equality comparison. Lower values run earlier.</param>
    public IncludeInEqualityAttribute(int order)
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