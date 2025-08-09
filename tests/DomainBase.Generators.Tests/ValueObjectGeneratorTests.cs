using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using VerifyXunit;

namespace DomainBase.Generators.Tests;

public class ValueObjectGeneratorTests
{
    [Fact]
    public Task GeneratesExpectedCode_ForSimpleValueObject()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Address : ValueObject<Address>
            {
                [IncludeInEquality]
                public string Street { get; init; }
                
                [IncludeInEquality]
                public string City { get; init; }
                
                [IgnoreEquality]
                public DateTime CreatedAt { get; init; }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesExpectedCode_WithPriorityOrdering()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Product : ValueObject<Product>
            {
                [IncludeInEquality(Priority = 10)]
                public string Id { get; init; }
                
                [IncludeInEquality(Priority = 5)]
                public string Name { get; init; }
                
                [IncludeInEquality(Priority = 1)]
                public decimal Price { get; init; }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesExpectedCode_WithCustomEquality()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Person : ValueObject<Person>
            {
                [IncludeInEquality]
                public string FirstName { get; init; }
                
                [CustomEquality]
                public string LastName { get; init; }
                
                private partial void IsEqualLastName(string? lastName, string? otherLastName, ref bool result)
                {
                    result = string.Equals(lastName, otherLastName, StringComparison.OrdinalIgnoreCase);
                }
                
                private partial void GetHashCodeLastName(string? lastName, ref System.HashCode hashCode)
                {
                    hashCode.Add(lastName?.ToUpperInvariant());
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesExpectedCode_WithSequenceEquality()
    {
        var source = """
            using DomainBase;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ValueObject]
            public partial class Order : ValueObject<Order>
            {
                [IncludeInEquality]
                public string OrderId { get; init; }
                
                [SequenceEquality(OrderMatters = true)]
                public List<string> Items { get; init; }
                
                [SequenceEquality(OrderMatters = false)]
                public IReadOnlyList<int> Tags { get; init; }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForNonPartialClass()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public class Address : ValueObject<Address>
            {
                [IncludeInEquality]
                public string Street { get; init; }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForMissingEqualityAttribute()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Address : ValueObject<Address>
            {
                [IncludeInEquality]
                public string Street { get; init; }
                
                public string City { get; init; } // Missing attribute
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForMissingCustomMethods()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Person : ValueObject<Person>
            {
                [CustomEquality]
                public string Name { get; init; }
                
                // Missing IsEqualName and GetHashCodeName methods
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForSequenceOnNonEnumerable()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Product : ValueObject<Product>
            {
                [SequenceEquality]
                public string Name { get; init; } // String is not treated as sequence
                
                [SequenceEquality]
                public int Count { get; init; } // Not an enumerable
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task HandlesFieldsAndProperties()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class MixedMembers : ValueObject<MixedMembers>
            {
                [IncludeInEquality]
                public string Property { get; init; }
                
                [IncludeInEquality]
                private readonly int _field;
                
                [IgnoreEquality]
                private readonly string _ignoredField;
                
                public MixedMembers(string property, int field)
                {
                    Property = property;
                    _field = field;
                }
            }
            """;

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DoesNotGenerateCode_ForNonValueObjectClass()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            // Missing [ValueObject] attribute
            public partial class Address : ValueObject<Address>
            {
                [IncludeInEquality]
                public string Street { get; init; }
            }
            """;

        return TestHelper.Verify(source);
    }
}