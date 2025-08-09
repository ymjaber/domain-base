namespace DomainBase;

/// <summary>
/// Controls how JSON converters behave when encountering unknown values for generated enumeration converters.
/// </summary>
public enum UnknownValueBehavior
{
    /// <summary>
    /// Return null for unknown values.
    /// </summary>
    ReturnNull = 0,

    /// <summary>
    /// Throw for unknown values.
    /// </summary>
    ThrowException = 1
}

