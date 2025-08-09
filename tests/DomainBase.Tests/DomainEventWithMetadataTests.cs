using DomainBase;

namespace DomainBase.Tests;

public class DomainEventWithMetadataTests
{
    private record TestEventWithMetadata(string Data) : DomainEventWithMetadata
    {
        public override DomainEventWithMetadata WithMetadata(DomainEventMetadata metadata) =>
            this with { Metadata = metadata, Id = metadata.EventId, OccurredOn = metadata.OccurredOn };
    }

    [Fact]
    public void DefaultConstructor_SetsMetadataAndBaseProperties()
    {
        var before = DateTimeOffset.UtcNow;
        var ev = new TestEventWithMetadata("x");
        Assert.NotEqual(Guid.Empty, ev.Id);
        Assert.True(ev.OccurredOn >= before);
        Assert.True(ev.OccurredOn <= DateTimeOffset.UtcNow);
        Assert.Equal(ev.Id, ev.Metadata.EventId);
        Assert.Equal(ev.OccurredOn, ev.Metadata.OccurredOn);
        Assert.Null(ev.UserId);
        Assert.Null(ev.CorrelationId);
        Assert.Null(ev.CausationId);
    }

    [Fact]
    public void WithMetadata_ReplacesMetadataAndUpdatesBaseProperties()
    {
        var ev = new TestEventWithMetadata("payload");
        var meta = DomainEventMetadata.CreateWithUser("u1");
        var updated = (TestEventWithMetadata)ev.WithMetadata(meta);

        Assert.Equal(meta, updated.Metadata);
        Assert.Equal(meta.EventId, updated.Id);
        Assert.Equal(meta.OccurredOn, updated.OccurredOn);
        Assert.Equal("u1", updated.UserId);
        Assert.Equal("payload", updated.Data);
    }

    [Fact]
    public void CreateWithCorrelation_SetsCorrelationAndOptionalCausation()
    {
        var ev = new TestEventWithMetadata("p");
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();
        var meta = DomainEventMetadata.CreateWithCorrelation(corr, caus, "u");
        var updated = (TestEventWithMetadata)ev.WithMetadata(meta);

        Assert.Equal(corr, updated.CorrelationId);
        Assert.Equal(caus, updated.CausationId);
        Assert.Equal("u", updated.UserId);

        var meta2 = DomainEventMetadata.CreateWithCorrelation(corr);
        var updated2 = (TestEventWithMetadata)ev.WithMetadata(meta2);
        Assert.Equal(corr, updated2.CorrelationId);
        Assert.Null(updated2.CausationId);
    }
}

