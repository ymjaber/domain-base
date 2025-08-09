namespace DomainBase;

/// <summary>
/// Base record for domain events in Domain-Driven Design.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredOn { get; init; }

    /// <summary>
    /// Initializes a new domain event with generated Id and current UTC timestamp.
    /// </summary>
    protected DomainEvent() : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new domain event with specified values.
    /// </summary>
    protected DomainEvent(Guid id, DateTimeOffset occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;
    }

    /// <summary>
    /// Gets the event type name.
    /// </summary>
    /// <returns>The simple type name of the event.</returns>
    public virtual string GetEventName() => GetType().Name;
}
