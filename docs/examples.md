# Real-world Examples

This page extends the quick start by showing cohesive examples that combine entities, value objects, enumerations, and domain events.

## Example 1: Customer domain with behavior-rich Enumeration

```csharp
// Value objects
[ValueObject]
public sealed partial class FullName : ValueObject<FullName>
{
[IncludeInEquality(Order = 10)] public string First { get; init; }
[IncludeInEquality(Order = 10)] public string Last  { get; init; }
}

// Enumeration
public sealed partial class CustomerStatus : Enumeration
{
    public static readonly CustomerStatus Active   = new(1, "Active");
    public static readonly CustomerStatus Inactive = new(2, "Inactive");
    private CustomerStatus(int value, string name) : base(value, name) { }

    public bool CanDeactivate() => this == Active;
}

// Aggregate
public sealed class Customer : AggregateRoot<Guid>
{
    public Customer(Guid id, FullName name) : base(id)
    { Name = name; Status = CustomerStatus.Active; }

    public FullName Name { get; private set; }
    public CustomerStatus Status { get; private set; }

    public void Deactivate(string reason)
    {
        if (!Status.CanDeactivate()) return;
        Status = CustomerStatus.Inactive;
        AddDomainEvent(new CustomerDeactivated(Id, reason));
    }
}

public sealed record CustomerDeactivated(Guid CustomerId, string Reason) : DomainEvent;

// Usage
var name = new FullName { First = "Ahmad", Last = "Saleh" };
var customer = new Customer(Guid.NewGuid(), name);
customer.Deactivate("Requested by user");
```

## Example 2: Basket with sequence equality

```csharp
[ValueObject]
public sealed partial class Basket : ValueObject<Basket>
{
    // Order of items doesn't matter, but we want deep equality of elements
    [SequenceEquality(OrderMatters = false, DeepEquality = true)]
    public IReadOnlyList<string> ItemSkus { get; init; } = Array.Empty<string>();
}

var basket1 = new Basket { ItemSkus = new[] { "SKU1", "SKU2", "SKU2" } };
var basket2 = new Basket { ItemSkus = new[] { "SKU2", "SKU1", "SKU2" } };
var equal = basket1 == basket2; // true
```

## Example 3: Custom equality for tolerant decimals

```csharp
[ValueObject]
public sealed partial class Temperature : ValueObject<Temperature>
{
    [CustomEquality]
    public decimal Celsius { get; init; }

    private const decimal Tolerance = 0.01m;
    private static bool Equals_Celsius(in decimal a, in decimal b) => Math.Abs(a - b) <= Tolerance;
    private static int  GetHashCode_Celsius(in decimal a) => Math.Round(a / Tolerance).GetHashCode();
}
```

## Example 4: Aggregate root with DomainService

```csharp
public interface IPricingService : IDomainService
{
    Money CalculateTotal(IEnumerable<OrderItem> items, string currency);
}

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    public Order(Guid id) : base(id) { Status = OrderStatus.Draft; Total = new Money(0, "JOD"); }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items;

    public void AddItem(string sku, int qty, Money unitPrice)
    {
        if (Status != OrderStatus.Draft) throw new DomainConflictException("Can only add items in Draft");
        _items.Add(new OrderItem(Guid.NewGuid(), sku, qty, unitPrice));
    }

    public void Reprice(IPricingService pricing)
    {
        if (Status != OrderStatus.Draft) throw new DomainConflictException("Can only reprice in Draft");
        Total = pricing.CalculateTotal(_items, Total.Currency);
    }
}
```

## Example 5: Simple wrappers and manual value objects

```csharp
// Single-property wrapper (value equality comes for free)
public sealed class PhoneNumber : ValueObject<PhoneNumber, string>
{
    public PhoneNumber(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Phone cannot be empty");
    }
}

// Manual equality
public sealed class Percentage : ValueObject<Percentage>
{
    public decimal Value { get; }
    public Percentage(decimal value) => Value = value;
    protected override bool EqualsCore(Percentage other) => Value == other.Value;
    protected override int GetHashCodeCore() => Value.GetHashCode();
}
```

## Example 6: IncludeInEquality with ordering

```csharp
[ValueObject]
public sealed partial class PersonName : ValueObject<PersonName>
{
    // LastName compared first, then FirstName
    [CustomEquality(Order = 5)] public string LastName  { get; init; } = "Jaber";
    [IncludeInEquality(Order = 10)] public string FirstName { get; init; } = "Yousef";

    private static bool Equals_LastName(in string a, in string b) => string.Equals(a, b, StringComparison.Ordinal);
    private static int  GetHashCode_LastName(in string a) => a.GetHashCode();
}
```

