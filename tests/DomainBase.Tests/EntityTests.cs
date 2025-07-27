using DomainBase;

namespace DomainBase.Tests;

public class EntityTests
{
    private class TestEntity : Entity<int>
    {
        public TestEntity(int id) : base(id) { }
    }

    private class AnotherTestEntity : Entity<int>
    {
        public AnotherTestEntity(int id) : base(id) { }
    }

    [Fact]
    public void Constructor_SetsId()
    {
        // Arrange & Act
        var entity = new TestEntity(123);

        // Assert
        Assert.Equal(123, entity.Id);
    }


    [Fact]
    public void Equals_WhenComparingSameReference_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity(123);

        // Act & Assert
        Assert.True(entity.Equals(entity));
    }

    [Fact]
    public void Equals_WhenComparingNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(123);

        // Act & Assert
        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void Equals_WhenComparingDifferentTypes_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new AnotherTestEntity(123);

        // Act & Assert
        Assert.False(entity1.Equals(entity2));
    }

    [Fact]
    public void Equals_WhenBothHaveDefaultId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(default);
        var entity2 = new TestEntity(default);

        // Act & Assert
        Assert.False(entity1.Equals(entity2));
    }

    [Fact]
    public void Equals_WhenOneHasDefaultId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(default);

        // Act & Assert
        Assert.False(entity1.Equals(entity2));
    }

    [Fact]
    public void Equals_WhenSameTypeAndId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(123);

        // Act & Assert
        Assert.True(entity1.Equals(entity2));
    }

    [Fact]
    public void Equals_WhenSameTypeButDifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(456);

        // Act & Assert
        Assert.False(entity1.Equals(entity2));
    }

    [Fact]
    public void EqualityOperator_WhenBothNull_ReturnsTrue()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act & Assert
        Assert.True(entity1 == entity2);
    }

    [Fact]
    public void EqualityOperator_WhenLeftIsNull_ReturnsFalse()
    {
        // Arrange
        TestEntity? entity1 = null;
        var entity2 = new TestEntity(123);

        // Act & Assert
        Assert.False(entity1 == entity2);
    }

    [Fact]
    public void EqualityOperator_WhenRightIsNull_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        TestEntity? entity2 = null;

        // Act & Assert
        Assert.False(entity1 == entity2);
    }

    [Fact]
    public void EqualityOperator_WhenEqual_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(123);

        // Act & Assert
        Assert.True(entity1 == entity2);
    }

    [Fact]
    public void InequalityOperator_WhenNotEqual_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(456);

        // Act & Assert
        Assert.True(entity1 != entity2);
    }

    [Fact]
    public void InequalityOperator_WhenEqual_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(123);

        // Act & Assert
        Assert.False(entity1 != entity2);
    }

    [Fact]
    public void GetHashCode_ReturnsCombinationOfTypeAndId()
    {
        // Arrange
        var entity = new TestEntity(123);
        var expectedHash = HashCode.Combine(entity.GetType(), 123);

        // Act
        var actualHash = entity.GetHashCode();

        // Assert
        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void GetHashCode_ForEqualEntities_ReturnsSameValue()
    {
        // Arrange
        var entity1 = new TestEntity(123);
        var entity2 = new TestEntity(123);

        // Act & Assert
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsTypeNameAndId()
    {
        // Arrange
        var entity = new TestEntity(123);

        // Act
        var result = entity.ToString();

        // Assert
        Assert.Equal("TestEntity: 123", result);
    }
}