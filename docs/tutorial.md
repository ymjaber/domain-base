# Tutorial: build a small order domain

Goal: by the end, you will model an order domain with value objects, enumerations, entities/aggregates, domain events, specifications, and repositories.

## Table of contents

- [Prerequisites](#prerequisites)
- [1) Define value objects](#1-define-value-objects)
- [2) Define enumerations](#2-define-enumerations)
- [3) Define an aggregate root](#3-define-an-aggregate-root)
- [4) Handle domain events](#4-handle-domain-events)
- [5) Query with specifications](#5-query-with-specifications)
- [6) Repository pattern (optional)](#6-repository-pattern-optional)
- [7) Put it together](#7-put-it-together)

## Prerequisites

- .NET SDK 9
- A test project or console app

## 1) Define value objects

```csharp
using DomainBase;

[GenerateVoJsonConverter]
public sealed partial class OrderId : ValueObject<OrderId, Guid>
{
    public OrderId(Guid value) : base(value) { }
}

[ValueObject]
public sealed partial class Money : ValueObject<Money>
{
    [IncludeInEquality] public decimal Amount { get; init; }
    [IncludeInEquality] public string  Currency { get; init; }

    public static Money Zero(string currency) => new(0m, currency);
    public Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
}
```

## 2) Define enumerations

```csharp
using DomainBase;

[GenerateJsonConverter]
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Draft     = new(0, "Draft");
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Paid      = new(2, "Paid");
    public static readonly OrderStatus Shipped   = new(3, "Shipped");
    public OrderStatus(int value, string name) : base(value, name) { }
}
```

## 3) Define an aggregate root

```csharp
using DomainBase;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id)
    {
        Status = OrderStatus.Draft;
        Total  = Money.Zero("USD");
    }

    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    public void Submit()
    {
        if (Status != OrderStatus.Draft) throw new InvalidOperationDomainException("Submit", "Only Draft orders can be submitted");
        Status = OrderStatus.Submitted;
        AddDomainEvent(new OrderSubmitted(Id));
    }

    public void Pay(Money amount)
    {
        if (Status != OrderStatus.Submitted) throw new InvalidOperationDomainException("Pay", "Order must be Submitted");
        if (amount.Amount <= 0) throw new DomainValidationException(nameof(amount), "Amount must be positive");
        Total = amount;
        Status = OrderStatus.Paid;
        AddDomainEvent(new OrderPaid(Id, amount));
    }
}

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;
public sealed record OrderSubmittedWithMetadata(Guid OrderId) : DomainEventWithMetadata
{
    public OrderSubmittedWithMetadata(Guid orderId) : base(DomainEventMetadata.Create()) { }
}
public sealed record OrderPaid(Guid OrderId, Money Amount) : DomainEvent;
```

## 4) Handle domain events

```csharp
public sealed class OrderSubmittedHandler : IDomainEventHandler<OrderSubmitted>
{
    public Task HandleAsync(OrderSubmitted e, CancellationToken ct = default)
    {
        // send confirmation, enqueue workflow, etc.
        return Task.CompletedTask;
    }
}

public sealed class OrderPaidHandler : IDomainEventHandler<OrderPaid>
{
    public Task HandleAsync(OrderPaid e, CancellationToken ct = default)
    {
        // invoice, notify, etc.
        return Task.CompletedTask;
    }
}
```

DI setup:

```csharp
services.AddSingleton<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
services.AddScoped<IDomainEventHandler, OrderSubmittedHandler>();
services.AddScoped<IDomainEventHandler, OrderPaidHandler>();
```

## 5) Query with specifications

```csharp
public sealed class OrdersByStatus : Specification<Order>
{
    public OrdersByStatus(OrderStatus status) : base(o => o.Status == status) { }
}

public sealed class OrdersPaged : Specification<Order>
{
    public OrdersPaged(int page, int size)
    {
        ApplyOrderByDescending(o => o.Id);
        ApplyPaging((page - 1) * size, size);
    }
}

// Applying to IQueryable (e.g., EF Core DbSet<Order>)
var filtered = SpecificationEvaluator.GetQuery(db.Orders, new OrdersByStatus(OrderStatus.Submitted));

// Evaluate in-memory
var isSatisfied = new OrdersByStatus(OrderStatus.Submitted).IsSatisfiedBy(new Order(Guid.NewGuid()));
```

## 6) Repository pattern (optional)

```csharp
public sealed class OrderRepository : Repository<Order, Guid>
{
    private readonly DbContext _db;
    public OrderRepository(DbContext db, IDomainEventDispatcher dispatcher) : base(dispatcher) => _db = db;

    public override Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Set<Order>().FindAsync([id], ct).AsTask();
    public override Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) => _db.Set<Order>().ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Order>)t.Result, ct);
    public override Task<IReadOnlyList<Order>> FindAsync(ISpecification<Order> spec, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), spec).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Order>)t.Result, ct);
    public override Task<Order?> FindSingleAsync(ISpecification<Order> spec, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), spec).FirstOrDefaultAsync(ct);
    public override Task<int> CountAsync(ISpecification<Order> spec, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), spec).CountAsync(ct);
    public override Task<bool> AnyAsync(ISpecification<Order> spec, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), spec).AnyAsync(ct);

    protected override Task AddCoreAsync(Order entity, CancellationToken ct) => _db.AddAsync(entity, ct).AsTask();
    protected override Task AddRangeCoreAsync(IEnumerable<Order> entities, CancellationToken ct) { _db.AddRange(entities); return Task.CompletedTask; }
    protected override Task UpdateCoreAsync(Order entity, CancellationToken ct) { _db.Update(entity); return Task.CompletedTask; }
    protected override Task RemoveCoreAsync(Order entity, CancellationToken ct) { _db.Remove(entity); return Task.CompletedTask; }
    protected override Task RemoveRangeCoreAsync(IEnumerable<Order> entities, CancellationToken ct) { _db.RemoveRange(entities); return Task.CompletedTask; }
}
```

## 7) Put it together

```csharp
var repo = provider.GetRequiredService<OrderRepository>();
var order = new Order(Guid.NewGuid());
await repo.AddAsync(order);
order.Submit();
await repo.UpdateAsync(order);
order.Pay(new Money(100, "USD"));
await repo.UpdateAsync(order);
```

You now have a cohesive domain model with safe equality, events, and query composition.

Continue to the guide: [guide.md](guide.md) and API reference: [reference.md](reference.md).

