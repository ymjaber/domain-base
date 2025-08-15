# API Reference (Cheat Sheet)

## Table of contents

- [Entities](#entities)
- [Aggregate roots](#aggregate-roots)
- [Value objects](#value-objects)
- [Equality attributes (VO)](#equality-attributes-vo)
- [Generator-driven VO requirements](#generator-driven-vo-requirements)
- [Enumerations](#enumerations)
- [Generated APIs (Enumeration)](#generated-apis-enumeration)
- [Generated APIs (ValueObject)](#generated-apis-valueobject)
- [Domain events](#domain-events)
- [Exceptions](#exceptions)
- [Services](#services)
- [Diagnostics quick ref](#diagnostics-quick-ref)

Namespaces: all public types live under `DomainBase`.

## Entities

- `abstract class Entity<TId> where TId : struct`
    - ctor(TId id)
    - `TId Id { get; }`
    - Operators: `==`, `!=`
    - Overrides: `Equals(object)`, `GetHashCode()`, `ToString()`
    - Notes: equality compares by concrete type and non-default `Id`.

## Aggregate roots

- `abstract class AggregateRoot<TId> : Entity<TId> where TId : struct`
    - `IReadOnlyCollection<DomainEvent> DomainEvents { get; }`
    - `void ClearDomainEvents()`
    - `protected void AddDomainEvent(DomainEvent domainEvent)`

## Value objects

- `abstract class ValueObject<TSelf> : IEquatable<TSelf>`
    - Operators: `==`, `!=`
    - `override bool Equals(object)` → type + `EqualsCore`
    - `override int GetHashCode()` → combines type + `GetHashCodeCore`
    - `protected abstract bool EqualsCore(TSelf other)`
    - `protected abstract int GetHashCodeCore()`

- `abstract class ValueObject<TSelf, TValue> : ValueObject<TSelf>`
    - ctor(TValue value)
    - `TValue Value { get; }`
    - `static implicit operator TValue(ValueObject<TSelf, TValue> valueObject)`
    - `override string ToString()` → `Value.ToString()`
    - Implements `EqualsCore`/`GetHashCodeCore` based on `Value`

- `sealed class ValueObjectAttribute : Attribute`
    - Marks a `partial` VO class to enable generated equality.

## Equality attributes (VO)

- `IncludeInEqualityAttribute`
    - Ctors: `()`, `(int order)`
    - Props: `int Order { get; set; } = 0`

- `IgnoreEqualityAttribute`
    - Excludes member from equality.

- `SequenceEqualityAttribute`
    - Ctors: `()`, `(int order)`
    - Props: `bool OrderMatters { get; set; } = true`, `bool DeepEquality { get; set; } = true`, `int Order { get; set; } = 0`
    - Note: strings are not treated as sequences.

- `CustomEqualityAttribute`
    - Ctors: `()`, `(int order)`
    - Props: `int Order { get; set; } = 0`
    - Requires, per annotated member name `<Name>`:
        - `private static bool Equals_<Name>(in T value, in T otherValue)`
        - `private static int  GetHashCode_<Name>(in T value)`

## Generator-driven VO requirements

- Apply `[ValueObject]` to a `partial` class inheriting `ValueObject<TSelf>`.
- Members participating in equality must be fields or auto-properties.
- Exactly one equality attribute per member.
- Immutability: properties must be get-only or init-only; fields must be `readonly`.
- Do not apply `[ValueObject]` to simple wrappers (`ValueObject<TSelf, TValue>`).
- `[SequenceEquality]` requires an enumerable/enumerator type (not `string`).

## Enumerations

- `abstract class Enumeration : IComparable`
    - `string Name { get; }`, `int Value { get; }`
    - Operators: `<, <=, >, >=, ==, !=`
    - Overrides: `ToString()`, `Equals(object)`, `GetHashCode()`

Usage pattern:

```csharp
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Draft     = new(0, "Draft");
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    private OrderStatus(int value, string name) : base(value, name) { }
}
```

Notes:

- Class must be `partial` for helper generation.
- Use `static readonly` instances; keep constructor private.
- Ensure unique `Value` and `Name` per type (diagnostics enforce this when literals are used).

## Generated APIs (Enumeration)

For each `partial` class inheriting `Enumeration`:

- `static IReadOnlyCollection<T> GetAll()`
- `static T FromValue(int value)`
- `static T FromName(string name)`
- `static bool TryFromValue(int value, out T? result)`
- `static bool TryFromName(string name, out T? result)`

Example:

```csharp
var all = OrderStatus.GetAll();
var submitted = OrderStatus.FromValue(1);
var ok = OrderStatus.TryFromName("Draft", out var draft);
```

## Generated APIs (ValueObject)

- The generator emits optimized overrides for `Equals(object)`, `EqualsCore`, and `GetHashCodeCore` based on annotated members and their `Order`.

## Domain events

- `abstract record DomainEvent`
    - `Guid Id { get; }`
    - `DateTimeOffset OccurredOn { get; }`
    - `string GetEventName()` (returns simple type name; `virtual`)
    - Constructors:
        - Default: `Id` = Guid v7, `OccurredOn` = `UtcNow`
        - `(Guid id, DateTimeOffset occurredOn)`

Example:

```csharp
public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;
```

## Exceptions

- `abstract class DomainException : Exception`
- `class DomainValidationException : DomainException`
    - `DomainValidationException(string message)` (object-level)
    - `DomainValidationException(string propertyName, string errorMessage)` (single-property)
    - `string? PropertyName { get; }`
- `class DomainConflictException : DomainException`
    - `DomainConflictException(string message)`
- `class DomainNotFoundException : DomainException`
    - `DomainNotFoundException(string resourceName, object id)`
    - Props: `string ResourceName`, `object Id`
- `class DomainNotFoundException<TId> : DomainException`
    - `DomainNotFoundException(string resourceName, TId id)`
    - Props: `string ResourceName`, `TId Id`

## Services

- `interface IDomainService` (marker for domain services)

Related: [guide.md](guide.md), [examples.md](examples.md).

## Diagnostics quick ref

ValueObject (DBVO):

- DBVO001: class must be `partial`
- DBVO002: member missing equality attribute
- DBVO003: missing `Equals_Name`
- DBVO004: missing `GetHashCode_Name`
- DBVO005: multiple equality attributes on member
- DBVO006: `[SequenceEquality]` on non-sequence type
- DBVO007: equality attribute on non-`[ValueObject]` class
- DBVO008: `[ValueObject]` without inheriting `ValueObject<TSelf>`
- DBVO009: duplicate custom method names after cleaning
- DBVO010: mutable property (must be get-only or init-only)
- DBVO011: mutable field (must be `readonly`)
- DBVO012: extra members in simple wrappers
- DBVO013: duplicate `Order` among equality members (falls back to declaration order)
- DBVO014: `[ValueObject]` unnecessary on simple wrapper
- DBVO015: equality attribute on unsupported member (only fields/auto-properties)

Enumeration (DBEN):

- DBEN001: duplicate enumeration value
- DBEN002: duplicate enumeration name
- DBEN003: enumeration class must be `partial`

---

Previous: [Code fixes](code-fixes.md) · Next: [Examples](examples.md)
