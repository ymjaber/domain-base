# DomainBase Generators

This project contains source generators for the DomainBase library.

## Enumeration Generator

The Enumeration source generator augments `partial` classes that inherit from `DomainBase.Enumeration` with high-performance helper APIs, eliminating reflection and enabling AOT-friendly usage.

### Usage

1. Make your enumeration class inherit from `DomainBase.Enumeration`
2. Mark the class as `partial`
3. Optionally decorate the class with:
   - `[GenerateJsonConverter(Behavior = UnknownValueBehavior.ReturnNull | UnknownValueBehavior.ThrowException)]`
   - `[GenerateEfValueConverter]`

The generator creates:
- `IReadOnlyList<T> GetAll()`
- `T FromValue(int value)` / `bool TryFromValue(int value, out T result)`
- `T FromName(string name)` / `bool TryFromName(string name, out T result)`

### Example

```csharp
using DomainBase;

[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
public partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Processing = new(2, "Processing");
    public static readonly OrderStatus Shipped = new(3, "Shipped");
    public static readonly OrderStatus Delivered = new(4, "Delivered");

    private OrderStatus(int value, string name) : base(value, name) { }
}

// Usage
var all = OrderStatus.GetAll();
var status = OrderStatus.FromValue(2);
var shipped = OrderStatus.FromName("Shipped");
```

### Options

- `GenerateJsonConverter` - Generates a System.Text.Json converter (optional)
- `GenerateEfValueConverter` - Generates an EF Core value converter (optional)

### Benefits

- Performance: O(1) lookups, no reflection
- AOT-friendly: Works with Native AOT
- Validation: Duplicate values/names and `partial` requirement enforced by analyzers

### Compile-Time Validation

Authoring diagnostics (e.g., duplicate values/names, `partial` requirement) are enforced by `DomainBase.Analyzers`. The generator focuses solely on emitting source code.

## Value Object Generator

The Value Object generator augments classes marked with `[ValueObject]` and inheriting from `ValueObject<TSelf>` with optimized equality and hash code implementations. It also supports sequence equality and custom equality per member.

### Usage

1. Annotate your class with `[ValueObject]`
2. Inherit from `ValueObject<TSelf>` (or `ValueObject<TSelf, TValue>` for single-value VOs)
3. Make the class `partial`
4. Decorate members with one of:
   - `[IncludeInEquality(Priority = n)]`
   - `[SequenceEquality(OrderMatters = true|false, DeepEquality = true|false, Priority = n)]`
   - `[CustomEquality(Priority = n)]` and provide the static methods:
     - `private static void Equals_{Member}(in T value, in T otherValue, out bool result)`
     - `private static void GetHashCode_{Member}(in T value, ref HashCode hashCode)`

Optional generation:
- `[GenerateVoJsonConverter]` for System.Text.Json
- `[GenerateEfValueConverter]` for EF Core
- `[GenerateTypeConverter]` for `TypeConverter` (only for `ValueObject<TSelf, TValue>`)

### Example

```csharp
using DomainBase;

[ValueObject]
public partial class Email : ValueObject<Email>
{
    [CustomEquality]
    public string Value { get; init; }

    private static void Equals_Value(in string value, in string otherValue, out bool result)
        => result = string.Equals(value, otherValue, StringComparison.OrdinalIgnoreCase);

    private static void GetHashCode_Value(in string value, ref HashCode hash)
        => hash.Add(value, StringComparer.OrdinalIgnoreCase);
}
```
