### Examples

#### E‑commerce Domain

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

        public bool TryReserveStock(int quantity, out StockReservation reservation)
        {
            reservation = default!;
            if (Status != ProductStatus.Active)
                return false;

            if (!Stock.CanReserve(quantity))
                return false;

            reservation = new StockReservation(Guid.NewGuid(), Id, quantity);
            Stock = Stock.Reserve(quantity);
            AddDomainEvent(new StockReservedEvent(Id, reservation.Id, quantity));
            return true;
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
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        protected override bool EqualsCore(Money other) => Amount == other.Amount && Currency == other.Currency;
        protected override int GetHashCodeCore() => HashCode.Combine(Amount, Currency);

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

#### Banking Transfers

```csharp
public sealed class Account : AggregateRoot<Guid>
{
    public Money Balance { get; private set; }
    public Account(Guid id, Money openingBalance) : base(id) => Balance = openingBalance;

    public bool TryWithdraw(Money amount)
    {
        if (Balance.Amount < amount.Amount || Balance.Currency != amount.Currency)
            return false;
        Balance = new Money(Balance.Amount - amount.Amount, Balance.Currency);
        return true;
    }

    public void Deposit(Money amount)
    {
        if (Balance.Currency != amount.Currency)
            throw new InvalidOperationException("Currency mismatch");
        Balance = new Money(Balance.Amount + amount.Amount, Balance.Currency);
    }
}

public sealed class TransferService : IDomainService
{
    public bool Transfer(Account from, Account to, Money amount)
    {
        if (!from.TryWithdraw(amount)) return false;
        to.Deposit(amount);
        return true;
    }
}
```

#### Bookings

- Reservation aggregate emitting domain events
- Date range value object with validation

