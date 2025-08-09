namespace DomainBase;

/// <summary>
/// Optional abstract repository that wires <see cref="IDomainEventDispatcher"/> dispatch after save.
/// This is a minimal example; actual persistence is left to derived classes.
/// </summary>
public abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : struct
{
    /// <summary>
    /// The dispatcher used to publish domain events after persistence operations.
    /// </summary>
    protected readonly IDomainEventDispatcher DomainEventDispatcher;

    /// <summary>
    /// Initializes a new instance of the repository.
    /// </summary>
    /// <param name="domainEventDispatcher">The dispatcher used to publish domain events.</param>
    protected Repository(IDomainEventDispatcher domainEventDispatcher)
    {
        DomainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>Gets an entity by its identifier.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public abstract Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    /// <summary>Gets all entities.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of all entities.</returns>
    public abstract Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Finds entities that match the specified specification.</summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A read-only collection of entities that match the specification.</returns>
    public abstract Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    /// <summary>Finds a single entity that matches the specified specification.</summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public abstract Task<TEntity?> FindSingleAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    /// <summary>Counts entities that match the specified specification.</summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    public abstract Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    /// <summary>Checks if any entity matches the specified specification.</summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if any entity matches; otherwise, <c>false</c>.</returns>
    public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity and dispatches its domain events.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await AddCoreAsync(entity, cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a range of entities and dispatches their domain events.</summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await AddRangeCoreAsync(entities, cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates an entity and dispatches its domain events.</summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await UpdateCoreAsync(entity, cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes an entity and dispatches its domain events.</summary>
    /// <param name="entity">The entity to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await RemoveCoreAsync(entity, cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes a range of entities and dispatches their domain events.</summary>
    /// <param name="entities">The entities to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await RemoveRangeCoreAsync(entities, cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Persistence hook for add.</summary>
    protected abstract Task AddCoreAsync(TEntity entity, CancellationToken cancellationToken);
    /// <summary>Persistence hook for add range.</summary>
    protected abstract Task AddRangeCoreAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    /// <summary>Persistence hook for update.</summary>
    protected abstract Task UpdateCoreAsync(TEntity entity, CancellationToken cancellationToken);
    /// <summary>Persistence hook for remove.</summary>
    protected abstract Task RemoveCoreAsync(TEntity entity, CancellationToken cancellationToken);
    /// <summary>Persistence hook for remove range.</summary>
    protected abstract Task RemoveRangeCoreAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches and clears domain events for a single entity if it is an aggregate root.
    /// </summary>
    /// <param name="entity">The entity to process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when dispatching has finished.</returns>
    protected virtual async Task DispatchDomainEventsAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is AggregateRoot<TId> aggregate && aggregate.DomainEvents.Count > 0)
        {
            await DomainEventDispatcher.DispatchAsync(aggregate.DomainEvents, cancellationToken).ConfigureAwait(false);
            aggregate.ClearDomainEvents();
        }
    }

    /// <summary>
    /// Dispatches and clears domain events for a collection of entities if they are aggregate roots.
    /// </summary>
    /// <param name="entities">The entities to process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when dispatching has finished.</returns>
    protected virtual async Task DispatchDomainEventsAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        foreach (var entity in entities)
        {
            await DispatchDomainEventsAsync(entity, cancellationToken).ConfigureAwait(false);
        }
    }
}

