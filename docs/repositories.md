### Repositories

`IRepository<TEntity, TId>` defines a minimal abstraction with `GetByIdAsync`, `GetAllAsync`, `FindAsync`, `FindSingleAsync`, `CountAsync`, `AnyAsync`, `AddAsync`, `AddRangeAsync`, `UpdateAsync`, `RemoveAsync`, `RemoveRangeAsync`.

`Repository<TEntity, TId>` is an optional base class that wires `IDomainEventDispatcher` dispatch after persistence. Implement the persistence hooks in a derived class.

Example outline:

```csharp
public sealed class EfRepository<TEntity, TId> : Repository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : struct
{
    private readonly DbContext _db;

    public EfRepository(DbContext db, IDomainEventDispatcher dispatcher) : base(dispatcher) => _db = db;

    public override Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => _db.Set<TEntity>().FindAsync(new object[] { id }, ct).AsTask();

    public override Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.Set<TEntity>().ToListAsync(ct).ContinueWith(t => (IReadOnlyList<TEntity>)t.Result, ct);

    public override Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => SpecificationEvaluator.GetQuery(_db.Set<TEntity>().AsQueryable(), spec).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<TEntity>)t.Result, ct);

    // Implement other abstract members...

    protected override Task AddCoreAsync(TEntity entity, CancellationToken ct) { _db.Add(entity); return _db.SaveChangesAsync(ct); }
    protected override Task AddRangeCoreAsync(IEnumerable<TEntity> entities, CancellationToken ct) { _db.AddRange(entities); return _db.SaveChangesAsync(ct); }
    protected override Task UpdateCoreAsync(TEntity entity, CancellationToken ct) { _db.Update(entity); return _db.SaveChangesAsync(ct); }
    protected override Task RemoveCoreAsync(TEntity entity, CancellationToken ct) { _db.Remove(entity); return _db.SaveChangesAsync(ct); }
    protected override Task RemoveRangeCoreAsync(IEnumerable<TEntity> entities, CancellationToken ct) { _db.RemoveRange(entities); return _db.SaveChangesAsync(ct); }
}
```

