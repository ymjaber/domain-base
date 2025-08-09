namespace DomainBase;

/// <summary>
/// Non-generic domain event handler contract used for AOT-friendly dispatch.
/// </summary>
public interface IDomainEventHandler
{
    /// <summary>
    /// Returns true if this handler can handle the specified event type.
    /// </summary>
    bool CanHandle(Type eventType);

    /// <summary>
    /// Handles the specified domain event.
    /// Implementations should assume the event is of the expected type.
    /// </summary>
    Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for domain events of a specific type.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the specified domain event of type <typeparamref name="TEvent"/>.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);

    // Default interface implementations provide AOT-friendly non-generic dispatch.
    bool IDomainEventHandler.CanHandle(Type eventType) => typeof(TEvent).IsAssignableFrom(eventType);

    Task IDomainEventHandler.HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
        => domainEvent is TEvent typed ? HandleAsync(typed, cancellationToken) : Task.CompletedTask;
}