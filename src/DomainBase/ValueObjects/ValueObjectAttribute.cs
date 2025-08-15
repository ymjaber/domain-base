namespace DomainBase;

/// <summary>
/// Marks a class as a value object to enable source generation of equality members.
/// When applied to a partial class that inherits from <see cref="ValueObject{TSelf}"/>,
/// the source generator will create optimized implementations of equality methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ValueObjectAttribute : Attribute
{
}