## Example 7: IgnoreEquality for non-essential data

```csharp
[ValueObject]
public sealed partial class DocumentInfo : ValueObject<DocumentInfo>
{
    [IncludeInEquality] public string Title { get; init; } = "Proposal";
    [IgnoreEquality]   public DateTimeOffset LastModified { get; init; } = DateTimeOffset.UtcNow;
}
```

## Example 8: Sequence equality variants (order/element comparison)

```csharp
// Item type with value equality
public sealed class Tag : ValueObject<Tag, string>
{ public Tag(string value) : base(value) { } }

[ValueObject]
public sealed partial class TagList : ValueObject<TagList>
{
    // Order-sensitive, deep element equality
    [SequenceEquality(OrderMatters = true, DeepEquality = true)]
    public IReadOnlyList<Tag> Items { get; init; } = Array.Empty<Tag>();
}

[ValueObject]
public sealed partial class TagBagByReference : ValueObject<TagBagByReference>
{
    // Order-insensitive, reference equality for elements
    [SequenceEquality(OrderMatters = false, DeepEquality = false)]
    public IReadOnlyList<Tag> Items { get; init; } = Array.Empty<Tag>();
}

// Usage
var list1 = new TagList { Items = new[] { new Tag("a"), new Tag("b") } };
var list2 = new TagList { Items = new[] { new Tag("a"), new Tag("b") } };
var eq1 = list1 == list2; // true (deep equality)

var t1 = new Tag("a"); var t2 = new Tag("a");
var bag1 = new TagBagByReference { Items = new[] { t1, t2 } };
var bag2 = new TagBagByReference { Items = new[] { new Tag("a"), new Tag("a") } };
var eq2 = bag1 == bag2; // false (reference equality required)
```

## Example 9: Fields and auto-properties in generator-driven VOs

```csharp
[ValueObject]
public sealed partial class GeoPoint : ValueObject<GeoPoint>
{
    [IncludeInEquality] private readonly double _lat;
    [IncludeInEquality] private readonly double _lng;

    public GeoPoint(double lat, double lng) { _lat = lat; _lng = lng; }
}
```

## Example 10: Custom equality on a field with name cleaning

```csharp
[ValueObject]
public sealed partial class Person : ValueObject<Person>
{
    [CustomEquality] private readonly string _lastName; // suffix cleans to "LastName"
    [IncludeInEquality] public string FirstName { get; init; } = "Ahmad";

    public Person(string firstName, string lastName)
    { FirstName = firstName; _lastName = lastName; }

    private static bool Equals_LastName(in string a, in string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static int  GetHashCode_LastName(in string a) => StringComparer.OrdinalIgnoreCase.GetHashCode(a);
}
```

## Example 11: Global namespace support (no namespace)

```csharp
public sealed partial class ShipmentStatus : Enumeration
{
    public static readonly ShipmentStatus Pending  = new(0, "Pending");
    public static readonly ShipmentStatus Shipped  = new(1, "Shipped");
    public static readonly ShipmentStatus Delivered= new(2, "Delivered");

    private ShipmentStatus(int value, string name) : base(value, name) { }
}

var delivered = ShipmentStatus.FromName("Delivered");
```

## Example 12: Exceptions in practice

```csharp
void ValidateAmount(decimal amount)
{
    if (amount <= 0)
        throw new DomainValidationException(nameof(amount), "Amount must be positive");
}

T GetOrThrow<T, TId>(IDictionary<TId, T> store, TId id)
{
    return store.TryGetValue(id, out var value)
        ? value
        : throw new DomainNotFoundException<TId>("Order", id);
}

void EnsureDraft(bool isDraft)
{
    if (!isDraft)
        throw new DomainConflictException("Only Draft orders can proceed");
}
```

## Example 13: Enumeration helper APIs

```csharp
public sealed partial class PaymentStatus : Enumeration
{
    public static readonly PaymentStatus Pending = new(0, "Pending");
    public static readonly PaymentStatus Paid    = new(1, "Paid");
    private PaymentStatus(int value, string name) : base(value, name) { }
}

var all = PaymentStatus.GetAll();
var paid = PaymentStatus.FromValue(1);
if (PaymentStatus.TryFromName("Pending", out var pending))
{
    // use pending
}
```

## Example 14: ValueObject in the global namespace

```csharp
[ValueObject]
public sealed partial class Coordinates : ValueObject<Coordinates>
{
    [IncludeInEquality] public double Latitude  { get; init; }
    [IncludeInEquality] public double Longitude { get; init; }
}

var gaza = new Coordinates { Latitude = 31.5, Longitude = 34.45 };
```

---

Previous: [Reference](reference.md) · Next: [Best practices](best-practices.md)
