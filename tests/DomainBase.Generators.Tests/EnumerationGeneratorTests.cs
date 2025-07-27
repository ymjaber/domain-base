using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using VerifyXunit;

namespace DomainBase.Generators.Tests;

public class EnumerationGeneratorTests
{
    [Fact]
    public Task GeneratesExpectedCode_ForBasicEnumeration()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [Enumeration]
            public partial class OrderStatus : Enumeration
            {
                public static readonly OrderStatus Submitted = new(1, "Submitted");
                public static readonly OrderStatus Approved = new(2, "Approved");
                public static readonly OrderStatus Rejected = new(3, "Rejected");

                public OrderStatus(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesExpectedCode_WithJsonConverter()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [Enumeration(GenerateJsonConverter = true)]
            public partial class PaymentMethod : Enumeration
            {
                public static readonly PaymentMethod CreditCard = new(1, "Credit Card");
                public static readonly PaymentMethod DebitCard = new(2, "Debit Card");

                public PaymentMethod(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesExpectedCode_InGlobalNamespace()
    {
        var source = """
            using DomainBase;

            [Enumeration]
            public partial class GlobalStatus : Enumeration
            {
                public static readonly GlobalStatus Active = new(1, "Active");
                public static readonly GlobalStatus Inactive = new(2, "Inactive");

                public GlobalStatus(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesExpectedCode_WithNestedNamespaces()
    {
        var source = """
            using DomainBase;

            namespace Company.Product.Domain.Enumerations;

            [Enumeration]
            public partial class Priority : Enumeration
            {
                public static readonly Priority Low = new(1, "Low");
                public static readonly Priority Medium = new(2, "Medium");
                public static readonly Priority High = new(3, "High");

                public Priority(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DoesNotGenerateCode_ForNonPartialClass()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [Enumeration]
            public class InvalidStatus : Enumeration
            {
                public static readonly InvalidStatus Active = new(1, "Active");

                public InvalidStatus(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DoesNotGenerateCode_ForAbstractClass()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [Enumeration]
            public abstract partial class AbstractStatus : Enumeration
            {
                public static readonly AbstractStatus Active = new(1, "Active");

                protected AbstractStatus(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task HandlesComplexScenario_WithMultipleEnumerations()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [Enumeration]
            public partial class Status1 : Enumeration
            {
                public static readonly Status1 Active = new(1, "Active");
                public Status1(int id, string name) : base(id, name) { }
            }

            [Enumeration(GenerateJsonConverter = true)]
            public partial class Status2 : Enumeration
            {
                public static readonly Status2 Pending = new(1, "Pending");
                public Status2(int id, string name) : base(id, name) { }
            }

            // Should not generate - not partial
            [Enumeration]
            public class Status3 : Enumeration
            {
                public static readonly Status3 Complete = new(1, "Complete");
                public Status3(int id, string name) : base(id, name) { }
            }
            """;

        return TestHelper.Verify(source);
    }
}