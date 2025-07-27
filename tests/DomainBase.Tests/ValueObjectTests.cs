using DomainBase;

namespace DomainBase.Tests;

public class ValueObjectTests
{
    private class TestValueObject : ValueObject<TestValueObject>
    {
        public TestValueObject(string value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public string Value1 { get; }
        public int Value2 { get; }

        protected override bool EqualsCore(TestValueObject other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2;
        }

        protected override int GetHashCodeCore()
        {
            return HashCode.Combine(Value1, Value2);
        }
    }

    private class AnotherTestValueObject : ValueObject<AnotherTestValueObject>
    {
        public AnotherTestValueObject(string value)
        {
            Value = value;
        }

        public string Value { get; }

        protected override bool EqualsCore(AnotherTestValueObject other)
        {
            return Value == other.Value;
        }

        protected override int GetHashCodeCore()
        {
            return Value?.GetHashCode() ?? 0;
        }
    }

    [Fact]
    public void Equals_WhenComparingSameReference_ReturnsTrue()
    {
        // Arrange
        var vo = new TestValueObject("test", 123);

        // Act & Assert
        Assert.True(vo.Equals(vo));
    }

    [Fact]
    public void Equals_WhenComparingNull_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("test", 123);

        // Act & Assert
        Assert.False(vo.Equals(null));
    }

    [Fact]
    public void Equals_WhenComparingDifferentTypes_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new AnotherTestValueObject("test");

        // Act & Assert
        Assert.False(vo1.Equals(vo2));
    }

    [Fact]
    public void Equals_WhenValuesAreEqual_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 123);

        // Act & Assert
        Assert.True(vo1.Equals(vo2));
    }

    [Fact]
    public void Equals_WhenValuesAreDifferent_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 456);

        // Act & Assert
        Assert.False(vo1.Equals(vo2));
    }

    [Fact]
    public void EqualityOperator_WhenBothNull_ReturnsTrue()
    {
        // Arrange
        TestValueObject? vo1 = null;
        TestValueObject? vo2 = null;

        // Act & Assert
        Assert.True(vo1 == vo2);
    }

    [Fact]
    public void EqualityOperator_WhenLeftIsNull_ReturnsFalse()
    {
        // Arrange
        TestValueObject? vo1 = null;
        var vo2 = new TestValueObject("test", 123);

        // Act & Assert
        Assert.False(vo1 == vo2);
    }

    [Fact]
    public void EqualityOperator_WhenRightIsNull_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        TestValueObject? vo2 = null;

        // Act & Assert
        Assert.False(vo1 == vo2);
    }

    [Fact]
    public void EqualityOperator_WhenEqual_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 123);

        // Act & Assert
        Assert.True(vo1 == vo2);
    }

    [Fact]
    public void InequalityOperator_WhenNotEqual_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 456);

        // Act & Assert
        Assert.True(vo1 != vo2);
    }

    [Fact]
    public void InequalityOperator_WhenEqual_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 123);

        // Act & Assert
        Assert.False(vo1 != vo2);
    }

    [Fact]
    public void GetHashCode_IncludesTypeInHash()
    {
        // Arrange
        var vo = new TestValueObject("test", 123);
        var coreHash = HashCode.Combine("test", 123);
        var expectedHash = HashCode.Combine(vo.GetType(), coreHash);

        // Act
        var actualHash = vo.GetHashCode();

        // Assert
        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void GetHashCode_ForEqualValueObjects_ReturnsSameValue()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 123);

        // Act & Assert
        Assert.Equal(vo1.GetHashCode(), vo2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ForDifferentValueObjects_ReturnsDifferentValues()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 123);
        var vo2 = new TestValueObject("test", 456);

        // Act & Assert
        Assert.NotEqual(vo1.GetHashCode(), vo2.GetHashCode());
    }
}