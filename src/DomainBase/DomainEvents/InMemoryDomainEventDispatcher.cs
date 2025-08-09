namespace DomainBase;

/// <summary>
/// Simple in-memory domain event dispatcher that resolves handlers from an <see cref="IServiceProvider"/>.
/// </summary>
public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new dispatcher that resolves handlers from the provided service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="IDomainEventHandler{TEvent}"/> implementations.</param>
    public InMemoryDomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Dispatches a single domain event to all resolved handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when dispatching has finished.</returns>
    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = (IEnumerable<object>?)_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType))
                       ?? Array.Empty<object>();

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync");
            if (method != null)
            {
                var task = (Task?)method.Invoke(handler, new object?[] { domainEvent, cancellationToken });
                if (task != null)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Dispatches a sequence of domain events to their handlers.
    /// </summary>
    /// <param name="domainEvents">The events to dispatch.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when dispatching has finished.</returns>
    public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}

