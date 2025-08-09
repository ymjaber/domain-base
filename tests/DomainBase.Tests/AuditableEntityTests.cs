using DomainBase;

namespace DomainBase.Tests;

public class AuditableEntityTests
{
    private class TestAuditableEntity : AuditableEntity<int>
    {
        public TestAuditableEntity(int id) : base(id) { }
    }

    private class TestAuditableEntityWithUser : AuditableEntity<int, string>
    {
        public TestAuditableEntityWithUser(int id) : base(id) { }
    }

    [Fact]
    public void Constructor_SetsCreatedAtUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity(1);
        Assert.True(entity.CreatedAt >= before);
        Assert.True(entity.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(entity.UpdatedAt);
    }

    [Fact]
    public void MarkAsUpdated_SetsUpdatedAtUtc()
    {
        var entity = new TestAuditableEntity(1);
        var beforeUpdate = DateTimeOffset.UtcNow;
        entity.MarkAsUpdated();
        Assert.NotNull(entity.UpdatedAt);
        Assert.True(entity.UpdatedAt!.Value >= beforeUpdate);
    }

    [Fact]
    public void MarkAsUpdated_WithUser_SetsUpdatedAtAndUpdatedBy()
    {
        var entity = new TestAuditableEntityWithUser(2) { CreatedBy = "creator" };
        var beforeUpdate = DateTimeOffset.UtcNow;
        entity.MarkAsUpdated("updater");
        Assert.NotNull(entity.UpdatedAt);
        Assert.True(entity.UpdatedAt!.Value >= beforeUpdate);
        Assert.Equal("updater", entity.UpdatedBy);
        Assert.Equal("creator", entity.CreatedBy);
    }
}

