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

    /// <summary>
    /// Gets the error code associated with this domain exception.
    /// Override in derived classes to provide specific error codes.
    /// </summary>
    public virtual string ErrorCode => GetType().Name.Replace("Exception", "");
}

/// <summary>
/// Exception thrown when an entity is not found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The identifier that was not found.</param>
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.")
    {
        EntityName = entityName;
        Id = id;
    }

    /// <summary>
    /// Gets the name of the entity that was not found.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the identifier that was not found.
    /// </summary>
    public object Id { get; }

    /// <inheritdoc/>
    public override string ErrorCode => "EntityNotFound";
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    /// <param name="ruleName">The name of the violated business rule.</param>
    /// <param name="message">The error message.</param>
    public BusinessRuleViolationException(string ruleName, string message)
        : base(message)
    {
        RuleName = ruleName;
    }

    /// <summary>
    /// Gets the name of the violated business rule.
    /// </summary>
    public string RuleName { get; }

    /// <inheritdoc/>
    public override string ErrorCode => $"BusinessRule.{RuleName}";
}

/// <summary>
/// Exception thrown when domain validation fails.
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public DomainValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public DomainValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    public DomainValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = new[] { errorMessage }
        };
    }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <inheritdoc/>
    public override string ErrorCode => "ValidationFailed";
}

/// <summary>
/// Exception thrown when an operation would violate domain invariants.
/// </summary>
public class InvariantViolationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvariantViolationException"/> class.
    /// </summary>
    /// <param name="invariantName">The name of the violated invariant.</param>
    /// <param name="message">The error message.</param>
    public InvariantViolationException(string invariantName, string message)
        : base(message)
    {
        InvariantName = invariantName;
    }

    /// <summary>
    /// Gets the name of the violated invariant.
    /// </summary>
    public string InvariantName { get; }

    /// <inheritdoc/>
    public override string ErrorCode => $"Invariant.{InvariantName}";
}

/// <summary>
/// Exception thrown when a domain operation is not allowed in the current state.
/// </summary>
public class InvalidOperationDomainException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidOperationDomainException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidOperationDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidOperationDomainException"/> class.
    /// </summary>
    /// <param name="operation">The name of the invalid operation.</param>
    /// <param name="reason">The reason why the operation is invalid.</param>
    public InvalidOperationDomainException(string operation, string reason)
        : base($"Operation '{operation}' is not allowed: {reason}")
    {
        Operation = operation;
        Reason = reason;
    }

    /// <summary>
    /// Gets the name of the invalid operation.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Gets the reason why the operation is invalid.
    /// </summary>
    public string? Reason { get; }

    /// <inheritdoc/>
    public override string ErrorCode => "InvalidOperation";
}