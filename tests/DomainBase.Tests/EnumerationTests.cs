using DomainBase;

namespace DomainBase.Tests;

public class EnumerationTests
{
    // Test enumeration without source generator (traditional approach)
    public class OrderStatus : Enumeration
    {
        public static readonly OrderStatus Submitted = new(1, "Submitted");
        public static readonly OrderStatus Approved = new(2, "Approved");
        public static readonly OrderStatus Rejected = new(3, "Rejected");

        public OrderStatus(int value, string name) : base(value, name) { }
    }

    // Enumeration leveraging source generator
    [GenerateJsonConverter]
    public partial class PaymentMethod : Enumeration
    {
        public static readonly PaymentMethod CreditCard = new(1, "Credit Card");
        public static readonly PaymentMethod DebitCard = new(2, "Debit Card");
        public static readonly PaymentMethod PayPal = new(3, "PayPal");
        public static readonly PaymentMethod BankTransfer = new(4, "Bank Transfer");

        public PaymentMethod(int value, string name) : base(value, name) { }
    }

    [Fact]
    public void Constructor_SetsValueAndName()
    {
        // Arrange & Act
        var status = OrderStatus.Submitted;

        // Assert
        Assert.Equal(1, status.Value);
        Assert.Equal("Submitted", status.Name);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        // Arrange
        var status = OrderStatus.Approved;

        // Act
        var result = status.ToString();

        // Assert
        Assert.Equal("Approved", result);
    }

    [Fact]
    public void Equals_WhenSameIdAndType_ReturnsTrue()
    {
        // Arrange
        var status1 = OrderStatus.Submitted;
        var status2 = OrderStatus.Submitted;

        // Act & Assert
        Assert.True(status1.Equals(status2));
        Assert.True(status1 == status2);
        Assert.Equal(status1.GetHashCode(), status2.GetHashCode());
    }

    [Fact]
    public void Equals_WhenDifferentId_ReturnsFalse()
    {
        // Arrange
        var status1 = OrderStatus.Submitted;
        var status2 = OrderStatus.Approved;

        // Act & Assert
        Assert.False(status1.Equals(status2));
        Assert.True(status1 != status2);
    }

    [Fact]
    public void CompareTo_OrdersByIdValue()
    {
        // Arrange
        var statuses = new[]
        {
            OrderStatus.Rejected,
            OrderStatus.Submitted,
            OrderStatus.Approved
        };

        // Act
        var sorted = statuses.OrderBy(s => s).ToArray();

        // Assert
        Assert.Equal(OrderStatus.Submitted, sorted[0]);
        Assert.Equal(OrderStatus.Approved, sorted[1]);
        Assert.Equal(OrderStatus.Rejected, sorted[2]);
    }

    // The following tests rely on generator-produced methods. They are covered
    // in `DomainBase.Generators.Tests` where the generator is exercised. Here we
    // leave them disabled to avoid coupling runtime tests to generators.
    
    // [Fact]
    // public void GetAll_ReturnsAllDefinedValues()
    // {
    //     var allMethods = PaymentMethod.GetAll();
    //     Assert.Equal(4, allMethods.Count);
    // }

    // [Fact]
    // public void FromValue_WithValidId_ReturnsCorrectInstance()
    // {
    //     var method = PaymentMethod.FromValue(2);
    //     Assert.Equal(PaymentMethod.DebitCard, method);
    // }

    // [Fact]
    // public void FromValue_WithInvalidId_ThrowsException()
    // {
    //     Assert.Throws<InvalidOperationException>(() => PaymentMethod.FromValue(99));
    // }

    // [Fact]
    // public void FromName_WithValidName_ReturnsCorrectInstance()
    // {
    //     var method = PaymentMethod.FromName("PayPal");
    //     Assert.Equal(PaymentMethod.PayPal, method);
    // }

    // [Fact]
    // public void FromName_WithInvalidName_ThrowsException()
    // {
    //     Assert.Throws<InvalidOperationException>(() => PaymentMethod.FromName("Bitcoin"));
    // }

    // [Fact]
    // public void TryFromValue_WithValidId_ReturnsTrueAndSetsResult()
    // {
    //     var success = PaymentMethod.TryFromValue(3, out var method);
    //     Assert.True(success);
    //     Assert.Equal(PaymentMethod.PayPal, method);
    // }

    // [Fact]
    // public void TryFromValue_WithInvalidId_ReturnsFalseAndSetsNull()
    // {
    //     var success = PaymentMethod.TryFromValue(99, out var method);
    //     Assert.False(success);
    //     Assert.Null(method);
    // }

    // [Fact]
    // public void TryFromName_WithValidName_ReturnsTrueAndSetsResult()
    // {
    //     var success = PaymentMethod.TryFromName("Bank Transfer", out var method);
    //     Assert.True(success);
    //     Assert.Equal(PaymentMethod.BankTransfer, method);
    // }

    // [Fact]
    // public void TryFromName_WithInvalidName_ReturnsFalseAndSetsNull()
    // {
    //     var success = PaymentMethod.TryFromName("Cryptocurrency", out var method);
    //     Assert.False(success);
    //     Assert.Null(method);
    // }

    [Fact]
    public void ComparisonOperators_WorkCorrectly()
    {
        // Arrange
        var method1 = PaymentMethod.CreditCard; // Id = 1
        var method2 = PaymentMethod.PayPal; // Id = 3

        // Act & Assert
        Assert.True(method1 < method2);
        Assert.True(method1 <= method2);
        Assert.False(method1 > method2);
        Assert.False(method1 >= method2);
        Assert.True(method2 > method1);
        Assert.True(method2 >= method1);
    }
}