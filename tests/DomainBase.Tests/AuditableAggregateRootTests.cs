using DomainBase;

namespace DomainBase.Tests;

public class AuditableAggregateRootTests
{
    private class TestAuditableAgg : AuditableAggregateRoot<int>
    {
        public TestAuditableAgg(int id) : base(id) { }
        public void Touch() => MarkAsUpdated();
    }

    private class TestAuditableAggWithUser : AuditableAggregateRoot<int, Guid>
    {
        public TestAuditableAggWithUser(int id) : base(id) { }
        public void Touch(Guid userId) => MarkAsUpdated(userId);
    }

    [Fact]
    public void Constructor_SetsCreatedAtUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var agg = new TestAuditableAgg(10);
        Assert.True(agg.CreatedAt >= before);
        Assert.True(agg.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(agg.UpdatedAt);
    }

    [Fact]
    public void MarkAsUpdated_SetsUpdatedAtUtc()
    {
        var agg = new TestAuditableAgg(10);
        var beforeUpdate = DateTimeOffset.UtcNow;
        agg.Touch();
        Assert.NotNull(agg.UpdatedAt);
        Assert.True(agg.UpdatedAt!.Value >= beforeUpdate);
    }

    [Fact]
    public void MarkAsUpdated_WithUser_SetsUpdatedAtAndUpdatedBy()
    {
        var agg = new TestAuditableAggWithUser(20);
        var userId = Guid.NewGuid();
        var beforeUpdate = DateTimeOffset.UtcNow;
        agg.Touch(userId);
        Assert.NotNull(agg.UpdatedAt);
        Assert.True(agg.UpdatedAt!.Value >= beforeUpdate);
        Assert.Equal(userId, agg.UpdatedBy);
    }
}

