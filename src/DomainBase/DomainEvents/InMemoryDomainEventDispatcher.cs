using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace DomainBase;

/// <summary>
/// Simple in-memory domain event dispatcher that resolves handlers from an <see cref="IServiceProvider"/>.
/// Uses cached delegates and expression interpreter for AOT-friendly invocation.
/// </summary>
public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Func<object, DomainEvent, CancellationToken, Task>> _dispatchers = new();

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
        var eventType = domainEvent.GetType();
        var handlers = _serviceProvider.GetService(typeof(IEnumerable<IDomainEventHandler>)) as IEnumerable<IDomainEventHandler>
                      ?? Array.Empty<IDomainEventHandler>();

        foreach (var handler in handlers)
        {
            if (!handler.CanHandle(eventType))
                continue;

            await handler.HandleAsync(domainEvent, cancellationToken).ConfigureAwait(false);
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

    private static Func<object, DomainEvent, CancellationToken, Task> BuildDispatcher(Type eventType) => static (_, __, ___) => Task.CompletedTask;
}

