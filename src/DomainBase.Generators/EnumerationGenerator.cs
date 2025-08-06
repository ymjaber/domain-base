using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DomainBase.Generators;

[Generator]
public class EnumerationGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor DuplicateValueError = new(
        id: "DBENUM001",
        title: "Duplicate enumeration value",
        messageFormat: "The enumeration '{0}' has duplicate value '{1}'. Values must be unique within an enumeration type.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateNameError = new(
        id: "DBENUM002", 
        title: "Duplicate enumeration name",
        messageFormat: "The enumeration '{0}' has duplicate name '{1}'. Names must be unique within an enumeration type.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonPartialClassError = new(
        id: "DBENUM003",
        title: "Enumeration class must be partial",
        messageFormat: "The enumeration '{0}' must be declared as a partial class to enable source generation",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax c && c.BaseList != null;

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        
        if (classSymbol == null)
            return null;
            
        // Check if the class inherits from Enumeration
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == "DomainBase.Enumeration")
                return classDeclarationSyntax;
            baseType = baseType.BaseType;
        }
        
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var distinctClasses = classes.Where(x => x is not null).Distinct();
        var enumerationSymbol = compilation.GetTypeByMetadataName("DomainBase.Enumeration");
        var jsonConverterAttributeSymbol = compilation.GetTypeByMetadataName("DomainBase.GenerateJsonConverterAttribute");
        var efValueConverterAttributeSymbol = compilation.GetTypeByMetadataName("DomainBase.GenerateEfValueConverterAttribute");

        if (enumerationSymbol == null)
            return;

        foreach (var classDeclaration in distinctClasses)
        {
            if (classDeclaration == null)
                continue;
                
            context.CancellationToken.ThrowIfCancellationRequested();

            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null || !InheritsFrom(classSymbol, enumerationSymbol))
                continue;

            // Check if class is partial
            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NonPartialClassError,
                    classDeclaration.Identifier.GetLocation(),
                    classSymbol.Name));
                continue;
            }

            var fields = GetEnumerationFieldsWithDiagnostics(classSymbol, context);
            
            if (fields == null)
                continue; // Errors were reported
                
            var generateJsonConverter = jsonConverterAttributeSymbol != null && 
                classSymbol.GetAttributes().Any(ad => ad.AttributeClass?.Equals(jsonConverterAttributeSymbol, SymbolEqualityComparer.Default) == true);
            var generateEfValueConverter = efValueConverterAttributeSymbol != null && 
                classSymbol.GetAttributes().Any(ad => ad.AttributeClass?.Equals(efValueConverterAttributeSymbol, SymbolEqualityComparer.Default) == true);
                
            var source = GenerateEnumerationExtensions(classSymbol, generateJsonConverter, generateEfValueConverter, fields);
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol classSymbol, INamedTypeSymbol baseSymbol)
    {
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (current.Equals(baseSymbol, SymbolEqualityComparer.Default))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string GenerateEnumerationExtensions(INamedTypeSymbol classSymbol, bool generateJsonConverter, bool generateEfValueConverter, List<(string name, int value, string displayName)> fields)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {className}");
        sb.AppendLine("    {");
        
        GenerateStaticFields(sb, className, fields);
        GenerateGetAllMethod(sb, className, fields);
        GenerateFromValueMethod(sb, className, fields);
        GenerateFromNameMethod(sb, className, fields);
        GenerateTryFromValueMethod(sb, className);
        GenerateTryFromNameMethod(sb, className);
        
        sb.AppendLine("    }");
        
        if (generateJsonConverter)
        {
            GenerateJsonConverter(sb, namespaceName, className);
        }
        
        if (generateEfValueConverter)
        {
            GenerateEfValueConverter(sb, namespaceName, className);
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static void GenerateStaticFields(StringBuilder sb, string className, List<(string name, int value, string displayName)> fields)
    {
        sb.AppendLine($"        private static readonly Dictionary<int, {className}> _byValue = new Dictionary<int, {className}>()");
        sb.AppendLine("        {");
        foreach (var field in fields)
        {
            sb.AppendLine($"            [{field.value}] = {field.name},");
        }
        sb.AppendLine("        };");
        sb.AppendLine();
        
        sb.AppendLine($"        private static readonly Dictionary<string, {className}> _byName = new Dictionary<string, {className}>()");
        sb.AppendLine("        {");
        foreach (var field in fields)
        {
            sb.AppendLine($"            [\"{field.displayName}\"] = {field.name},");
        }
        sb.AppendLine("        };");
        sb.AppendLine();
    }

    private static void GenerateGetAllMethod(StringBuilder sb, string className, List<(string name, int value, string displayName)> fields)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets all values of {className} ordered by value.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static IReadOnlyCollection<{className}> GetAll() => _byValue.Values;");
        sb.AppendLine();
    }

    private static void GenerateFromValueMethod(StringBuilder sb, string className, List<(string name, int value, string displayName)> fields)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the {className} instance from its value.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static {className} FromValue(int value)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_byValue.TryGetValue(value, out var result))");
        sb.AppendLine("                return result;");
        sb.AppendLine($"            throw new InvalidOperationException($\"No {className} with value {{value}} found.\");");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateFromNameMethod(StringBuilder sb, string className, List<(string name, int id, string displayName)> fields)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the {className} instance from its name.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static {className} FromName(string name)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_byName.TryGetValue(name, out var result))");
        sb.AppendLine("                return result;");
        sb.AppendLine($"            throw new InvalidOperationException($\"No {className} with name '{{name}}' found.\");");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateTryFromValueMethod(StringBuilder sb, string className)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Tries to get the {className} instance from its value.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static bool TryFromValue(int value, out {className}? result)");
        sb.AppendLine("        {");
        sb.AppendLine("            return _byValue.TryGetValue(value, out result);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateTryFromNameMethod(StringBuilder sb, string className)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Tries to get the {className} instance from its name.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static bool TryFromName(string name, out {className}? result)");
        sb.AppendLine("        {");
        sb.AppendLine("            return _byName.TryGetValue(name, out result);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateJsonConverter(StringBuilder sb, string namespaceName, string className)
    {
        sb.AppendLine();
        sb.AppendLine("#if NET6_0_OR_GREATER");
        sb.AppendLine($"    public class {className}JsonConverter : System.Text.Json.Serialization.JsonConverter<{className}>");
        sb.AppendLine("    {");
        sb.AppendLine($"        public override {className}? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == System.Text.Json.JsonTokenType.Number && reader.TryGetInt32(out var value))");
        sb.AppendLine("            {");
        sb.AppendLine($"                return {className}.TryFromValue(value, out var result) ? result : null;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (reader.TokenType == System.Text.Json.JsonTokenType.String)");
        sb.AppendLine("            {");
        sb.AppendLine("                var name = reader.GetString();");
        sb.AppendLine($"                return name != null && {className}.TryFromName(name, out var result) ? result : null;");
        sb.AppendLine("            }");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public override void Write(System.Text.Json.Utf8JsonWriter writer, {className} value, System.Text.Json.JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteNumberValue(value.Value);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("#endif");
    }

    private static void GenerateEfValueConverter(StringBuilder sb, string namespaceName, string className)
    {
        sb.AppendLine();
        sb.AppendLine("#if NET6_0_OR_GREATER");
        sb.AppendLine($"    public class {className}ValueConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{className}, int>");
        sb.AppendLine("    {");
        sb.AppendLine($"        public {className}ValueConverter() : base(");
        sb.AppendLine("            v => v.Value,");
        sb.AppendLine($"            v => {className}.FromValue(v))");
        sb.AppendLine("        {");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("#endif");
    }

    private static List<(string name, int value, string displayName)>? GetEnumerationFieldsWithDiagnostics(
        INamedTypeSymbol classSymbol, 
        SourceProductionContext context)
    {
        var fields = new List<(string name, int value, string displayName, Location location)>();
        var valueMap = new Dictionary<int, (string fieldName, Location location)>();
        var nameMap = new Dictionary<string, (string fieldName, Location location)>();
        var hasErrors = false;
        
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IFieldSymbol field && 
                field.IsStatic && 
                field.IsReadOnly && 
                field.Type.Equals(classSymbol, SymbolEqualityComparer.Default))
            {
                var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (syntax is VariableDeclaratorSyntax declarator && declarator.Initializer?.Value != null)
                {
                    BaseObjectCreationExpressionSyntax? creation = declarator.Initializer.Value switch
                    {
                        ObjectCreationExpressionSyntax objectCreation => objectCreation,
                        ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation,
                        _ => null
                    };

                    if (creation?.ArgumentList?.Arguments.Count >= 2)
                    {
                        var valueArg = creation.ArgumentList.Arguments[0].Expression;
                        var nameArg = creation.ArgumentList.Arguments[1].Expression;
                        
                        if (valueArg is LiteralExpressionSyntax valueLiteral && valueLiteral.Token.Value is int value &&
                            nameArg is LiteralExpressionSyntax nameLiteral && nameLiteral.Token.Value is string name)
                        {
                        var location = declarator.GetLocation();
                        
                        // Check for duplicate value
                        if (valueMap.TryGetValue(value, out var existingValue))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DuplicateValueError,
                                location,
                                classSymbol.Name,
                                value));
                            context.ReportDiagnostic(Diagnostic.Create(
                                DuplicateValueError,
                                existingValue.location,
                                classSymbol.Name,
                                value));
                            hasErrors = true;
                        }
                        else
                        {
                            valueMap[value] = (field.Name, location);
                        }
                        
                        // Check for duplicate name
                        if (nameMap.TryGetValue(name, out var existingName))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DuplicateNameError,
                                location,
                                classSymbol.Name,
                                name));
                            context.ReportDiagnostic(Diagnostic.Create(
                                DuplicateNameError,
                                existingName.location,
                                classSymbol.Name,
                                name));
                            hasErrors = true;
                        }
                        else
                        {
                            nameMap[name] = (field.Name, location);
                        }
                        
                        fields.Add((field.Name, value, name, location));
                        }
                    }
                }
            }
        }
        
        if (hasErrors)
            return null;
            
        return fields.OrderBy(f => f.value).Select(f => (f.name, f.value, f.displayName)).ToList();
    }

}