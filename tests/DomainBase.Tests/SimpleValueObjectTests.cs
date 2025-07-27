using DomainBase;

namespace DomainBase.Tests;

public class SimpleValueObjectTests
{
    private class StringValueObject : ValueObject<StringValueObject, string>
    {
        public StringValueObject(string value) : base(value) { }
    }

    private class IntValueObject : ValueObject<IntValueObject, int>
    {
        public IntValueObject(int value) : base(value) { }
    }

    [Fact]
    public void Constructor_SetsValue()
    {
        // Arrange & Act
        var vo = new StringValueObject("test");

        // Assert
        Assert.Equal("test", vo.Value);
    }

    [Fact]
    public void EqualsCore_WhenValuesAreEqual_ReturnsTrue()
    {
        // Arrange
        var vo1 = new StringValueObject("test");
        var vo2 = new StringValueObject("test");

        // Act & Assert
        Assert.True(vo1.Equals(vo2));
    }

    [Fact]
    public void EqualsCore_WhenValuesAreDifferent_ReturnsFalse()
    {
        // Arrange
        var vo1 = new StringValueObject("test1");
        var vo2 = new StringValueObject("test2");

        // Act & Assert
        Assert.False(vo1.Equals(vo2));
    }

    [Fact]
    public void GetHashCodeCore_ReturnsValueHashCode()
    {
        // Arrange
        var value = "test";
        var vo = new StringValueObject(value);

        // Act
        var hash = vo.GetHashCode();
        var expectedHash = HashCode.Combine(vo.GetType(), value.GetHashCode());

        // Assert
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public void WorksWithValueTypes()
    {
        // Arrange
        var vo1 = new IntValueObject(123);
        var vo2 = new IntValueObject(123);
        var vo3 = new IntValueObject(456);

        // Act & Assert
        Assert.Equal(123, vo1.Value);
        Assert.True(vo1 == vo2);
        Assert.False(vo1 == vo3);
        Assert.Equal(vo1.GetHashCode(), vo2.GetHashCode());
        Assert.NotEqual(vo1.GetHashCode(), vo3.GetHashCode());
    }

    [Fact]
    public void InheritsValueObjectBehavior()
    {
        // Arrange
        var vo1 = new StringValueObject("test");
        var vo2 = new StringValueObject("test");

        // Act & Assert
        Assert.True(vo1 == vo2);
        Assert.False(vo1 != vo2);
        Assert.Equal(vo1.GetHashCode(), vo2.GetHashCode());
    }

    [Fact]
    public void HandlesNullOperatorComparisons()
    {
        // Arrange
        StringValueObject? vo1 = null;
        var vo2 = new StringValueObject("test");

        // Act & Assert
        Assert.False(vo1 == vo2);
        Assert.False(vo2 == vo1);
        Assert.True(vo1 != vo2);
        Assert.True(vo2 != vo1);
    }
    
    [Fact]
    public void ToString_ReturnsValueToString()
    {
        // Arrange
        var stringVo = new StringValueObject("test value");
        var intVo = new IntValueObject(123);

        // Act
        var stringResult = stringVo.ToString();
        var intResult = intVo.ToString();

        // Assert
        Assert.Equal("test value", stringResult);
        Assert.Equal("123", intResult);
    }
    
    [Fact]
    public void ImplicitConversion_ConvertsToUnderlyingValue()
    {
        // Arrange
        var stringVo = new StringValueObject("test value");
        var intVo = new IntValueObject(456);

        // Act
        string stringValue = stringVo;
        int intValue = intVo;

        // Assert
        Assert.Equal("test value", stringValue);
        Assert.Equal(456, intValue);
    }
    
    [Fact]
    public void ImplicitConversion_WorksInMethodCalls()
    {
        // Arrange
        var vo = new StringValueObject("hello");
        
        // Act
        // The implicit conversion should allow passing the value object where string is expected
        var length = GetStringLength(vo);
        var upper = GetUpperCase(vo);

        // Assert
        Assert.Equal(5, length);
        Assert.Equal("HELLO", upper);
    }
    
    private int GetStringLength(string value) => value.Length;
    private string GetUpperCase(string value) => value.ToUpper();
}