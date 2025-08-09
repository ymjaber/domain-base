namespace DomainBase;

/// <summary>
/// Defines an entity that tracks creation and modification timestamps.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Defines an entity that tracks creation and modification timestamps with user information.
/// </summary>
public interface IAuditableEntity<TUserId> : IAuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    TUserId? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    TUserId? UpdatedBy { get; set; }
}

/// <summary>
/// Base class for auditable entities that tracks creation and modification timestamps.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableEntity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected AuditableEntity(TId id) : base(id)
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Updates the UpdatedAt timestamp to the current UTC time.
    /// </summary>
    public virtual void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Base class for auditable entities that tracks creation and modification timestamps with user information.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
/// <typeparam name="TUserId">The type of the user identifier.</typeparam>
public abstract class AuditableEntity<TId, TUserId> : Entity<TId>, IAuditableEntity<TUserId>
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableEntity{TId, TUserId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected AuditableEntity(TId id) : base(id)
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public TUserId? CreatedBy { get; set; }

    /// <inheritdoc/>
    public TUserId? UpdatedBy { get; set; }

    /// <summary>
    /// Updates the UpdatedAt timestamp to the current UTC time.
    /// </summary>
    public virtual void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the UpdatedAt timestamp and UpdatedBy user.
    /// </summary>
    /// <param name="userId">The identifier of the user performing the update.</param>
    public virtual void MarkAsUpdated(TUserId userId)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }
}

/// <summary>
/// Base class for auditable aggregate roots that tracks creation and modification timestamps.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableAggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the aggregate root.</param>
    protected AuditableAggregateRoot(TId id) : base(id)
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Updates the UpdatedAt timestamp to the current UTC time.
    /// </summary>
    public virtual void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Base class for auditable aggregate roots that tracks creation and modification timestamps with user information.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
/// <typeparam name="TUserId">The type of the user identifier.</typeparam>
public abstract class AuditableAggregateRoot<TId, TUserId> : AggregateRoot<TId>, IAuditableEntity<TUserId>
    where TId : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableAggregateRoot{TId, TUserId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the aggregate root.</param>
    protected AuditableAggregateRoot(TId id) : base(id)
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public TUserId? CreatedBy { get; set; }

    /// <inheritdoc/>
    public TUserId? UpdatedBy { get; set; }

    /// <summary>
    /// Updates the UpdatedAt timestamp to the current UTC time.
    /// </summary>
    public virtual void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the UpdatedAt timestamp and UpdatedBy user.
    /// </summary>
    /// <param name="userId">The identifier of the user performing the update.</param>
    public virtual void MarkAsUpdated(TUserId userId)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }
}