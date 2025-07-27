namespace DomainBase;

/// <summary>
/// Marks a class as an enumeration to trigger source generation of helper methods.
/// When applied to a partial class that inherits from <see cref="Enumeration"/>,
/// the source generator will create optimized implementations of enumeration methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EnumerationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to generate JSON converter for the enumeration.
    /// Default is true.
    /// </summary>
    public bool GenerateJsonConverter { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate EF Core value converter for the enumeration.
    /// Default is false.
    /// </summary>
    public bool GenerateEfValueConverter { get; set; } = false;
}