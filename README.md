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
- **💎 Value Objects** - Immutable objects with value-based equality
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

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

// Value objects are compared by value, not reference
var email1 = new Email("john@example.com");
var email2 = new Email("JOHN@EXAMPLE.COM");
Console.WriteLine(email1 == email2); // True (case-insensitive)
```

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
[Enumeration]
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

// Source generator provides static methods when using [Enumeration] attribute
var allStatuses = OrderStatus.GetAll();

// Parse from value or name
var shipped = OrderStatus.FromValue(2);
var pending = OrderStatus.FromName("Pending");
```

### Specifications - Encapsulated Queries

```csharp
using DomainBase.Specifications;

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

var activeOrRecentSpec = new ActiveCustomerSpecification()
    .Or(new Specification<Customer>(c => c.LastActivityDate > DateTime.UtcNow.AddDays(-7)));

// Use with repositories
var customers = await repository.FindAsync(activePremiumSpec);

// Check individual entities
if (activePremiumSpec.IsSatisfiedBy(customer))
{
    // Apply premium benefits
}
```

## Common Patterns

### 1. Rich Domain Entity

```csharp
public class Account : AggregateRoot<Guid>
{
    public AccountNumber Number { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }

    public Result Withdraw(Money amount)
    {
        if (Status != AccountStatus.Active)
            return Result.Failure("Account is not active");

        if (Balance < amount)
            return Result.Failure("Insufficient funds");

        Balance = Balance.Subtract(amount);
        AddDomainEvent(new MoneyWithdrawnEvent(Id, amount));

        return Result.Success();
    }
}
```

### 2. Complex Value Object

```csharp
public class Address : ValueObject<Address>
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(string street, string city, string postalCode, string country)
    {
        Street = Guard.Against.NullOrWhiteSpace(street);
        City = Guard.Against.NullOrWhiteSpace(city);
        PostalCode = ValidatePostalCode(postalCode, country);
        Country = Guard.Against.NullOrWhiteSpace(country);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }
}
```

### 3. Business Rule Specification

```csharp
public class EligibleForPromotionSpec : Specification<Customer>
{
    public EligibleForPromotionSpec()
        : base(c => c.Status == CustomerStatus.Active &&
                   c.TotalPurchases >= 1000 &&
                   c.RegisteredDate <= DateTime.UtcNow.AddMonths(-6))
    {
        AddInclude(c => c.Orders);
    }
}

// Use in service or repository
var eligibleCustomers = await repository.ListAsync(new EligibleForPromotionSpec());
```

### 4. Domain Service Pattern

```csharp
public interface IExchangeRateService : IDomainService
{
    Money Convert(Money amount, Currency targetCurrency);
}

public class MoneyTransferService : IDomainService
{
    private readonly IExchangeRateService _exchangeRates;

    public Result TransferFunds(Account from, Account to, Money amount)
    {
        // Complex business logic involving multiple aggregates
        var withdrawResult = from.Withdraw(amount);
        if (withdrawResult.IsFailure)
            return withdrawResult;

        var convertedAmount = from.Currency != to.Currency
            ? _exchangeRates.Convert(amount, to.Currency)
            : amount;

        to.Deposit(convertedAmount);

        return Result.Success();
    }
}
```

## Documentation

For comprehensive documentation, visit the [docs](./docs) folder:

- [Getting Started Guide](./docs/getting-started/quick-start.md)
- [Core Concepts](./docs/core-concepts/)
- [API Reference](./docs/api-reference/)
- [Examples](./docs/examples/)
- [Best Practices](./docs/guides/ddd-best-practices.md)
- [Migration Guide](./docs/guides/migration-guide.md)

## Real-World Example

```csharp
public class ECommerceExample
{
    // Rich domain entity with business logic
    public class Product : AggregateRoot<Guid>
    {
        public ProductName Name { get; private set; }
        public Money Price { get; private set; }
        public StockQuantity Stock { get; private set; }
        public ProductStatus Status { get; private set; }

        public Product(Guid id, ProductName name, Money price) : base(id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Price = price ?? throw new ArgumentNullException(nameof(price));
            Stock = StockQuantity.Zero;
            Status = ProductStatus.Draft;
        }

        public void Publish()
        {
            if (Status != ProductStatus.Draft)
                throw new InvalidOperationException("Only draft products can be published");

            Status = ProductStatus.Active;
            AddDomainEvent(new ProductPublishedEvent(Id, Name.Value));
        }

        public void AddStock(int quantity)
        {
            Stock = Stock.Add(quantity);
            AddDomainEvent(new StockAddedEvent(Id, quantity));
        }

        public Result<StockReservation> ReserveStock(int quantity)
        {
            if (Status != ProductStatus.Active)
                return Result.Failure<StockReservation>("Product is not active");

            if (!Stock.CanReserve(quantity))
                return Result.Failure<StockReservation>("Insufficient stock");

            var reservation = new StockReservation(Guid.NewGuid(), Id, quantity);
            Stock = Stock.Reserve(quantity);

            AddDomainEvent(new StockReservedEvent(Id, reservation.Id, quantity));
            return Result.Success(reservation);
        }
    }

    // Value objects enforce business rules
    public class Money : ValueObject<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative");
            Amount = amount;
            Currency = Guard.Against.NullOrWhiteSpace(currency);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Cannot add different currencies");
            return new Money(Amount + other.Amount, Currency);
        }
    }

    // Specifications for complex queries
    public class AvailableProductsSpec : Specification<Product>
    {
        public AvailableProductsSpec()
            : base(p => p.Status == ProductStatus.Active && p.Stock.Available > 0)
        {
            AddInclude(p => p.Reviews);
            ApplyOrderBy(p => p.Name.Value);
        }
    }
}
```

## Performance

DomainBase is designed for high performance:

- Zero-allocation equality checks for common scenarios
- Compiled expressions for specification queries
- Minimal boxing with generic constraints
- Immutable types prevent defensive copying
- Source generators for enumeration lookups

## Contributing

We welcome contributions! Please see our [Contributing Guide](./docs/contributing.md) for details.

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

