using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using DomainBase.Generators.CodeFixes;

namespace DomainBase.CodeFixes.Tests;

public class MakeClassPartialCodeFixTests
{
    [Fact]
    public async Task AddsPartialModifier_ForValueObject()
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

        var tree = CSharpSyntaxTree.ParseText(source);
        var baseRefs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DomainBase.Enumeration).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.Unsafe).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
        };
        var runtimeAssemblyLocation = typeof(object).Assembly.Location;
        var runtimeDirectory = System.IO.Path.GetDirectoryName(runtimeAssemblyLocation);
        if (runtimeDirectory != null)
        {
            var systemRuntimePath = System.IO.Path.Combine(runtimeDirectory, "System.Runtime.dll");
            if (System.IO.File.Exists(systemRuntimePath))
            {
                baseRefs.Add(MetadataReference.CreateFromFile(systemRuntimePath));
            }
        }
        var references = baseRefs;
        var compilation = CSharpCompilation.Create("Tests", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new DomainBase.Analyzers.ValueObjectAnalyzer();
        var analyzers = System.Collections.Immutable.ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
        var withAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.FirstOrDefault(d => d.Id == "DBVO001");
        Assert.NotNull(diagnostic);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(references)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var document = workspace.AddDocument(project.Id, "Test.cs", await tree.GetTextAsync());

        var provider = new MakeClassPartialCodeFixProvider();
        var actions = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new CodeFixContext(document, diagnostic!, (a, d) => actions.Add(a), CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);

        Assert.NotEmpty(actions);
    }
}

