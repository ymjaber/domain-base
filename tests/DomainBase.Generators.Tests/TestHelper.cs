using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using VerifyTests;
using DomainBase.Generators;
using DomainBase.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;

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
        // Add ValueObject base class and attributes to the compilation
        var valueObjectSource = """
            namespace DomainBase
            {
                public abstract class ValueObject<TSelf> where TSelf : ValueObject<TSelf>
                {
                    protected abstract bool EqualsCore(TSelf other);
                    protected abstract int GetHashCodeCore();
                    public override bool Equals(object? obj) => obj is TSelf other && EqualsCore(other);
                    public override int GetHashCode() => GetHashCodeCore();
                }
                
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class ValueObjectAttribute : System.Attribute { }
                
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class IncludeInEqualityAttribute : System.Attribute 
                { 
                    public int Priority { get; set; } = 0;
                }
                
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class CustomEqualityAttribute : System.Attribute 
                { 
                    public int Priority { get; set; } = 0;
                }
                
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class IgnoreEqualityAttribute : System.Attribute { }
                
                [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
                public sealed class SequenceEqualityAttribute : System.Attribute 
                { 
                    public bool OrderMatters { get; set; } = true;
                    public bool DeepEquality { get; set; } = true;
                    public int Priority { get; set; } = 0;
                }
            }
            """;
        
        var syntaxTrees = new[] { 
            CSharpSyntaxTree.ParseText(source),
            CSharpSyntaxTree.ParseText(valueObjectSource)
        };
        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generators = new IIncrementalGenerator[] { new EnumerationGenerator(), new ValueObjectGenerator() };

        var driver = CSharpGeneratorDriver.Create(generators)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        // Run analyzers to collect diagnostics (generators do not emit analyzer diagnostics)
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EnumerationAnalyzer(), new ValueObjectAnalyzer());
        var analyzerDiagnostics = outputCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().Result;
        var orderedDiagnostics = analyzerDiagnostics
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ThenBy(d => d.Location.GetLineSpan().Path)
            .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Line)
            .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Character)
            .ThenBy(d => d.Id)
            .Select(d => new
            {
                d.Id,
                d.Severity,
                Message = d.GetMessage(),
                Location = FormatLocation(d.Location)
            })
            .ToList();

        object payload = orderedDiagnostics.Count == 0
            ? new
            {
                GeneratedSources = runResult.Results.SelectMany(r => r.GeneratedSources)
                    .Select(s => new
                    {
                        s.HintName,
                        Content = s.SourceText.ToString()
                    })
            }
            : new
            {
                Diagnostics = (IEnumerable<object>)orderedDiagnostics,
                GeneratedSources = runResult.Results.SelectMany(r => r.GeneratedSources)
                    .Select(s => new
                    {
                        s.HintName,
                        Content = s.SourceText.ToString()
                    })
            };

        return Verifier.Verify(payload, _verifySettings);
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

        var generators = new IIncrementalGenerator[] { new EnumerationGenerator(), new ValueObjectGenerator() };

        var driver = CSharpGeneratorDriver.Create(generators)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        // Analyzer diagnostics (not generator diagnostics)
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EnumerationAnalyzer(), new ValueObjectAnalyzer());
        var analyzerDiagnostics = outputCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().Result;
        var orderedDiagnostics = analyzerDiagnostics
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ThenBy(d => d.Location.GetLineSpan().Path)
            .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Line)
            .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Character)
            .ThenBy(d => d.Id)
            .Select(d => new
            {
                d.Id,
                d.Severity,
                Message = d.GetMessage(),
                Location = FormatLocation(d.Location)
            });

        return Verifier.Verify(orderedDiagnostics, _verifySettings);
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