# Getting started

This guide takes you from install to a working, ergonomic domain model with value objects, enumerations, entities/aggregates, domain events, specifications, and repositories. It goes beyond the Quick start in the root README with richer guidance, integration tips, and practical examples.

## Table of contents

- [Prerequisites](#prerequisites)
- [Install](#install)
- [Project setup](#project-setup)
- [What you get](#what-you-get)
- [First 10 minutes (hands-on)](#first-10-minutes-hands-on)
- [JSON and EF Core integration](#json-and-ef-core-integration)
- [Querying with specifications](#querying-with-specifications)
- [Repository pattern (optional)](#repository-pattern-optional)
- [Tips, pitfalls, and conventions](#tips-pitfalls-and-conventions)
- [Next steps](#next-steps)

## Prerequisites

- .NET SDK 9
- Any .NET app: console, web, or test project

## Install

```bash
dotnet add package DomainBase
```

NuGet: [DomainBase](https://www.nuget.org/packages/DomainBase/)

## Project setup

No special MSBuild configuration is required. Source generators and analyzers are included automatically when you reference the package.

Keep in mind:

- Make classes `partial` when using the generators.
- Rich value objects require the `[ValueObject]` attribute and must inherit `ValueObject<TSelf>`.
- Simple single-property wrappers should inherit `ValueObject<TSelf, TValue>` and do not use `[ValueObject]`.

## What you get

- `Entity<TId>`, `AggregateRoot<TId>`: identity-based equality and domain events
- `ValueObject<TSelf>`, `ValueObject<TSelf,TValue>`: value equality with generators and analyzers
- `Enumeration`: richer alternative to enums with generator helpers
- `DomainEvent` + dispatcher and handlers
- `Specification<T>` + `SpecificationEvaluator`
- `Repository<TEntity,TId>` abstraction
- Analyzers and code fixes for correctness and ergonomics

## First 10 minutes (hands-on)

1) Single-property value object (wrapper)

```csharp
using DomainBase;

[GenerateVoJsonConverter] // optional: generates System.Text.Json converter
[GenerateTypeConverter]   // optional: enables binding/round-tripping from strings
public sealed partial class OrderId : ValueObject<OrderId, Guid>
{
    public OrderId(Guid value) : base(value)
    {
        if (value == Guid.Empty) throw new ArgumentException("OrderId cannot be empty", nameof(value));
    }
}
```

Notes:
- Do not annotate wrappers with `[ValueObject]`. Wrappers already implement equality via `ValueObject<TSelf,TValue>`.
- Prefer this wrapper for single values instead of custom equality attributes.

2) Rich value object (multiple members and generated equality)

```csharp
using DomainBase;

[ValueObject]
public sealed partial class Money : ValueObject<Money>
{
    [IncludeInEquality] public decimal Amount  { get; init; }
    [IncludeInEquality] public string  Currency{ get; init; } = "USD";

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = amount;
        Currency = string.IsNullOrWhiteSpace(currency) ? throw new ArgumentNullException(nameof(currency)) : currency;
    }
}
```

Guidance:
- Annotate each member with exactly one equality attribute. Start with `[IncludeInEquality]`.
- Reserve `[CustomEquality]` only when a member needs special comparison (e.g., case-insensitive strings).

3) Smart enumeration with helpers

```csharp
using DomainBase;

[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)] // optional
[GenerateEfValueConverter]                                            // optional
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Approved  = new(2, "Approved");
    public static readonly OrderStatus Rejected  = new(3, "Rejected");

    public OrderStatus(int value, string name) : base(value, name) { }

    public bool IsFinal() => this == Approved || this == Rejected;
}
```

4) Aggregate root and domain events

```csharp
using DomainBase;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }

    public bool Submitted { get; private set; }

    public void Submit()
    {
        if (Submitted) return;
        Submitted = true;
        AddDomainEvent(new OrderSubmitted(Id));
    }
}

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;
```

5) Wire up the dispatcher and handlers (DI)

```csharp
// Startup/Program.cs (example)
services.AddSingleton<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
// Register handlers using the non-generic interface
services.AddScoped<IDomainEventHandler, OrderSubmittedHandler>();

public sealed class OrderSubmittedHandler : IDomainEventHandler<OrderSubmitted>
{
    public Task HandleAsync(OrderSubmitted e, CancellationToken ct = default)
    {
        // react (send email, enqueue workflow, etc.)
        return Task.CompletedTask;
    }
}
```

## JSON and EF Core integration

- Value object wrappers: add `[GenerateVoJsonConverter]` and optionally `[GenerateTypeConverter]`.
- Enumerations: add `[GenerateJsonConverter]` and `[GenerateEfValueConverter]`.

EF Core snippet (for enumerations):

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasConversion(new OrderStatusValueConverter());
```

System.Text.Json works automatically once the generated converters are in your assembly.

## Querying with specifications

```csharp
public sealed class OrdersByStatus : Specification<Order>
{
    public OrdersByStatus(OrderStatus status) : base(o => o.Status == status) { }
}

// Apply to IQueryable (e.g., EF Core DbSet<Order>)
var submitted = SpecificationEvaluator.GetQuery(db.Orders, new OrdersByStatus(OrderStatus.Submitted));
```

## Repository pattern (optional)

Use `Repository<TEntity,TId>` to centralize persistence and automatically dispatch domain events after saves.

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

## Tips, pitfalls, and conventions

- Make generator-backed classes `partial`.
- For rich value objects, apply exactly one equality attribute per member.
- Strings are not sequences for `[SequenceEquality]`.
- Register event handlers via the non-generic `IDomainEventHandler` interface in DI.
- Prefer wrappers (`ValueObject<TSelf,TValue>`) for single values; avoid using equality attributes there.
- Reserve `[CustomEquality]` for complex cases where a member needs special comparison or hashing.

## Next steps

- Follow the full tutorial: [tutorial.md](tutorial.md)
- See detailed usage and options: [guide.md](guide.md)
- Dive into the API: [reference.md](reference.md)
- Learn about generators: [generators.md](generators.md)
- Explore best practices: [best-practices.md](best-practices.md)
