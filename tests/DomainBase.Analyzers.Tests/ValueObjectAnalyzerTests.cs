using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DomainBase.Analyzers.Tests;

public class ValueObjectAnalyzerTests
{
    private static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) Analyze(string source)
    {
        var code = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class Product : ValueObject<Product>
            {
                [IncludeInEquality]
                public string Name { get; init; }

                [CustomEquality]
                public int Price { get; init; }
            }
            """;

        var valueObjectSource = """
            namespace DomainBase
            {
                public abstract class ValueObject<TSelf> where TSelf : ValueObject<TSelf>
                {
                    protected abstract bool EqualsCore(TSelf other);
                    protected abstract int GetHashCodeCore();
                }
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class ValueObjectAttribute : System.Attribute { }
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class IncludeInEqualityAttribute : System.Attribute { public int Priority { get; set; } }
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class CustomEqualityAttribute : System.Attribute { public int Priority { get; set; } }
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class IgnoreEqualityAttribute : System.Attribute { }
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class SequenceEqualityAttribute : System.Attribute { public bool OrderMatters { get; set; } public bool DeepEquality { get; set; } public int Priority { get; set; } }
            }
            """;

        var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(code), CSharpSyntaxTree.ParseText(valueObjectSource) };
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DomainBase.Enumeration).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create("Tests", syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new ValueObjectAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
        var withAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = withAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return (compilation, diagnostics);
    }

    [Fact]
    public void ReportsImmutabilityDiagnostics()
    {
        var (_, diagnostics) = Analyze("");
        Assert.DoesNotContain(diagnostics, d => d.Id == "DBVO010");
        Assert.DoesNotContain(diagnostics, d => d.Id == "DBVO011");
        Assert.Contains(diagnostics, d => d.Id == "DBVO003");
        Assert.Contains(diagnostics, d => d.Id == "DBVO004");
    }

    [Fact]
    public async Task InheritsValueObject_WithoutAttribute_ReportsImmutabilityWarnings()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public class PersonName : ValueObject<PersonName>
            {
                public string First { get; set; }
                public int Age;
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DomainBase.Enumeration).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create("Tests", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ValueObjectAnalyzer());
        var diags = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        Assert.Contains(diags, d => d.Id == "DBVO010");
        Assert.Contains(diags, d => d.Id == "DBVO011");
    }

    [Fact]
    public async Task SimpleValueObject_ReportsWarning_WhenDeclaringAdditionalProperty()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public sealed class Age : ValueObject<Age, int>
            {
                public Age(int value) : base(value) {}
                public int Extra { get; }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DomainBase.Enumeration).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create("Tests", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ValueObjectAnalyzer());
        var diags = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        Assert.Contains(diags, d => d.Id == "DBVO012");
    }

    [Fact]
    public async Task SimpleValueObject_ReportsWarning_WhenDeclaringAdditionalField()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public sealed class Weight : ValueObject<Weight, int>
            {
                public Weight(int value) : base(value) {}
                private readonly int _ignoredBacking = 0; // readonly still counts as additional
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DomainBase.Enumeration).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create("Tests", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ValueObjectAnalyzer());
        var diags = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        Assert.Contains(diags, d => d.Id == "DBVO012");
    }
}

