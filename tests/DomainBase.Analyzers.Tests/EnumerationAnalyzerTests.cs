using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DomainBase.Analyzers.Tests;

public class EnumerationAnalyzerTests
{
    [Fact]
    public async Task ReportsDuplicateValueAndName()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            public partial class Status : Enumeration
            {
                public static readonly Status A = new(1, "One");
                public static readonly Status B = new(1, "One");
                public Status(int value, string name) : base(value, name) { }
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

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EnumerationAnalyzer());
        var diags = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        Assert.Contains(diags, d => d.Id == "DBENUM001");
        Assert.Contains(diags, d => d.Id == "DBENUM002");
    }
}

