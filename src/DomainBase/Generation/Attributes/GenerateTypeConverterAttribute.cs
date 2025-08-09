namespace DomainBase;

/// <summary>
/// Indicates that a System.ComponentModel.TypeConverter should be generated for a value object class.
/// When applied to a class that inherits from <see cref="ValueObject{TSelf, TValue}"/>,
/// the source generator will create a type converter to convert to/from the underlying value type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateTypeConverterAttribute : Attribute
{
}

