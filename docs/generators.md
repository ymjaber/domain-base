# Generators

This library ships two source generators:

- Enumeration generator: augments `Enumeration` subclasses
- Value object generator: augments `[ValueObject]` classes inheriting `ValueObject<TSelf>`

Both run at compile-time and emit partial class members and optional converters.

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

Attributes:
- `[GenerateJsonConverter(Behavior = UnknownValueBehavior.ReturnNull|ThrowException)]`
  - Emits `JsonConverter<T>` that reads/writes integer (value) or string (name)
  - On unknown token: returns null or throws (per `Behavior`)
- `[GenerateEfValueConverter]`
  - Emits `ValueConverter<T,int>` for EF Core

Example:
```csharp
[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
[GenerateEfValueConverter]
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

EF Core model:
```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasConversion(new OrderStatusValueConverter());
```

## Value object generator

Requirements:
- Class has `[ValueObject]` attribute
- Class inherits `ValueObject<TSelf>` and is `partial`

Annotate members with exactly one equality attribute:
- `[IncludeInEquality(Priority = 0)]`
- `[SequenceEquality(OrderMatters = true, DeepEquality = true, Priority = 0)]`
- `[CustomEquality(Priority = 0)]` (requires static methods)
- `[IgnoreEquality]`

Generated members:
- Optimized `EqualsCore(TSelf other)`
- Optimized `GetHashCodeCore()`
- Optional converters when attributes are present:
  - `[GenerateVoJsonConverter]` → `JsonConverter<T>` using the `Value` property for wrappers
  - `[GenerateEfValueConverter]` → `ValueConverter<T,object>` for wrappers
  - `[GenerateTypeConverter]` → `TypeConverter` (string round-tripping)

Custom equality static method contracts for member `Name`:
```csharp
private static void Equals_Name(in T value, in T otherValue, out bool result);
private static void GetHashCode_Name(in T value, ref HashCode hashCode);
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
    private static void Equals_LastName(in string v, in string o, out bool r) => r = string.Equals(v, o, StringComparison.OrdinalIgnoreCase);
    private static void GetHashCode_LastName(in string v, ref HashCode h) => h.Add(v.ToUpperInvariant());
}
```

Notes:
- Properties must be get-only or init-only; fields must be readonly
- One equality attribute per member
- Strings are not considered sequences for `[SequenceEquality]`