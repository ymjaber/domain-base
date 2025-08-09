### Entities

Inherit `Entity<TId>` to get identity-based equality. Equality is by type and non-default `Id` only.

```csharp
public sealed class Customer : Entity<Guid>
{
    public string Name { get; private set; }
    public Customer(Guid id, string name) : base(id) => Name = name;
}
```

Notes:
- Default/empty IDs are treated as transient; transient entities are never equal.
- `AggregateRoot<TId>` extends `Entity<TId>` and tracks domain events.
 - Auditing variants are available: `IAuditableEntity`, `IAuditableEntity<TUserId>`, and base classes `AuditableEntity<TId>`, `AuditableEntity<TId, TUserId>`, and their `AggregateRoot` counterparts.

