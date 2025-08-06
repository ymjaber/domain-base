namespace DomainBase;

/// <summary>
/// Indicates that a JSON converter should be generated for an enumeration class.
/// When applied to a class that inherits from <see cref="Enumeration"/>,
/// the source generator will create a System.Text.Json converter.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateJsonConverterAttribute : Attribute
{
}