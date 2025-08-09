### Value Objects

Two approaches:
- Manual: inherit `ValueObject<TSelf>` and implement `EqualsCore`/`GetHashCodeCore`
- Generator-based: mark class with `[ValueObject]`, inherit `ValueObject<TSelf>` (or `ValueObject<TSelf,TValue>`), make it `partial`, and annotate members for equality

Attributes:
- `[IncludeInEquality(Priority = n)]`
- `[SequenceEquality(OrderMatters = true|false, DeepEquality = true|false, Priority = n)]`
- `[CustomEquality(Priority = n)]` with static methods:
  - `private static void Equals_{Member}(in T value, in T otherValue, out bool result)`
  - `private static void GetHashCode_{Member}(in T value, ref HashCode hashCode)`
- `[IgnoreEquality]`

Optional generators:
- `[GenerateVoJsonConverter]`
- `[GenerateEfValueConverter]`
- `[GenerateTypeConverter]` (for `ValueObject<TSelf, TValue>`)

Example:

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

