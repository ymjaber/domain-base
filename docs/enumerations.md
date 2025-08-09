### Enumerations

`Enumeration` provides type-safe alternative to C# enums with behavior.

Authoring rules:
- Inherit from `DomainBase.Enumeration`
- Mark the class `partial` to enable generator helpers
- Define static readonly instances (e.g., `public static readonly Status Active = new(1, "Active");`)

Optional generation attributes:
- `[GenerateJsonConverter(Behavior = UnknownValueBehavior.ReturnNull | UnknownValueBehavior.ThrowException)]`
- `[GenerateEfValueConverter]`

Generated helpers:
- `GetAll()`
- `FromValue(int)` / `TryFromValue(int, out T)`
- `FromName(string)` / `TryFromName(string, out T)`

Analyzer diagnostics:
- DBENUM001 Duplicate enumeration value
- DBENUM002 Duplicate enumeration name
- DBENUM003 Enumeration class must be partial

Example:

```csharp
using DomainBase;

[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
public partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

