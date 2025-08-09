namespace DomainBase.Generators.Tests;

public class ValueObjectValidationTests
{
    [Fact]
    public Task ReportsDiagnostic_ForEqualityAttributeOnNonValueObjectClass()
    {
        var source = """
            using DomainBase;
            using System.Collections.Generic;

            namespace TestNamespace;

            public class Product  // No [ValueObject] attribute
            {
                [IncludeInEquality]
                public string Name { get; init; }
                
                [CustomEquality]
                public int Price { get; init; }
                
                [SequenceEquality]
                public List<string> Tags { get; set; }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForValueObjectAttributeWithoutInheritance()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Product  // Doesn't inherit from ValueObject<T>
            {
                [IncludeInEquality]
                public string Name { get; init; }
                
                [IncludeInEquality]
                public int Price { get; init; }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task NoDiagnostic_ForProperValueObjectUsage()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Product : ValueObject<Product>
            {
                [IncludeInEquality]
                public string Name { get; init; }
                
                [CustomEquality]
                public int Price { get; init; }
                
                private partial void IsEqualPrice(int price, int otherPrice, ref bool result)
                {
                    result = price == otherPrice;
                }
                
                private partial void GetHashCodePrice(int price, ref System.HashCode hashCode)
                {
                    hashCode.Add(price);
                }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task ReportsBothDiagnostics_ForCombinedIssues()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            // Class with equality attributes but no [ValueObject]
            public class ClassA
            {
                [IncludeInEquality]
                public string Name { get; init; }
            }

            // Class with [ValueObject] but no inheritance
            [ValueObject]
            public partial class ClassB
            {
                [IncludeInEquality]
                public string Name { get; init; }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }
}