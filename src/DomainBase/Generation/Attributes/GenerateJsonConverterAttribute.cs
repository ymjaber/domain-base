namespace DomainBase;

/// <summary>
/// Indicates that a JSON converter should be generated for an enumeration class.
/// When applied to a class that inherits from <see cref="Enumeration"/>,
/// the source generator will create a System.Text.Json converter.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateJsonConverterAttribute : Attribute
{
    /// <summary>
    /// Controls the behavior when the JSON value does not map to a known enumeration value.
    /// Default is <see cref="UnknownValueBehavior.ReturnNull"/>.
    /// </summary>
    public UnknownValueBehavior Behavior { get; set; } = UnknownValueBehavior.ReturnNull;
}