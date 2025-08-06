namespace DomainBase;

/// <summary>
/// Indicates that an Entity Framework Core value converter should be generated for an enumeration class.
/// When applied to a class that inherits from <see cref="Enumeration"/>,
/// the source generator will create an EF Core value converter.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateEfValueConverterAttribute : Attribute
{
}