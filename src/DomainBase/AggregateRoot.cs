namespace DomainBase;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design.
/// Extends <see cref="Entity{TId}"/> with domain event support.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier. Must be a value type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the aggregate root.</param>
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Gets a read-only collection of domain events raised by this aggregate root.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears all domain events from this aggregate root.
    /// Should be called after the events have been processed and persisted.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Adds a domain event to this aggregate root.
    /// The event will be available through the <see cref="DomainEvents"/> property.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}