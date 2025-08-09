### Quick Start

This page introduces the basic building blocks using small, focused examples.

1) Entities

```csharp
using DomainBase;

public class Customer : Entity<Guid>
{
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Customer(Guid id, string name, Email email) : base(id)
    {
        Name = name;
        Email = email;
    }
}
```

2) Value Objects (generator-based)

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

3) Enumerations

```csharp
using DomainBase;

public partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");

    private OrderStatus(int value, string name) : base(value, name) { }
}

var all = OrderStatus.GetAll();
var shipped = OrderStatus.FromName("Shipped");
```

4) Specifications

```csharp
using DomainBase;

public class ActiveCustomerSpec : Specification<Customer>
{
    public ActiveCustomerSpec() : base(c => c.IsActive) { }
}
```

