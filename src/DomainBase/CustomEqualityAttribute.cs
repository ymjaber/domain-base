namespace DomainBase;

/// <summary>
/// Marks a property or field to use custom equality comparison methods.
/// Requires implementation of partial methods for both equality and hash code computation.
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
    /// <param name="priority">The priority for equality comparison. Higher values are checked first.</param>
    public CustomEqualityAttribute(int priority)
    {
        Priority = priority;
    }
    
    /// <summary>
    /// Gets or sets the priority for equality comparison.
    /// Higher values are checked first, allowing for early exit optimization.
    /// Default is 0.
    /// </summary>
    public int Priority { get; set; } = 0;
}