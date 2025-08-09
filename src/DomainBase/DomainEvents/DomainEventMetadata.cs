namespace DomainBase;

/// <summary>
/// Represents metadata associated with a domain event.
/// </summary>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="UserId">The identifier of the user who triggered the event (optional).</param>
/// <param name="CorrelationId">The correlation identifier for tracking related events (optional).</param>
/// <param name="CausationId">The identifier of the event that caused this event (optional).</param>
public record DomainEventMetadata(
    Guid EventId,
    DateTimeOffset OccurredOn,
    string? UserId = null,
    Guid? CorrelationId = null,
    Guid? CausationId = null)
{
    /// <summary>
    /// Creates a new instance of <see cref="DomainEventMetadata"/> with default values.
    /// </summary>
    /// <returns>A new instance with a new EventId and current timestamp.</returns>
    public static DomainEventMetadata Create() => 
        new(Guid.NewGuid(), DateTimeOffset.UtcNow);

    /// <summary>
    /// Creates a new instance of <see cref="DomainEventMetadata"/> with a specific user.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <returns>A new instance with a new EventId, current timestamp, and specified user.</returns>
    public static DomainEventMetadata CreateWithUser(string userId) => 
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId);

    /// <summary>
    /// Creates a new instance of <see cref="DomainEventMetadata"/> with correlation tracking.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causationId">The causation identifier (optional).</param>
    /// <param name="userId">The user identifier (optional).</param>
    /// <returns>A new instance with correlation tracking.</returns>
    public static DomainEventMetadata CreateWithCorrelation(
        Guid correlationId, 
        Guid? causationId = null, 
        string? userId = null) => 
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, correlationId, causationId);
}