using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyXunit;

namespace DomainBase.Generators.Tests;

public class NonPartialClassDiagnosticTest
{
    [Fact]
    public Task ReportsDiagnostic_ForNonPartialClass()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public class InvalidStatus : Enumeration
            {
                public static readonly InvalidStatus Active = new(1, "Active");

                public InvalidStatus(int value, string name) : base(value, name) { }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }
}