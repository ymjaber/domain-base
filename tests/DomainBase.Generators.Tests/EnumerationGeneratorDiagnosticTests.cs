using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyXunit;

namespace DomainBase.Generators.Tests;

public class EnumerationGeneratorDiagnosticTests
{
    [Fact]
    public Task ReportsDiagnostic_ForDuplicateIds()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public partial class Status : Enumeration
            {
                public static readonly Status Active = new(1, "Active");
                public static readonly Status Inactive = new({|DBEN001:1|}, "Inactive");
                public static readonly Status Pending = new(2, "Pending");

                public Status(int value, string name) : base(value, name) { }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForDuplicateNames()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public partial class Status : Enumeration
            {
                public static readonly Status First = new(1, "Active");
                public static readonly Status Second = new(2, {|DBEN002:"Active"|});
                public static readonly Status Third = new(3, "Pending");

                public Status(int value, string name) : base(value, name) { }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task ReportsDiagnostic_ForMultipleDuplicates()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public partial class Status : Enumeration
            {
                public static readonly Status First = new(1, "Active");
                public static readonly Status Second = new({|DBEN001:1|}, {|DBEN002:"Active"|});
                public static readonly Status Third = new({|DBEN001:1|}, "Pending");
                public static readonly Status Fourth = new(2, {|DBEN002:"Active"|});

                public Status(int value, string name) : base(value, name) { }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task DoesNotReportDiagnostic_ForUniqueValues()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public partial class Status : Enumeration
            {
                public static readonly Status Active = new(1, "Active");
                public static readonly Status Inactive = new(2, "Inactive");
                public static readonly Status Pending = new(3, "Pending");

                public Status(int value, string name) : base(value, name) { }
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }

    [Fact]
    public Task HandlesNonConstantValues_WithoutDiagnostics()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public partial class Status : Enumeration
            {
                private static int counter = 1;
                
                public static readonly Status Active = new(counter++, "Active");
                public static readonly Status Inactive = new(counter++, GetName());
                public static readonly Status Pending = new(3, "Pending");

                public Status(int value, string name) : base(value, name) { }
                
                private static string GetName() => "Inactive";
            }
            """;

        return TestHelper.VerifyDiagnostics(source);
    }
}