# Tutorial: build a small order domain

Goal: by the end, you will model an order domain with value objects, enumerations, entities/aggregates, domain events, specifications, and repositories.

## Table of contents

- [Prerequisites](#prerequisites)
- [1) Define value objects](#1-define-value-objects)
- [2) Define enumerations](#2-define-enumerations)
- [3) Define an entity](#3-define-an-entity)
- [4) Define an aggregate root](#4-define-an-aggregate-root)
- [5) Handle domain events](#5-handle-domain-events)
- [6) Query with specifications](#6-query-with-specifications)
- [7) Repository pattern (optional)](#7-repository-pattern-optional)
- [8) Put it together](#8-put-it-together)

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

### Usage

```csharp
var orderId = new OrderId(Guid.NewGuid());
var sameOrderId = new OrderId(orderId.Value);
var idsEqual = orderId == sameOrderId; // true

var price1 = new Money(100m, "USD");
var price2 = new Money(100m, "USD");
var valueObjectsEqual = price1.Equals(price2); // true
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

### Usage

```csharp
// Enumerations helpers
var allStatuses = OrderStatus.GetAll(); // IReadOnlyCollection<OrderStatus>

var submittedByValue = OrderStatus.FromValue(1); // Submitted
var paidValue = OrderStatus.FromName("Paid").Value; // 2

if (OrderStatus.TryFromName("Shipped", out var shipped))
{
    // use shipped
}
else
{
    // handle unknown
}
```

## 3) Define an entity

```csharp
using DomainBase;

public sealed class OrderItem : Entity<Guid>
{
    public OrderItem(Guid id, string sku, int quantity, Money unitPrice) : base(id)
    {
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string Sku { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public Money Subtotal() => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}
```

### Usage

```csharp
var id = Guid.NewGuid();
var item1 = new OrderItem(id, "SKU-001", 2, new Money(50, "USD"));
var item2 = new OrderItem(id, "SKU-CHANGED", 1, new Money(120, "USD"));
var sameEntity = item1 == item2; // true (identity-based equality)
```

## 4) Define an aggregate root

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

### Usage

```csharp
var order = new Order(Guid.NewGuid());
order.Submit();
order.Pay(new Money(100, "USD"));
```

## 5) Handle domain events

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

### Usage

```csharp
var order = new Order(Guid.NewGuid());
order.Submit();
// Events are queued on the aggregate and can be observed
var hasSubmitted = order.DomainEvents.OfType<OrderSubmitted>().Any();
// When using a repository (below), events are dispatched then cleared automatically.
```

## 6) Query with specifications

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

### Usage

```csharp
var order = new Order(Guid.NewGuid());
var spec = new OrdersByStatus(OrderStatus.Submitted);
var matches = spec.IsSatisfiedBy(order);

var paged = new OrdersPaged(page: 1, size: 20);
var query = SpecificationEvaluator.GetQuery(db.Orders, paged);
```

## 7) Repository pattern (optional)

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

### Usage

```csharp
var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
var repo = new OrderRepository(dbContext, dispatcher);
await repo.AddAsync(new Order(Guid.NewGuid()));
```

## 8) Put it together

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
