using System;

namespace DomainBase;

/// <summary>
/// Marks a property or field that implements IEnumerable for special sequence equality comparison.
/// Provides options for how sequences should be compared.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class SequenceEqualityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceEqualityAttribute"/> class.
    /// </summary>
    public SequenceEqualityAttribute() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceEqualityAttribute"/> class with specified priority.
    /// </summary>
    /// <param name="order">The evaluation order for equality comparison. Lower values run earlier.</param>
    public SequenceEqualityAttribute(int order)
    {
        Order = order;
    }
    
    /// <summary>
    /// Gets or sets whether the order of elements matters in the comparison.
    /// When true, sequences must have the same elements in the same order.
    /// When false, sequences are compared as sets (same elements, any order).
    /// Default is true.
    /// </summary>
    public bool OrderMatters { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use deep equality for sequence elements.
    /// When true, elements are compared by value (using their Equals method).
    /// When false, elements are compared by reference.
    /// Default is true.
    /// </summary>
    public bool DeepEquality { get; set; } = true;

    /// <summary>
    /// Gets or sets the evaluation order for equality comparison.
    /// Lower values are evaluated first.
    /// Default is 0.
    /// </summary>
    public int Order { get; set; } = 0;
}