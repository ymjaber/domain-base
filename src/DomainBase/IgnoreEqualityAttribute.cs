namespace DomainBase;

/// <summary>
/// Explicitly marks a property or field to be excluded from value object equality comparison.
/// Use this to silence analyzer warnings for properties that should not affect equality.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public sealed class IgnoreEqualityAttribute : Attribute
{
}