using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DomainBase.Analyzers;

/// <summary>
/// Analyzer for classes inheriting from <c>DomainBase.Enumeration</c>.
/// Reports DBEN003 when the class is not declared partial.
/// Reports DBEN001/DBEN002 when duplicate int values or string names are used
/// in static instances where constructor arguments are literal constants.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumerationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DuplicateValueError = new(
        id: "DBEN001",
        title: "Duplicate enumeration value",
        messageFormat: "The enumeration '{0}' has duplicate value '{1}'. Values must be unique within an enumeration type.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateNameError = new(
        id: "DBEN002",
        title: "Duplicate enumeration name",
        messageFormat: "The enumeration '{0}' has duplicate name '{1}'. Names must be unique within an enumeration type.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonPartialClassError = new(
        id: "DBEN003",
        title: "Enumeration class must be partial",
        messageFormat: "The enumeration '{0}' must be declared as a partial class to enable source generation",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// The set of diagnostics that this analyzer can produce.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DuplicateValueError, DuplicateNameError, NonPartialClassError);

    /// <summary>
    /// Registers analysis actions.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeEnumeration, SymbolKind.NamedType);
    }

    private static void AnalyzeEnumeration(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
            return;

        if (!InheritsFromEnumeration(typeSymbol))
            return;

        // Must be partial
        foreach (var decl in typeSymbol.DeclaringSyntaxReferences)
        {
            if (decl.GetSyntax() is ClassDeclarationSyntax cds)
            {
                if (!cds.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    context.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, cds.Identifier.GetLocation(), typeSymbol.Name));
                }
            }
        }

        // Find static readonly fields assigned with 'new {Type}(int, string)'
        var valueToLocation = new Dictionary<int, Location>();
        var nameToLocation = new Dictionary<string, Location>(StringComparer.Ordinal);

        foreach (var member in typeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.IsStatic || !member.IsReadOnly)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(member.Type, typeSymbol))
                continue;

            foreach (var syntaxRef in member.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is not VariableDeclaratorSyntax declarator)
                    continue;

                BaseObjectCreationExpressionSyntax? creation = declarator.Initializer?.Value switch
                {
                    ObjectCreationExpressionSyntax o => o,
                    ImplicitObjectCreationExpressionSyntax i => i,
                    _ => null
                };
                if (creation is null)
                    continue;

                var args = creation.ArgumentList?.Arguments;
                if (args == null || args.Value.Count < 2)
                    continue;

                var valueArg = args.Value[0].Expression;
                var nameArg = args.Value[1].Expression;

                // Only handle literal constants to avoid semantic model usage (RS1030)
                if (valueArg is LiteralExpressionSyntax { Token.Value: int intValue })
                {
                    if (valueToLocation.TryGetValue(intValue, out var existingLoc))
                    {
                        var loc = declarator.GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(DuplicateValueError, loc, typeSymbol.Name, intValue));
                        context.ReportDiagnostic(Diagnostic.Create(DuplicateValueError, existingLoc, typeSymbol.Name, intValue));
                    }
                    else
                    {
                        valueToLocation[intValue] = declarator.GetLocation();
                    }
                }

                if (nameArg is LiteralExpressionSyntax { Token.Value: string strName })
                {
                    if (nameToLocation.TryGetValue(strName, out var existingLoc))
                    {
                        var loc = declarator.GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(DuplicateNameError, loc, typeSymbol.Name, strName));
                        context.ReportDiagnostic(Diagnostic.Create(DuplicateNameError, existingLoc, typeSymbol.Name, strName));
                    }
                    else
                    {
                        nameToLocation[strName] = declarator.GetLocation();
                    }
                }
            }
        }
    }

    private static bool InheritsFromEnumeration(INamedTypeSymbol type)
    {
        var baseType = type;
        while ((baseType = baseType.BaseType) != null)
        {
            if (baseType.ToDisplayString() == "DomainBase.Enumeration")
                return true;
        }
        return false;
    }
}

