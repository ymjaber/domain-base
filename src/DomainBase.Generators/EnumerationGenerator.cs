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
    private static readonly DiagnosticDescriptor DuplicateIdError = new(
        id: "DBENUM001",
        title: "Duplicate enumeration ID",
        messageFormat: "The enumeration '{0}' has duplicate ID '{1}'. IDs must be unique within an enumeration type.",
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
        => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0 && c.Modifiers.Any(SyntaxKind.PartialKeyword);

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                if (symbol is not IMethodSymbol attributeSymbol)
                    continue;

                var attributeType = attributeSymbol.ContainingType;
                var fullName = attributeType.ToDisplayString();

                if (fullName == "DomainBase.EnumerationAttribute")
                    return classDeclarationSyntax;
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var distinctClasses = classes.Where(x => x is not null).Distinct();
        var enumerationSymbol = compilation.GetTypeByMetadataName("DomainBase.Enumeration");
        var attributeSymbol = compilation.GetTypeByMetadataName("DomainBase.EnumerationAttribute");

        if (enumerationSymbol == null || attributeSymbol == null)
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

            var attributeData = classSymbol.GetAttributes()
                .FirstOrDefault(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

            if (attributeData == null)
                continue;

            var fields = GetEnumerationFieldsWithDiagnostics(classSymbol, context);
            
            if (fields == null)
                continue; // Errors were reported
                
            var source = GenerateEnumerationExtensions(classSymbol, attributeData, fields);
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

    private static string GenerateEnumerationExtensions(INamedTypeSymbol classSymbol, AttributeData attributeData, List<(string name, int id, string displayName)> fields)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        
        var generateJsonConverter = GetAttributeValue(attributeData, "GenerateJsonConverter", true);
        var generateEfValueConverter = GetAttributeValue(attributeData, "GenerateEfValueConverter", false);
        
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

    private static void GenerateStaticFields(StringBuilder sb, string className, List<(string name, int id, string displayName)> fields)
    {
        sb.AppendLine($"        private static readonly Dictionary<int, {className}> _byId = new Dictionary<int, {className}>()");
        sb.AppendLine("        {");
        foreach (var field in fields)
        {
            sb.AppendLine($"            [{field.id}] = {field.name},");
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

    private static void GenerateGetAllMethod(StringBuilder sb, string className, List<(string name, int id, string displayName)> fields)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets all values of {className} ordered by ID.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static IReadOnlyCollection<{className}> GetAll() => _byId.Values;");
        sb.AppendLine();
    }

    private static void GenerateFromValueMethod(StringBuilder sb, string className, List<(string name, int id, string displayName)> fields)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the {className} instance from its ID value.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static {className} FromValue(int id)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_byId.TryGetValue(id, out var result))");
        sb.AppendLine("                return result;");
        sb.AppendLine($"            throw new InvalidOperationException($\"No {className} with Id {{id}} found.\");");
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
        sb.AppendLine($"        /// Tries to get the {className} instance from its ID value.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static bool TryFromValue(int id, out {className}? result)");
        sb.AppendLine("        {");
        sb.AppendLine("            return _byId.TryGetValue(id, out result);");
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
        sb.AppendLine("            if (reader.TokenType == System.Text.Json.JsonTokenType.Number && reader.TryGetInt32(out var id))");
        sb.AppendLine("            {");
        sb.AppendLine($"                return {className}.TryFromValue(id, out var result) ? result : null;");
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
        sb.AppendLine("            writer.WriteNumberValue(value.Id);");
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
        sb.AppendLine("            v => v.Id,");
        sb.AppendLine($"            v => {className}.FromValue(v))");
        sb.AppendLine("        {");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("#endif");
    }

    private static List<(string name, int id, string displayName)>? GetEnumerationFieldsWithDiagnostics(
        INamedTypeSymbol classSymbol, 
        SourceProductionContext context)
    {
        var fields = new List<(string name, int id, string displayName, Location location)>();
        var idMap = new Dictionary<int, (string fieldName, Location location)>();
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
                        var idArg = creation.ArgumentList.Arguments[0].Expression;
                        var nameArg = creation.ArgumentList.Arguments[1].Expression;
                        
                        if (idArg is LiteralExpressionSyntax idLiteral && idLiteral.Token.Value is int id &&
                            nameArg is LiteralExpressionSyntax nameLiteral && nameLiteral.Token.Value is string name)
                        {
                        var location = declarator.GetLocation();
                        
                        // Check for duplicate ID
                        if (idMap.TryGetValue(id, out var existingId))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DuplicateIdError,
                                location,
                                classSymbol.Name,
                                id));
                            context.ReportDiagnostic(Diagnostic.Create(
                                DuplicateIdError,
                                existingId.location,
                                classSymbol.Name,
                                id));
                            hasErrors = true;
                        }
                        else
                        {
                            idMap[id] = (field.Name, location);
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
                        
                        fields.Add((field.Name, id, name, location));
                        }
                    }
                }
            }
        }
        
        if (hasErrors)
            return null;
            
        return fields.OrderBy(f => f.id).Select(f => (f.name, f.id, f.displayName)).ToList();
    }

    private static bool GetAttributeValue(AttributeData attributeData, string propertyName, bool defaultValue)
    {
        var namedArgument = attributeData.NamedArguments.FirstOrDefault(na => na.Key == propertyName);
        if (namedArgument.Value.Value != null)
        {
            return (bool)namedArgument.Value.Value;
        }
        return defaultValue;
    }
}