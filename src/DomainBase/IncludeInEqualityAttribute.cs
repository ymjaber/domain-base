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
    /// <param name="priority">The priority for equality comparison. Higher values are checked first.</param>
    public IncludeInEqualityAttribute(int priority)
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