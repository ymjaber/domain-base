namespace DomainBase;

/// <summary>
/// Base exception class for domain-specific exceptions.
/// Provides structured error information for domain rule violations.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class DomainNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainNotFoundException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="id">The identifier that was not found.</param>
    public DomainNotFoundException(string resourceName, object id)
        : base($"{resourceName} with id '{id}' was not found.")
    {
        ResourceName = resourceName;
        Id = id;
    }

    /// <summary>
    /// Gets the name of the resource that was not found.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the identifier that was not found.
    /// </summary>
    public object Id { get; }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class DomainNotFoundException<TId> : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainNotFoundException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="id">The identifier that was not found.</param>
    public DomainNotFoundException(string resourceName, TId id)
        : base($"{resourceName} with id '{id}' was not found.")
    {
        ResourceName = resourceName;
        Id = id;
    }

    /// <summary>
    /// Gets the name of the resource that was not found.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the identifier that was not found.
    /// </summary>
    public TId Id { get; }

}

/// <summary>
/// Exception representing a domain conflict (e.g., invariant/state/uniqueness violations).
/// </summary>
public class DomainConflictException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DomainConflictException(string message) : base(message) { }

}

/// <summary>
/// Represents a single validation failure, either object-level or for a specific property.
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    /// Initializes a new instance for an object-level validation failure (no specific property).
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public DomainValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance for a single property validation failure.
    /// </summary>
    /// <param name="propertyName">The name of the invalid property.</param>
    /// <param name="errorMessage">The validation error message.</param>
    public DomainValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets the name of the invalid property (null for object-level failures).
    /// </summary>
    public string? PropertyName { get; }
}
