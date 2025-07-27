using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using VerifyTests;

namespace DomainBase.Generators.Tests;

internal static class TestHelper
{
    private static readonly VerifySettings _verifySettings = new();

    static TestHelper()
    {
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.UseUniqueDirectory();
    }

    public static Task Verify(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EnumerationGenerator();

        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        return Verifier.Verify(new
        {
            Diagnostics = diagnostics.Select(d => new
            {
                d.Id,
                d.Severity,
                Message = d.GetMessage(),
                Location = FormatLocation(d.Location)
            }),
            GeneratedSources = runResult.Results.SelectMany(r => r.GeneratedSources)
                .Select(s => new
                {
                    s.HintName,
                    Content = s.SourceText.ToString()
                })
        }, _verifySettings);
    }

    public static Task VerifyDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EnumerationGenerator();

        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        var generatorDiagnostics = runResult.Results
            .SelectMany(r => r.Diagnostics)
            .Select(d => new
            {
                d.Id,
                d.Severity,
                Message = d.GetMessage(),
                Location = FormatLocation(d.Location)
            });

        return Verifier.Verify(generatorDiagnostics, _verifySettings);
    }

    private static string FormatLocation(Location location)
    {
        if (location.IsInSource)
        {
            var lineSpan = location.GetLineSpan();
            return $"{lineSpan.Path}({lineSpan.StartLinePosition.Line + 1},{lineSpan.StartLinePosition.Character + 1})";
        }
        return location.ToString();
    }

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(DomainBase.Enumeration).Assembly,
            typeof(System.Runtime.CompilerServices.Unsafe).Assembly,
            typeof(Attribute).Assembly
        };

        // Get runtime assembly
        var runtimeAssemblyLocation = typeof(object).Assembly.Location;
        var runtimeDirectory = System.IO.Path.GetDirectoryName(runtimeAssemblyLocation);
        var systemRuntimePath = runtimeDirectory != null 
            ? System.IO.Path.Combine(runtimeDirectory, "System.Runtime.dll")
            : null;

        var references = assemblies
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToList();

        if (systemRuntimePath != null && System.IO.File.Exists(systemRuntimePath))
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
        }

        return references.ToImmutableArray();
    }
}