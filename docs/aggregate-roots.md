### Aggregate Roots & Domain Events

`AggregateRoot<TId>` extends `Entity<TId>` and captures domain events raised during state changes. Events are dispatched after persistence via `Repository<TEntity,TId>`.

API:
- `IReadOnlyCollection<DomainEvent> DomainEvents`
- `ClearDomainEvents()`
- `protected void AddDomainEvent(DomainEvent evt)`
- `IDomainEventDispatcher` and `InMemoryDomainEventDispatcher`
- `IDomainEventHandler<TEvent>` with `Task HandleAsync(TEvent, CancellationToken)`
 - Optional event metadata types: `DomainEventWithMetadata` and `DomainEventMetadata`

Example:

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }

    public void Ship()
    {
        AddDomainEvent(new OrderShippedEvent(Id));
    }
}

public sealed record OrderShippedEvent(Guid OrderId) : DomainEvent;
```

