namespace DomainBase;

/// <summary>
/// Defines a handler for domain events of a specific type.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when the event has been handled.</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}