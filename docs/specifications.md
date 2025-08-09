### Specifications

Encapsulate query logic and combine with AND/OR/NOT.

Core API:
- `Specification<T>` base class with `Criteria`, `Includes`, `IncludeStrings`, `ApplyOrderBy`, `ApplyOrderByDescending`, `ApplyPaging`
- Combos: `.And(...)`, `.Or(...)`, `.Not()`
- Evaluation: `SpecificationEvaluator.GetQuery(IQueryable<T>, ISpecification<T>)`
- In-memory check: `IsSatisfiedBy(entity)`

Example:

```csharp
public sealed class ActiveCustomerSpec : Specification<Customer>
{
    public ActiveCustomerSpec() : base(c => c.IsActive) { }
}

public sealed class RecentActivitySpec : Specification<Customer>
{
    public RecentActivitySpec() : base(c => c.LastActivityDate > DateTime.UtcNow.AddDays(-7)) { }
}

var spec = new ActiveCustomerSpec().And(new RecentActivitySpec());
var query = SpecificationEvaluator.GetQuery(db.Customers, spec);
```

