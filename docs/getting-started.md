# Getting started

## Table of contents

- [Install](#install)
- [What you get](#what-you-get)
- [First 5 minutes](#first-5-minutes)
- [Next steps](#next-steps)

## Install

```bash
dotnet add package DomainBase --version 2.0.0
```

NuGet: [DomainBase](https://www.nuget.org/packages/DomainBase/)

## What you get

- `Entity<TId>`, `AggregateRoot<TId>`: identity-based equality and domain events
- `ValueObject<TSelf>`, `ValueObject<TSelf,TValue>`: value equality with generators and analyzers
- `Enumeration`: richer alternative to enums with generator helpers
- `DomainEvent` + dispatcher and handlers
- `Specification<T>` + `SpecificationEvaluator`
- Optional `Repository<TEntity,TId>` abstraction
- Analyzers and code fixes for correctness and ergonomics

## First 5 minutes

1) Create a simple value object

```csharp
using DomainBase;

[GenerateVoJsonConverter]
[GenerateTypeConverter]
public sealed partial class OrderId : ValueObject<OrderId, Guid>
{
    public OrderId(Guid value) : base(value) { }
}
```

2) Create a rich value object

```csharp
using DomainBase;

[ValueObject]
public sealed partial class Money : ValueObject<Money>
{
    [IncludeInEquality] public decimal Amount { get; init; }
    [IncludeInEquality] public string Currency { get; init; }
}
```

3) Create an enumeration

```csharp
using DomainBase;

[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Approved  = new(2, "Approved");
    public static readonly OrderStatus Rejected  = new(3, "Rejected");
    public OrderStatus(int value, string name) : base(value, name) { }
}
```

4) Raise a domain event from an aggregate root

```csharp
using DomainBase;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }
    public void Submit() => AddDomainEvent(new OrderSubmitted(Id));
}

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;
```

5) Dispatch events

```csharp
// DI registration
services.AddSingleton<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
services.AddScoped<IDomainEventHandler, OrderSubmittedHandler>();

public sealed class OrderSubmittedHandler : IDomainEventHandler<OrderSubmitted>
{
    public Task HandleAsync(OrderSubmitted e, CancellationToken ct = default)
    {
        // react
        return Task.CompletedTask;
    }
}
```

## Next steps

- Follow the full tutorial: [tutorial.md](tutorial.md)
- See detailed usage and options: [guide.md](guide.md)
- Dive into the API: [reference.md](reference.md)

