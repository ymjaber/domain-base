namespace DomainBase;

/// <summary>
/// Indicates that a System.Text.Json converter should be generated for a value object class that inherits from ValueObject&lt;TSelf, TValue&gt;.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateVoJsonConverterAttribute : Attribute
{
}

