# DomainBase Guide

## Table of contents

- [Overview and install](#overview-and-install)
- [Entities and aggregate roots (with auditing)](#entities-and-aggregate-roots)
- [Value objects: wrappers, manual, generator-driven](#value-objects)
- [Equality attributes (Include, Ignore, Sequence, Custom)](#equality-attributes-generator-driven-vos)
- [Enumerations and generated helpers](#enumerations)
- [Domain events, metadata, handlers, and dispatcher](#domain-events-handlers-and-dispatcher)
- [Analyzers and code fixes (what the IDE enforces for you)](#analyzers-and-code-fixes)
- [Exceptions and guidance](#exceptions-and-guidance)
- [Domain services](#domain-services)
- [Full walk‑through example (mini order domain)](#full-walk-through-example-mini-order-domain)
- [Integration tips and best practices](#integration-tips-and-best-practices)

---

## Overview and install

DomainBase provides pragmatic DDD building blocks for .NET:

- Entities and aggregate roots with identity equality and domain events
- Value objects with safe, readable and maintainable equality options
- Smart enumerations with lookup helpers
- Domain events base
- Analyzers and code fixes to keep your model

Install:

```bash
dotnet add package DomainBase
```

---

## Entities

- `Entity<TId>` gives identity-based equality and an `Id`.
- Entities compare by Id and type

```csharp
public sealed class OrderItem : Entity<Guid>
{
    public OrderItem(Guid id, string sku, int quantity) : base(id)
    { Sku = sku; Quantity = quantity; }

    public string Sku { get; private set; }
    public int Quantity { get; private set; }
}
```

## Aggregate roots

- `AggregateRoot<TId>` adds `DomainEvents` and protected `AddDomainEvent`, plus `ClearDomainEvents`.

```csharp
public sealed class Customer : AggregateRoot<Guid>
{
    public Customer(Guid id) : base(id) { }
}
```

Notes:

- Prefer small, behavior-rich aggregates. Raise domain events for important changes that need to be consumed by the upper layers.

---

## Value objects

Choose one of three styles:

1. Single-property wrapper: inherit `ValueObject<TSelf, TValue>`

```csharp
public sealed class Email : ValueObject<Email, string>
{
    public Email(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));
    }
}
```

1. Manual overrides: inherit `ValueObject<TSelf>` and implement `EqualsCore`/`GetHashCodeCore` yourself

This is a common approach within teams implementing DDD concepts. (However I only recommend using it in rare cases as the next 3rd approach is better in my opinion)

```csharp
public sealed class Percentage : ValueObject<Percentage>
{
    public decimal Value { get; }
    public Percentage(decimal value) => Value = value;

    protected override bool EqualsCore(Percentage other) => Value == other.Value;
    protected override int GetHashCodeCore() => Value.GetHashCode();
}
```

3. Generator-driven: mark a partial class with `[ValueObject]` and annotate members

```csharp
[ValueObject]
public sealed partial class Address : ValueObject<Address>
{
    [IncludeInEquality] public string Country   { get; init; } = "Palestine";
    [IncludeInEquality] public string City { get; init; } = "Jenin";
    [IncludeInEquality] public string Street { get; init; } = "Abu-Bakr";
}
```

Guidance:

- Use wrappers or manual overrides for single-property VOs; do not annotate wrappers with `[ValueObject]`.
- Reserve `[CustomEquality]` for complex VOs where a member needs special comparison.
- Value Objects should be immutable by design, so properties must be get-only or init-only and fields must be readonly.

### Rules (generator-driven)

- The class must be `partial` and inherit `ValueObject<TSelf>`.
- Only fields and auto-properties are supported for equality attributes (no accessor bodies or expression bodies).
- Apply exactly one equality attribute per participating member.
- Properties must be get-only or init-only; fields must be `readonly`.
- `[SequenceEquality]` requires an enumerable/enumerator type (implements `IEnumerable`/`IEnumerable<T>` or `IEnumerator`/`IEnumerator<T>`); `string` is excluded.
- Global namespace is supported for generated code.

Edge cases:

- One equality attribute per member — analyzers flag multiple attributes.
- For floating-point tolerance (and other special equality behavior), use `[CustomEquality]` on the numeric member and provide tolerant comparison/hash code.
- A later update will add attributes that extend some functionalities (like floating-point tolerance shortcuts, string case-insensitive comparisons, etc).

### Ordering & evaluation

- Members are compared by `Order` ascending; unspecified `Order` defaults to 0.
- If multiple members share the same `Order`, evaluation among them follows declaration order (a warning is reported by the analyzer for duplicate explicit Orders).
- Equality short-circuits on first mismatch; hash code combines members in the same order.

### Sequence equality details

- `OrderMatters = true`: sequences must match element-by-element.
- `OrderMatters = false`: sequences compare as multisets (counts must match, any order). Hashing is order-insensitive in this mode.
- `DeepEquality = true`: elements compared via their `Equals`; `false` uses reference equality for reference types.

### Custom equality method naming

- For a member named `Name`, provide:
    - `private static bool Equals_Name(in T value, in T otherValue)`
    - `private static int  GetHashCode_Name(in T value)`
- Method suffix cleaning removes leading `_`/`m_` (e.g., `_lastName` → `LastName`). Ensure uniqueness after cleaning to avoid analyzer errors.

### Equality attributes (generator-driven VOs)

- `IncludeInEquality(int order = 0)`: include member in equality. Lower order is evaluated first.
- `IgnoreEquality`: exclude member from equality.
- `SequenceEquality(bool OrderMatters = true, bool DeepEquality = true, int order = 0)`: for testing the equality of member of types implementing `IEnumerable`, `IEnumerator`, `IEnumerable<T>`, `IEnumerator<T>` rather than the collections themselves.
- `CustomEquality(int order = 0)`: provide static methods on the VO for that member:
    - `private static bool Equals_{Name}(in T value, in T otherValue)`
    - `private static int  GetHashCode_{Name}(in T value)`

Example with ordering:

```csharp
[ValueObject]
public sealed partial class PersonName : ValueObject<PersonName>
{
    [IncludeInEquality(10)] public string FirstName { get; init; } = "Yousef";
    [CustomEquality(5)]     public string LastName  { get; init; } = "Jaber";

    private static bool Equals_LastName(string lastName, string otherLastName) => lastName == otherLastName;
    private static int  GetHashCode_LastName(string lastName) => lastName.GetHashCode();
}
```

Collections:

```csharp
[ValueObject]
public sealed partial class Basket : ValueObject<Basket>
{
    [SequenceEquality(OrderMatters = false, DeepEquality = true)]
    public IReadOnlyList<string> ItemSkus { get; init; } = Array.Empty<string>();
}
```

---

## Enumerations

Define a `partial` class inheriting `Enumeration` with static instances; the generator adds lookup and parsing helpers. Enumerations shine when you add behavior.

```csharp
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Draft     = new(0, "Draft");
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Paid      = new(2, "Paid");
    public static readonly OrderStatus Shipped   = new(3, "Shipped");
    public static readonly OrderStatus Cancelled = new(9, "Cancelled");

    private OrderStatus(int value, string name) : base(value, name) { }

    // Domain-specific behavior
    public bool CanSubmit() => this == Draft;
    public bool CanPay()    => this == Submitted;
    public bool CanShip()   => this == Paid;
    public bool IsTerminal  => this == Shipped || this == Cancelled;
}

var all        = OrderStatus.GetAll();
var submitted  = OrderStatus.FromValue(1);
var exists     = OrderStatus.TryFromName("Shipped", out var shipped);

Edge cases:

- Ensure uniqueness of both `Value` and `Name` per type — analyzers report duplicates.
- Keep the constructor private and the class `partial`.
- Global namespace is supported for generated code.
```

---

## Domain events, handlers, and dispatcher

```csharp
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

Notes:

- Keep event data minimal — only what consumers need.
- If you use MediatR, you can derive your events from a thin abstraction that implements both `DomainEvent` and `INotification`.
```

---

## Analyzers and code fixes

The package includes analyzers that run in your IDE and build to prevent mistakes and nudge best practices. Common diagnostics:

- DBVO001/DBEN003: class must be `partial` for generators
- DBVO002: member missing equality attribute
- DBVO003/DBVO004: missing custom equality methods for `[CustomEquality]`
- DBVO005: multiple equality attributes on the same member
- DBVO006: `[SequenceEquality]` on non-sequence type
- DBVO007: equality attribute outside a `[ValueObject]` class
- DBVO008: `[ValueObject]` class not inheriting `ValueObject<TSelf>`
- DBVO010/DBVO011: immutability rules (get-only/init-only/readonly)
- DBVO012: extra members in simple wrappers
- DBVO013: duplicate `Order` among equality members (falls back to declaration order)
- DBVO014: `[ValueObject]` unnecessary on simple wrapper
- DBVO015: equality attribute on unsupported member (only fields/auto-properties)

Bundled code fixes help you resolve these quickly (add `partial`, add equality attribute, generate custom methods).

See also: `docs/analyzers.md`, `docs/code-fixes.md`, `docs/generators.md`.

---

## Exceptions and guidance

Use rich, intention-revealing exceptions to capture domain rule failures. All derive from `DomainException`.

- `DomainValidationException(string message)`
- `DomainValidationException(string propertyName, string errorMessage)`
- `DomainConflictException(string message)`
- `DomainNotFoundException(string resourceName, object id)`
- `DomainNotFoundException<TId>(string resourceName, TId id)`

Notes:

- Prefer the specific exception that best expresses the breach (validation vs not-found vs conflict).

---

## Domain services

`IDomainService` is a simple marker interface to host domain logic that doesn’t naturally fit within an entity or a value object (coordination, cross-aggregate logic, policies). Keep services thin; model behavior in entities and VOs first.

---

## Full walk‑through example: mini order domain

This condenses a realistic flow using Arabic names to keep it friendly.

1. Value objects

```csharp
// Single-property wrapper
public sealed class ProjectDescription : ValueObject<ProjectDescription, string>
{
    public ProjectDescription(string value) : base(value)
    {
        if (value.Length < 5) throw new ArgumentException("Too short");
    }
}

// Generator-driven VO
[ValueObject]
public sealed partial class Money : ValueObject<Money>
{
    [IncludeInEquality] public decimal Amount  { get; init; }
    [IncludeInEquality] public string  Currency{ get; init; } = "JOD";
    public Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
}
```

2. Enumeration

```csharp
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Draft     = new(0, "Draft");
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Paid      = new(2, "Paid");
    public static readonly OrderStatus Shipped   = new(3, "Shipped");

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

3. Entity and aggregate root

```csharp
public sealed class OrderItem : Entity<Guid>
{
    public OrderItem(Guid id, string sku, int quantity, Money unitPrice) : base(id)
    { Sku = sku; Quantity = quantity; UnitPrice = unitPrice; }

    public string Sku { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money Subtotal() => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { Status = OrderStatus.Draft; Total = new Money(0, "AED"); }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    public void Submit()
    {
        if (Status != OrderStatus.Draft) throw new DomainConflictException("Only Draft orders can be submitted");
        Status = OrderStatus.Submitted;
        AddDomainEvent(new OrderSubmitted(Id));
    }

    public void Pay(Money amount)
    {
        if (Status != OrderStatus.Submitted) throw new DomainConflictException("Order must be Submitted");
        if (amount.Amount <= 0) throw new DomainValidationException(nameof(amount), "Amount must be positive");
        Total = amount; Status = OrderStatus.Paid; AddDomainEvent(new OrderPaid(Id, amount));
    }
}

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;
public sealed record OrderPaid(Guid OrderId, Money Amount) : DomainEvent;
```

5. Put it together

```csharp
var order = new Order(Guid.NewGuid());
order.Submit();
order.Pay(new Money(100, "AED"));
```

---

## Integration tips and best practices

Tips:

- Make generator-backed classes `partial`.
- For generator-driven VOs, apply exactly one equality attribute per member; strings are not sequences.
- Prefer wrappers for single values; avoid `[ValueObject]` and `[CustomEquality]` there.
- Reserve `[CustomEquality]` for special cases where a member requires custom comparison/hash code.

---

Next: [Generators](generators.md)
