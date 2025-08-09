namespace DomainBase;

/// <summary>
/// Base record for domain events with metadata support.
/// Provides enhanced tracking capabilities for domain events.
/// </summary>
public abstract record DomainEventWithMetadata : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the domain event with metadata.
    /// </summary>
    /// <param name="metadata">The metadata associated with the event.</param>
    protected DomainEventWithMetadata(DomainEventMetadata metadata) 
        : base(metadata.EventId, metadata.OccurredOn)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Initializes a new instance of the domain event with default metadata.
    /// </summary>
    protected DomainEventWithMetadata() 
        : this(DomainEventMetadata.Create())
    {
    }

    /// <summary>
    /// Gets the metadata associated with this event.
    /// </summary>
    public DomainEventMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the user identifier from the metadata.
    /// </summary>
    public string? UserId => Metadata.UserId;

    /// <summary>
    /// Gets the correlation identifier from the metadata.
    /// </summary>
    public Guid? CorrelationId => Metadata.CorrelationId;

    /// <summary>
    /// Gets the causation identifier from the metadata.
    /// </summary>
    public Guid? CausationId => Metadata.CausationId;

    /// <summary>
    /// Creates a new event with updated metadata while preserving the event data.
    /// </summary>
    /// <param name="metadata">The new metadata to apply.</param>
    /// <returns>A new instance of the event with updated metadata.</returns>
    public abstract DomainEventWithMetadata WithMetadata(DomainEventMetadata metadata);
}