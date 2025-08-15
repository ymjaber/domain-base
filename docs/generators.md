# Generators

This library ships two source generators:

- Enumeration generator: augments `Enumeration` subclasses
- Value object generator: augments `[ValueObject]` classes inheriting `ValueObject<TSelf>`

Both run at compile-time and emit partial class members.

## Enumeration generator

Requirements:

- Class inherits from `DomainBase.Enumeration`
- Class is `partial`

Generated members:

- `static IReadOnlyCollection<T> GetAll()`
- `static T FromValue(int value)`
- `static T FromName(string name)`
- `static bool TryFromValue(int value, out T? result)`
- `static bool TryFromName(string name, out T? result)`

Example:

```csharp
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Approved  = new(2, "Approved");
    public OrderStatus(int value, string name) : base(value, name) { }
}
```

Usage:

```csharp
var submitted = OrderStatus.FromValue(1);
if (OrderStatus.TryFromName("Approved", out var status)) { /* ... */ }
```

## Value object generator

Requirements:

- Class has `[ValueObject]` attribute
- Class inherits `ValueObject<TSelf>` and is `partial`

Annotate members with exactly one equality attribute:

- `[IncludeInEquality(Order = 0)]` (formerly `Priority`)
- `[SequenceEquality(OrderMatters = true, DeepEquality = true, Order = 0)]` (formerly `Priority`)
- `[CustomEquality(Order = 0)]` (formerly `Priority`, requires static methods)
- `[IgnoreEquality]`

Ordering rules:

- Members are evaluated by `Order` ascending (unspecified defaults to 0).
- If multiple members share the same `Order`, evaluation among them follows declaration order; a warning is reported if you assign the same explicit `Order` to multiple members.

Generated members:

- Optimized `EqualsCore(TSelf other)`
- Optimized `GetHashCodeCore()`

Custom equality static method contracts for member `Name`:

```csharp
private static bool Equals_Name(in T value, in T otherValue);
private static int GetHashCode_Name(in T value);
```

Examples:

```csharp
[ValueObject]
public sealed partial class Address : ValueObject<Address>
{
    [IncludeInEquality] public string City  { get; init; }
    [IncludeInEquality] public string Street{ get; init; }
}

[ValueObject]
public sealed partial class Person : ValueObject<Person>
{
    [IncludeInEquality] public string FirstName { get; init; } = "";
    [CustomEquality] public string LastName { get; init; } = "";
    private static bool Equals_LastName(in string v, in string o) => string.Equals(v, o, StringComparison.OrdinalIgnoreCase);
    private static int GetHashCode_LastName(in string v) => StringComparer.OrdinalIgnoreCase.GetHashCode(v);
}
```

Notes:

- One equality attribute can be added per member

---

Previous: [Guide](guide.md) · Next: [Analyzers](analyzers.md)

