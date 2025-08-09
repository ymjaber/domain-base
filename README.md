# DomainBase

[![NuGet](https://img.shields.io/nuget/v/DomainBase.svg)](https://www.nuget.org/packages/DomainBase/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

DomainBase is a comprehensive Domain-Driven Design (DDD) library for C#/.NET that provides essential building blocks for implementing rich domain models. Build maintainable domain layers with entities featuring identity-based equality, aggregate roots supporting domain events, immutable value objects, type-safe enumerations with behavior, and composable specifications for complex queries. Express your business rules clearly with clean abstractions that put domain logic where it belongs - in your domain model.

## Why DomainBase?

Traditional data-centric architectures in C# often lead to:

- **Anemic domain models** - Business logic scattered across services
- **Primitive obsession** - Using strings and ints instead of domain concepts
- **Inconsistent state** - No clear aggregate boundaries
- **Lost domain events** - Important business moments go unrecorded
- **Complex queries** - Business rules mixed with data access logic

DomainBase solves these problems by providing battle-tested DDD patterns that make your domain model the heart of your application.

## Key Features

- **🎯 Entity Base Classes** - Identity-based equality for domain objects
- **🏛️ Aggregate Roots** - Consistency boundaries with domain event support
- **💎 Value Objects** - Immutable objects with value-based equality and source generation
- **📢 Domain Events** - Capture important business moments
- **🔍 Specifications** - Composable query logic with AND/OR/NOT operations
- **🎨 Type-Safe Enumerations** - Smart enums with behavior and source generation
- **⚡ Zero Overhead** - Optimized implementations with minimal allocations
- **🛡️ Thread Safety** - Immutable value objects and thread-safe patterns
- **📦 Repository Abstractions** - Clean persistence interfaces
- **🔧 Domain Services** - Encapsulate cross-aggregate operations
- **📝 Comprehensive Docs** - Full API documentation and examples
- **✅ Battle Tested** - Production-ready with extensive test coverage
- **🚀 Performance First** - Designed for high-throughput scenarios
- **🎯 Best Practices** - Follows DDD patterns from Evans, Vernon, and Fowler

## Installation

### Core Package

```bash
dotnet add package DomainBase
```

Or via Package Manager:

```powershell
Install-Package DomainBase
```

## Quick Start

### Entity - Objects with Identity

```csharp
using DomainBase;

// Define an entity with identity
public class Customer : Entity<Guid>
{
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }

    public Customer(Guid id, string name, Email email) : base(id)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Customer is already inactive");

        IsActive = false;
    }
}

// Use the entity
var customerId = Guid.NewGuid();
var customer1 = new Customer(customerId, "John Doe", new Email("john@example.com"));
var customer2 = new Customer(customerId, "Jane Doe", new Email("jane@example.com"));

// Same ID = same entity, regardless of other properties
Console.WriteLine(customer1 == customer2); // True
```

### Value Object - Immutable Values

DomainBase provides two ways to create value objects:

#### Traditional Approach (Manual Implementation)

```csharp
using DomainBase;

// Define a value object
public class Email : ValueObject<Email>
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty");

        if (!value.Contains("@"))
            throw new ArgumentException("Invalid email format");

        Value = value.ToLowerInvariant();
    }

    protected override bool EqualsCore(Email other) => Value == other.Value;
    protected override int GetHashCodeCore() => Value.GetHashCode();
}

// Value objects are compared by value, not reference
var email1 = new Email("john@example.com");
var email2 = new Email("JOHN@EXAMPLE.COM");
Console.WriteLine(email1 == email2); // True (case-insensitive)
```

#### Source Generator Approach (New!)

```csharp
using DomainBase;

// Define a value object with source generation
[ValueObject]
public partial class Address : ValueObject<Address>
{
    [IncludeInEquality(Priority = 10)]
    public string Street { get; init; }
    
    [IncludeInEquality(Priority = 5)]
    public string City { get; init; }
    
    [IgnoreEquality] // Explicitly ignored in equality checks
    public DateTime CreatedAt { get; init; }
}

// Custom equality for case-insensitive email
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

// Sequence equality for collections
[ValueObject]
public partial class ShoppingCart : ValueObject<ShoppingCart>
{
    [IncludeInEquality]
    public string CartId { get; init; }
    
    [SequenceEquality(OrderMatters = false)] // Order doesn't matter
    public List<string> ItemIds { get; init; } = new();
    
    [SequenceEquality(OrderMatters = true)] // Order matters
    public List<decimal> PriceHistory { get; init; } = new();
}
```

The source generator approach provides:
- **Automatic equality implementation** - No need to implement GetEqualityComponents
- **Priority-based comparison** - Control the order of equality checks for performance
- **Custom equality logic** - Override comparison for specific properties
- **Sequence equality** - Built-in support for collection comparisons
- **Compile-time safety** - Analyzer warnings for missing attributes

### Aggregate Root - Consistency Boundary

```csharp
using DomainBase;

// Define an aggregate root that emits domain events
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount => _items.Sum(i => i.Price * i.Quantity);

    public Order(Guid id, Guid customerId) : base(id)
    {
        Status = OrderStatus.Pending;
        AddDomainEvent(new OrderCreatedEvent(id, customerId));
    }

    public void AddItem(string productName, decimal price, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to non-pending order");

        var item = new OrderItem(productName, price, quantity);
        _items.Add(item);

        AddDomainEvent(new OrderItemAddedEvent(Id, productName, quantity));
    }

    public void Ship()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be shipped");

        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id));
    }
}

// Domain events
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId) : DomainEvent;
public record OrderItemAddedEvent(Guid OrderId, string ProductName, int Quantity) : DomainEvent;
public record OrderShippedEvent(Guid OrderId) : DomainEvent;
```

### Type-Safe Enumeration

```csharp
using DomainBase;

// Define a type-safe enumeration with behavior
// Mark as partial to enable source-generated helpers
public partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");
    public static readonly OrderStatus Delivered = new(3, "Delivered");
    public static readonly OrderStatus Cancelled = new(4, "Cancelled");

    private OrderStatus(int id, string name) : base(id, name) { }

    // Add behavior to your enumerations
    public bool CanShip() => this == Pending;
    public bool IsFinal() => this == Delivered || this == Cancelled;

    public bool CanTransitionTo(OrderStatus newStatus)
    {
        return (this, newStatus) switch
        {
            (var current, var next) when current == next => false,
            (var current, _) when current.IsFinal() => false,
            _ => true
        };
    }
}

// Use the enumeration
var status = OrderStatus.Pending;
if (status.CanShip())
{
    // Ship the order
}

// Source generator provides static lookup/parse methods for partial Enumeration classes
var allStatuses = OrderStatus.GetAll();

// Parse from value or name
var shipped = OrderStatus.FromValue(2);
var pending = OrderStatus.FromName("Pending");
```

### Specifications - Encapsulated Queries

```csharp
using DomainBase;

// Define reusable specifications
public class ActiveCustomerSpecification : Specification<Customer>
{
    public ActiveCustomerSpecification()
        : base(c => c.IsActive && !c.IsDeleted) { }
}

public class PremiumCustomerSpecification : Specification<Customer>
{
    private readonly decimal _minimumPurchases;

    public PremiumCustomerSpecification(decimal minimumPurchases)
        : base(c => c.TotalPurchases >= minimumPurchases)
    {
        _minimumPurchases = minimumPurchases;
    }
}

// Compose specifications
var activePremiumSpec = new ActiveCustomerSpecification()
    .And(new PremiumCustomerSpecification(10000));

public class RecentActivitySpecification : Specification<Customer>
{
    public RecentActivitySpecification()
        : base(c => c.LastActivityDate > DateTime.UtcNow.AddDays(-7)) { }
}

var activeOrRecentSpec = new ActiveCustomerSpecification()
    .Or(new RecentActivitySpecification());

// Use with repositories
var customers = await repository.FindAsync(activePremiumSpec);

// Check individual entities
if (activePremiumSpec.IsSatisfiedBy(customer))
{
    // Apply premium benefits
}
```

## Examples

See examples and real-world walkthroughs in `https://github.com/ymjaber/domain-base/blob/main/docs/examples.md`.

## Documentation

The full documentation is available on GitHub:

- Getting Started: `https://github.com/ymjaber/domain-base/blob/main/docs/quick-start.md`
- Core Concepts: `https://github.com/ymjaber/domain-base/blob/main/docs/README.md`
- Generators & Analyzers: `https://github.com/ymjaber/domain-base/blob/main/docs/generators.md`, `https://github.com/ymjaber/domain-base/blob/main/docs/analyzers.md`
- Guides: `https://github.com/ymjaber/domain-base/blob/main/docs/ddd-best-practices.md`, `https://github.com/ymjaber/domain-base/blob/main/docs/ef-core.md`, `https://github.com/ymjaber/domain-base/blob/main/docs/serialization.md`, `https://github.com/ymjaber/domain-base/blob/main/docs/migration-guide.md`
- Examples: `https://github.com/ymjaber/domain-base/blob/main/docs/examples.md`

## Real-World Example

See `https://github.com/ymjaber/domain-base/blob/main/docs/examples.md#e-commerce-domain` for a complete e‑commerce walkthrough.

## Performance

DomainBase is designed for high performance:

- Zero-allocation equality checks for common scenarios
- Compiled expressions for specification queries
- Minimal boxing with generic constraints
- Immutable types prevent defensive copying
- Source generators for enumeration lookups

## Contributing

We welcome contributions! Please see our Contributing Guide: `https://github.com/ymjaber/domain-base/blob/main/docs/contributing.md`.

## Best Practices

1. **Make Your Domain Model Explicit**

    ```csharp
    // Don't use primitives
    public void ProcessOrder(string orderId, decimal amount) { }

    // Use domain concepts
    public void ProcessOrder(OrderId orderId, Money amount) { }
    ```

2. **Protect Your Invariants**

    ```csharp
    public class Order : AggregateRoot<OrderId>
    {
        private readonly List<OrderItem> _items = new();

        public void AddItem(Product product, int quantity)
        {
            // Enforce business rules
            if (Status != OrderStatus.Draft)
                throw new InvalidOperationException("Cannot modify submitted order");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            _items.Add(new OrderItem(product, quantity));
        }
    }
    ```

3. **Use Domain Events for Side Effects**
    ```csharp
    public void Ship()
    {
        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id, CustomerId, DateTime.UtcNow));
    }
    ```

