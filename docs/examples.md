# Real-world examples

## Example 1: Customer domain

```csharp
// Value objects
[ValueObject]
public sealed partial class FullName : ValueObject<FullName>
{
    [IncludeInEquality(Priority = 10)] public string First { get; init; }
    [IncludeInEquality(Priority = 10)] public string Last  { get; init; }
}

// Enumeration
public sealed partial class CustomerStatus : Enumeration
{
    public static readonly CustomerStatus Active   = new(1, "Active");
    public static readonly CustomerStatus Inactive = new(2, "Inactive");
    public CustomerStatus(int value, string name) : base(value, name) { }
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
        if (Status == CustomerStatus.Inactive) return;
        Status = CustomerStatus.Inactive;
        AddDomainEvent(new CustomerDeactivated(Id, reason));
    }
}

public sealed record CustomerDeactivated(Guid CustomerId, string Reason) : DomainEvent;
```

## Example 2: Catalog with specifications

```csharp
public sealed class Product : AggregateRoot<Guid>
{
    public Product(Guid id, string name, decimal price) : base(id)
    { Name = name; Price = price; }

    public string Name { get; private set; }
    public decimal Price { get; private set; }
}

public sealed class ProductsByPrice : Specification<Product>
{
    public ProductsByPrice(decimal min, decimal max)
        : base(p => p.Price >= min && p.Price <= max) { }
}

public sealed class ProductRepository : Repository<Product, Guid>
{
    private readonly DbContext _db; public ProductRepository(DbContext db, IDomainEventDispatcher d) : base(d) => _db = db;
    public override Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Set<Product>().FindAsync([id], ct).AsTask();
    public override Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) => _db.Set<Product>().ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Product>)t.Result, ct);
    public override Task<IReadOnlyList<Product>> FindAsync(ISpecification<Product> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Product>(), s).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Product>)t.Result, ct);
    public override Task<Product?> FindSingleAsync(ISpecification<Product> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Product>(), s).FirstOrDefaultAsync(ct);
    public override Task<int> CountAsync(ISpecification<Product> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Product>(), s).CountAsync(ct);
    public override Task<bool> AnyAsync(ISpecification<Product> s, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(_db.Set<Product>(), s).AnyAsync(ct);
    protected override Task AddCoreAsync(Product e, CancellationToken ct) => _db.AddAsync(e, ct).AsTask();
    protected override Task AddRangeCoreAsync(IEnumerable<Product> es, CancellationToken ct) { _db.AddRange(es); return Task.CompletedTask; }
    protected override Task UpdateCoreAsync(Product e, CancellationToken ct) { _db.Update(e); return Task.CompletedTask; }
    protected override Task RemoveCoreAsync(Product e, CancellationToken ct) { _db.Remove(e); return Task.CompletedTask; }
    protected override Task RemoveRangeCoreAsync(IEnumerable<Product> es, CancellationToken ct) { _db.RemoveRange(es); return Task.CompletedTask; }
}
```

## Example 3: JSON and EF Core converters

See more: [guide.md](guide.md), [reference.md](reference.md).

```csharp
[GenerateVoJsonConverter]
[GenerateEfValueConverter]
[GenerateTypeConverter]
public sealed partial class Sku : ValueObject<Sku, string>
{ public Sku(string value) : base(value) { } }

[GenerateJsonConverter]
[GenerateEfValueConverter]
public sealed partial class PaymentMethod : Enumeration
{
    public static readonly PaymentMethod CreditCard = new(1, "Credit Card");
    public static readonly PaymentMethod Cash       = new(2, "Cash");
    public PaymentMethod(int value, string name) : base(value, name) { }
}
```

