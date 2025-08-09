# Guide

## Table of contents

- [Overview](#overview)
- [Entities and aggregate roots](#entities-and-aggregate-roots)
- [Value objects](#value-objects)
- [Generators and analyzers](#generators-and-analyzers)
- [Enumerations](#enumerations)
- [Domain events and handlers](#domain-events-and-handlers)
- [Specifications](#specifications)
- [Repositories (optional)](#repositories-optional)
- [Serialization helpers](#serialization-helpers)
- [Integration tips](#integration-tips)

## Overview

DomainBase provides primitives and tooling for DDD:

- Entities and aggregate roots: identity equality, domain events
- Value objects: value equality via base class + generator + analyzers
- Enumerations: type-safe enum-like types with helper APIs
- Domain events: base types, handlers, and an in-memory dispatcher
- Specifications: compose query logic and apply to IQueryable
- Repositories: optional base that wires domain event dispatch after persistence

## Entities and aggregate roots

- `Entity<TId>` provides identity-based equality and `Id` property.
- `AggregateRoot<TId>` adds domain event collection and `AddDomainEvent`/`ClearDomainEvents`.

```csharp
public sealed class Customer : AggregateRoot<Guid>
{
    public Customer(Guid id) : base(id) { }
}
```

Auditing variants are available when you need timestamps and/or user IDs:

```csharp
public sealed class AuditableOrder : AuditableAggregateRoot<Guid>
{
    public AuditableOrder(Guid id) : base(id) { }

    public void UpdateSomething()
    {
        // ... mutate
        MarkAsUpdated(); // updates UpdatedAt
    }
}

public sealed class AuditableOrderWithUser : AuditableAggregateRoot<Guid, string>
{
    public AuditableOrderWithUser(Guid id) : base(id) { }
    public void UpdateBy(string userId) => MarkAsUpdated(userId);
}
```

## Value objects

Two styles:

1) Simple wrapper: `ValueObject<TSelf, TValue>`

```csharp
public sealed partial class Email : ValueObject<Email, string>
{
    public Email(string value) : base(value) { }
}
```

2) Rich object: `ValueObject<TSelf>` with `[ValueObject]` and member-level equality attributes

Attributes:

- `[IncludeInEquality(Priority = n)]` include member, higher priority compared first
- `[IgnoreEquality]` exclude member
- `[SequenceEquality(OrderMatters = true|false, DeepEquality = true|false, Priority = n)]` for `IEnumerable` members
- `[CustomEquality(Priority = n)]` pair with required static methods:
  - `private static void Equals_{Name}(in T value, in T otherValue, out bool result)`
  - `private static void GetHashCode_{Name}(in T value, ref HashCode hashCode)`

```csharp
[ValueObject]
public sealed partial class Address : ValueObject<Address>
{
    [IncludeInEquality] public string City  { get; init; }
    [IncludeInEquality] public string Street{ get; init; }
}
```

Advanced equality options:

```csharp
// Sequence equality for collections
[ValueObject]
public sealed partial class Basket : ValueObject<Basket>
{
    [SequenceEquality(OrderMatters = false, DeepEquality = true, Priority = 10)]
    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
}

// Custom equality for case-insensitive member
[ValueObject]
public sealed partial class Person : ValueObject<Person>
{
    [IncludeInEquality(Priority = 10)] public string FirstName { get; init; } = "";
    [CustomEquality(Priority = 9)] public string LastName { get; init; } = "";

    private static void Equals_LastName(in string value, in string other, out bool result)
        => result = string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    private static void GetHashCode_LastName(in string value, ref HashCode hash)
        => hash.Add(value.ToUpperInvariant());
}
```

## Generators and analyzers

- Mark value objects with `[ValueObject]` and make the class `partial`.
- The generator produces optimized `EqualsCore`/`GetHashCodeCore` implementations.
- Analyzers enforce:
  - DBVO001: class must be partial
  - DBVO002: members need one equality attribute
  - DBVO003/004: missing custom methods
  - DBVO005: multiple equality attributes
  - DBVO006: sequence attribute on non-sequence
  - DBVO007: equality attribute outside `[ValueObject]` class
  - DBVO008: `[ValueObject]` without inheriting `ValueObject<TSelf>`
  - DBVO010/011: immutability rules
  - DBVO012: extra members in simple VO wrappers

Code fixes:

- DBVO001/DBEN003: add `partial` modifier
- DBVO002: add equality attribute (`IncludeInEquality`, `IgnoreEquality`, `SequenceEquality`)
- DBVO003/004: generate required custom equality methods

## Enumerations

Define a `partial` class inheriting `Enumeration` with static instances. The generator adds:

- Lookup: `GetAll()`, `FromValue(int)`, `FromName(string)`, `TryFromValue`, `TryFromName`
- Optional JSON converter via `[GenerateJsonConverter(Behavior = ...)]`
- Optional EF Core `ValueConverter` via `[GenerateEfValueConverter]`

```csharp
[GenerateJsonConverter]
public sealed partial class Priority : Enumeration
{
    public static readonly Priority Low = new(1, "Low");
    public static readonly Priority High = new(2, "High");
    public Priority(int value, string name) : base(value, name) { }
}
```

Generated helper APIs:

```csharp
var all = Priority.GetAll();
var fromValue = Priority.FromValue(1);         // Low
var fromName  = Priority.FromName("High");     // High
if (Priority.TryFromName("Unknown", out var p)) { /* ... */ }
```

JSON converter behavior for unknown values:

```csharp
[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
public sealed partial class PaymentMethod : Enumeration { /* ... */ }
```

EF Core converter generation (for saving as int):

```csharp
[GenerateEfValueConverter]
public sealed partial class OrderStatus : Enumeration { /* ... */ }
```

## Domain events and handlers

- `DomainEvent` base record, optional `DomainEventWithMetadata`
- Handlers implement `IDomainEventHandler<TEvent>` and are also resolvable via non-generic `IDomainEventHandler` at runtime
- Use `InMemoryDomainEventDispatcher` with DI to dispatch events raised on aggregates

With metadata:

```csharp
public sealed record OrderShipped(Guid OrderId) : DomainEventWithMetadata
{
    public OrderShipped(Guid orderId) : base(DomainEventMetadata.Create())
    {
        // set optional metadata via WithMetadata in a copy method if needed
    }
}

// Creating metadata explicitly
var metadata = DomainEventMetadata.CreateWithCorrelation(Guid.NewGuid(), userId: "alice");
var @event = new OrderShipped(orderId).WithMetadata(metadata);
```

Registration and dispatch:

```csharp
services.AddSingleton<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();

// Register all handlers as IDomainEventHandler
services.AddScoped<IDomainEventHandler, OrderSubmittedHandler>();
services.AddScoped<IDomainEventHandler, OrderShippedHandler>();

public sealed class OrderSubmittedHandler : IDomainEventHandler<OrderSubmitted>
{
    public Task HandleAsync(OrderSubmitted e, CancellationToken ct = default) => Task.CompletedTask;
}
```

## Specifications

- Compose query logic using expressions, includes, order, paging; evaluate via `SpecificationEvaluator.GetQuery`

```csharp
public sealed class ActiveCustomers : Specification<Customer>
{
    public ActiveCustomers() : base(c => c.IsActive) => ApplyOrderBy(c => c.Name);
}
```

Includes, ordering, paging, and composition:

```csharp
public sealed class CustomersWithOrders : Specification<Customer>
{
    public CustomersWithOrders()
    {
        AddInclude(c => c.Orders);
        ApplyOrderByDescending(c => c.CreatedAt);
        ApplyPaging(skip: 0, take: 50);
    }
}

var spec = new ActiveCustomers().And(new CustomersWithOrders());
var query = SpecificationEvaluator.GetQuery(db.Customers, spec);
```

Evaluate in-memory when needed:

```csharp
var isActive = new ActiveCustomers().IsSatisfiedBy(customer);
```

Note: `SpecificationEvaluator.Include(...)` extension dispatch is a no-op for non-EF providers; EF Core will resolve its own `Include` at runtime.

## Repositories (optional)

- `Repository<TEntity,TId>` wires domain event dispatch after persistence hooks you implement

```csharp
public sealed class OrderRepository : Repository<Order, Guid>
{
    private readonly DbContext _db;
    public OrderRepository(DbContext db, IDomainEventDispatcher d) : base(d) => _db = db;

    public override Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Set<Order>().FindAsync([id], ct).AsTask();
    public override Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) => _db.Set<Order>().ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Order>)t.Result, ct);
    public override Task<IReadOnlyList<Order>> FindAsync(ISpecification<Order> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), s).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Order>)t.Result, ct);
    public override Task<Order?> FindSingleAsync(ISpecification<Order> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), s).FirstOrDefaultAsync(ct);
    public override Task<int> CountAsync(ISpecification<Order> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), s).CountAsync(ct);
    public override Task<bool> AnyAsync(ISpecification<Order> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Order>(), s).AnyAsync(ct);

    protected override Task AddCoreAsync(Order e, CancellationToken ct) => _db.AddAsync(e, ct).AsTask();
    protected override Task AddRangeCoreAsync(IEnumerable<Order> es, CancellationToken ct) { _db.AddRange(es); return Task.CompletedTask; }
    protected override Task UpdateCoreAsync(Order e, CancellationToken ct) { _db.Update(e); return Task.CompletedTask; }
    protected override Task RemoveCoreAsync(Order e, CancellationToken ct) { _db.Remove(e); return Task.CompletedTask; }
    protected override Task RemoveRangeCoreAsync(IEnumerable<Order> es, CancellationToken ct) { _db.RemoveRange(es); return Task.CompletedTask; }
}
```

## Serialization helpers

- `[GenerateVoJsonConverter]` for VO JSON converter
- `[GenerateTypeConverter]` for VO type converter
- `[GenerateEfValueConverter]` for EF Core conversion (VO and Enumeration)

Examples:

```csharp
// Value object wrapper with JSON and TypeConverter
[GenerateVoJsonConverter]
[GenerateTypeConverter]
public sealed partial class Sku : ValueObject<Sku, string>
{ public Sku(string value) : base(value) { } }

// Serialize/Deserialize with System.Text.Json
var json = JsonSerializer.Serialize(new Sku("ABC-123"));
var deserialized = JsonSerializer.Deserialize<Sku>(json);

// Enumeration JSON converter with strict behavior
[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
public sealed partial class Method : Enumeration { /* ... */ }

// EF Core conversion: generator emits ValueConverter types you can reference
// e.g., modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion(new OrderStatusValueConverter());
```

## Integration tips

See also: [reference.md](reference.md), [examples.md](examples.md), and [best-practices.md](best-practices.md).

- EF Core: use `SpecificationEvaluator.GetQuery` to apply specs
- JSON: annotate VOs/Enumerations with generator attributes
- DI: register `IDomainEventDispatcher` and all `IDomainEventHandler` implementations

