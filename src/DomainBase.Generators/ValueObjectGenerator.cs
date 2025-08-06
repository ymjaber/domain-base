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
public class ValueObjectGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NonPartialClassError = new(
        id: "DBVO001",
        title: "ValueObject class must be partial",
        messageFormat: "The value object '{0}' must be declared as a partial class to enable source generation",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingEqualityAttributeWarning = new(
        id: "DBVO002",
        title: "Property or field missing equality attribute",
        messageFormat: "The {0} '{1}' in value object '{2}' should have an equality attribute",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingCustomEqualsError = new(
        id: "DBVO003",
        title: "Missing custom equality method",
        messageFormat: "The property '{0}' has [CustomEquality] but is missing the required static method 'Equals_{1}' with signature: private static void Equals_{1}(in T value, in T otherValue, out bool result)",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingCustomHashCodeError = new(
        id: "DBVO004",
        title: "Missing custom hash code method",
        messageFormat: "The property '{0}' has [CustomEquality] but is missing the required static method 'GetHashCode_{1}'",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor SequenceOnNonEnumerableInfo = new(
        id: "DBVO006",
        title: "SequenceEquality on non-sequence type",
        messageFormat: "The {0} '{1}' has [SequenceEquality] but does not implement IEnumerable. Consider using [IncludeInEquality] instead.",
        category: "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EqualityAttributeOnNonValueObjectError = new(
        id: "DBVO007",
        title: "Equality attribute on non-ValueObject class",
        messageFormat: "The {0} '{1}' has equality attribute '{2}' but the containing class '{3}' does not have the [ValueObject] attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ValueObjectAttributeWithoutInheritanceError = new(
        id: "DBVO008",
        title: "ValueObject attribute without inheritance",
        messageFormat: "The class '{0}' has [ValueObject] attribute but does not inherit from ValueObject<T>",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
        
    private static readonly DiagnosticDescriptor DuplicateMethodNamesError = new(
        id: "DBVO009",
        title: "Duplicate method names after cleaning",
        messageFormat: "The members '{0}' and '{1}' would generate the same method names after removing prefixes. Consider renaming one of them.",
        category: "Naming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Main generation pipeline
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc));

        // Validation pipeline for equality attributes on non-ValueObject classes
        var equalityAttributeValidation = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is MemberDeclarationSyntax,
                transform: static (ctx, _) => GetMembersWithEqualityAttributes(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(equalityAttributeValidation,
            static (spc, source) => ValidateEqualityAttributeUsage(spc, source!));

        // Validation pipeline for [ValueObject] without inheritance
        var valueObjectValidation = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClassWithValueObjectAttribute(s),
                transform: static (ctx, _) => GetValueObjectWithoutInheritance(ctx))
            .Where(static m => m is not null);

        var compilationAndValueObjectValidation = context.CompilationProvider.Combine(valueObjectValidation.Collect());
        
        context.RegisterSourceOutput(compilationAndValueObjectValidation,
            static (spc, source) => ReportValueObjectWithoutInheritance(spc, source.Left, source.Right));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0 && c.BaseList != null;

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        
        if (classSymbol == null)
            return null;
            
        // Check if the class has [ValueObject] attribute
        var hasValueObjectAttribute = classSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "DomainBase.ValueObjectAttribute");
            
        if (!hasValueObjectAttribute)
            return null;
            
        // Check if the class inherits from ValueObject<T>
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && 
                baseType.ConstructedFrom.ToDisplayString() == "DomainBase.ValueObject<TSelf>")
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
        var valueObjectSymbol = compilation.GetTypeByMetadataName("DomainBase.ValueObject`1");

        if (valueObjectSymbol == null)
            return;

        foreach (var classDeclaration in distinctClasses)
        {
            if (classDeclaration == null)
                continue;
                
            context.CancellationToken.ThrowIfCancellationRequested();

            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
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

            var members = AnalyzeMembers(classSymbol, context);
            if (members == null)
                continue; // Errors were reported
                
            var source = GenerateValueObjectExtensions(classSymbol, members);
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static List<MemberInfo>? AnalyzeMembers(INamedTypeSymbol classSymbol, SourceProductionContext context)
    {
        var members = new List<MemberInfo>();
        
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IPropertySymbol property && IsAutoProperty(property))
            {
                var memberInfo = AnalyzeMember(property, property.Type, property.Name, "property", classSymbol, context);
                if (memberInfo != null)
                    members.Add(memberInfo);
            }
            else if (member is IFieldSymbol field && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared)
            {
                var memberInfo = AnalyzeMember(field, field.Type, field.Name, "field", classSymbol, context);
                if (memberInfo != null)
                    members.Add(memberInfo);
            }
        }
        
        // Sort by priority (descending) then by name for stable ordering
        members.Sort((a, b) => 
        {
            var priorityCompare = b.Priority.CompareTo(a.Priority);
            return priorityCompare != 0 ? priorityCompare : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });
        
        // Check for duplicate method names after cleaning (only for custom equality)
        var customMembers = members.Where(m => m.EqualityKind == EqualityKind.Custom).ToList();
        var methodNameMap = new Dictionary<string, MemberInfo>();
        
        foreach (var member in customMembers)
        {
            var cleanMethodName = GetCleanMethodName(member.Name);
            if (methodNameMap.TryGetValue(cleanMethodName, out var existingMember))
            {
                // Report error for duplicate
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicateMethodNamesError,
                    member.Symbol.Locations.FirstOrDefault() ?? Location.None,
                    existingMember.Name,
                    member.Name));
                return null; // Don't generate code if there are duplicates
            }
            methodNameMap[cleanMethodName] = member;
        }
        
        return members;
    }

    private static bool IsAutoProperty(IPropertySymbol property)
    {
        return property.GetMethod != null && 
               property.GetMethod.DeclaredAccessibility != Accessibility.Private &&
               !property.IsStatic &&
               !property.IsIndexer &&
               !property.IsAbstract;
    }

    private static MemberInfo? AnalyzeMember(ISymbol member, ITypeSymbol type, string name, string memberKind, 
        INamedTypeSymbol classSymbol, SourceProductionContext context)
    {
        var attributes = member.GetAttributes();
        
        var includeAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "IncludeInEqualityAttribute");
        var customAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "CustomEqualityAttribute");
        var sequenceAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "SequenceEqualityAttribute");
        var ignoreAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "IgnoreEqualityAttribute");
        
        var attributeCount = new[] { includeAttr, customAttr, sequenceAttr, ignoreAttr }.Count(a => a != null);
        
        if (attributeCount == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingEqualityAttributeWarning,
                member.Locations.FirstOrDefault() ?? Location.None,
                memberKind,
                name,
                classSymbol.Name));
            return null;
        }
        
        if (attributeCount > 1)
        {
            // Multiple attributes - this is an error but we'll just take the first one
            return null;
        }
        
        if (ignoreAttr != null)
            return null; // Explicitly ignored
            
        var memberInfo = new MemberInfo
        {
            Name = name,
            Type = type,
            MemberKind = memberKind,
            Symbol = member
        };
        
        if (includeAttr != null)
        {
            memberInfo.EqualityKind = EqualityKind.Include;
            memberInfo.Priority = GetAttributeValue(includeAttr, "Priority", 0);
        }
        else if (customAttr != null)
        {
            memberInfo.EqualityKind = EqualityKind.Custom;
            memberInfo.Priority = GetAttributeValue(customAttr, "Priority", 0);
            
            // Check for static custom methods
            var methodSuffix = GetCleanMethodName(name);
            var equalsMethod = classSymbol.GetMembers($"Equals_{methodSuffix}")
                .FirstOrDefault(m => m is IMethodSymbol method && method.IsStatic);
            var hashCodeMethod = classSymbol.GetMembers($"GetHashCode_{methodSuffix}")
                .FirstOrDefault(m => m is IMethodSymbol method && method.IsStatic);
            
            if (equalsMethod == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingCustomEqualsError,
                    member.Locations.FirstOrDefault() ?? Location.None,
                    name,
                    methodSuffix));
            }
            
            if (hashCodeMethod == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingCustomHashCodeError,
                    member.Locations.FirstOrDefault() ?? Location.None,
                    name,
                    methodSuffix));
            }
        }
        else if (sequenceAttr != null)
        {
            memberInfo.EqualityKind = EqualityKind.Sequence;
            memberInfo.Priority = GetAttributeValue(sequenceAttr, "Priority", 0);
            memberInfo.OrderMatters = GetAttributeValue(sequenceAttr, "OrderMatters", true);
            memberInfo.DeepEquality = GetAttributeValue(sequenceAttr, "DeepEquality", true);
            
            // Check if type implements IEnumerable
            if (!ImplementsIEnumerable(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SequenceOnNonEnumerableInfo,
                    member.Locations.FirstOrDefault() ?? Location.None,
                    memberKind,
                    name));
            }
        }
        
        return memberInfo;
    }

    private static bool ImplementsIEnumerable(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return false; // String implements IEnumerable but we don't want to treat it as a sequence
            
        var enumerableType = type.AllInterfaces.FirstOrDefault(i => 
            i.SpecialType == SpecialType.System_Collections_IEnumerable ||
            (i.IsGenericType && i.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T));
            
        return enumerableType != null;
    }

    private static T GetAttributeValue<T>(AttributeData attribute, string propertyName, T defaultValue)
    {
        var namedArgument = attribute.NamedArguments.FirstOrDefault(na => na.Key == propertyName);
        if (namedArgument.Value.Value != null)
        {
            return (T)namedArgument.Value.Value;
        }
        return defaultValue;
    }

    private static string GenerateValueObjectExtensions(INamedTypeSymbol classSymbol, List<MemberInfo> members)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {className}");
        sb.AppendLine("    {");
        
        GenerateEqualsCore(sb, className, members);
        GenerateGetHashCodeCore(sb, members);
        GenerateCustomPartialMethods(sb, members);
        
        if (members.Any(m => m.EqualityKind == EqualityKind.Sequence))
        {
            GenerateSequenceEqualityHelper(sb);
        }
        
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static void GenerateEqualsCore(StringBuilder sb, string className, List<MemberInfo> members)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Determines whether this {className} is equal to another based on their values.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        protected override bool EqualsCore({className} other)");
        sb.AppendLine("        {");
        
        // Add priority documentation if there are members with different priorities
        var priorityGroups = members.GroupBy(m => m.Priority).OrderByDescending(g => g.Key).ToList();
        if (priorityGroups.Count > 1)
        {
            sb.AppendLine("            // Properties are compared in priority order (highest to lowest):");
            foreach (var group in priorityGroups)
            {
                var memberNames = string.Join(", ", group.Select(m => m.Name));
                sb.AppendLine($"            // Priority {group.Key}: {memberNames}");
            }
            sb.AppendLine();
        }
        
        // Declare result variable once if we have custom equality members
        var hasCustomEquality = members.Any(m => m.EqualityKind == EqualityKind.Custom);
        if (hasCustomEquality)
        {
            sb.AppendLine("            bool result;");
            sb.AppendLine();
        }
        
        foreach (var member in members)
        {
            switch (member.EqualityKind)
            {
                case EqualityKind.Include:
                    GenerateSimpleEquality(sb, member);
                    break;
                case EqualityKind.Custom:
                    GenerateCustomEquality(sb, member);
                    break;
                case EqualityKind.Sequence:
                    GenerateSequenceEquality(sb, member);
                    break;
            }
        }
        
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateSimpleEquality(StringBuilder sb, MemberInfo member)
    {
        var memberAccess = member.Name;
        var otherAccess = $"other.{member.Name}";
        
        if (member.Type.IsValueType)
        {
            sb.AppendLine($"            if (!{memberAccess}.Equals({otherAccess})) return false;");
        }
        else
        {
            sb.AppendLine($"            if (!EqualityComparer<{member.Type.ToDisplayString()}>.Default.Equals({memberAccess}, {otherAccess})) return false;");
        }
    }

    private static void GenerateCustomEquality(StringBuilder sb, MemberInfo member)
    {
        var methodSuffix = GetCleanMethodName(member.Name);
        // Add priority comment if it's non-zero
        if (member.Priority != 0)
        {
            sb.AppendLine($"            // Priority {member.Priority}: {member.Name}");
        }
        sb.AppendLine($"            Equals_{methodSuffix}({member.Name}, other.{member.Name}, out result);");
        sb.AppendLine("            if (!result) return false;");
    }

    private static void GenerateSequenceEquality(StringBuilder sb, MemberInfo member)
    {
        var orderMatters = member.OrderMatters ? "true" : "false";
        sb.AppendLine($"            if (!SequenceEquals({member.Name}, other.{member.Name}, {orderMatters})) return false;");
    }

    private static void GenerateGetHashCodeCore(StringBuilder sb, List<MemberInfo> members)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Returns a hash code for this value object's values.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        protected override int GetHashCodeCore()");
        sb.AppendLine("        {");
        sb.AppendLine("            var hashCode = new HashCode();");
        sb.AppendLine();
        
        foreach (var member in members)
        {
            if (member.EqualityKind == EqualityKind.Custom)
            {
                var methodSuffix = GetCleanMethodName(member.Name);
                sb.AppendLine($"            GetHashCode_{methodSuffix}({member.Name}, ref hashCode);");
            }
            else if (member.EqualityKind == EqualityKind.Sequence)
            {
                sb.AppendLine($"            if ({member.Name} != null)");
                sb.AppendLine("            {");
                sb.AppendLine($"                foreach (var item in {member.Name})");
                sb.AppendLine("                    hashCode.Add(item);");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"            hashCode.Add({member.Name});");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("            return hashCode.ToHashCode();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateCustomPartialMethods(StringBuilder sb, List<MemberInfo> members)
    {
        // We no longer generate partial method declarations
        // The user must provide static methods in their code using the code fix
    }
    
    private static string GetSafeParameterName(string name)
    {
        // Handle field naming conventions (e.g., _fieldName, m_fieldName)
        var cleanName = name;
        
        // Remove common field prefixes
        if (cleanName.StartsWith("_"))
            cleanName = cleanName.Substring(1);
        else if (cleanName.StartsWith("m_"))
            cleanName = cleanName.Substring(2);
            
        // If the name is now empty or starts with a digit, use the original
        if (string.IsNullOrEmpty(cleanName) || char.IsDigit(cleanName[0]))
            cleanName = name;
        
        // Make first letter lowercase for parameter naming convention
        var paramName = cleanName.Length > 0 
            ? char.ToLower(cleanName[0]) + (cleanName.Length > 1 ? cleanName.Substring(1) : "")
            : cleanName;
        
        // C# keywords that need @ prefix
        var keywords = new HashSet<string> 
        { 
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };
        
        return keywords.Contains(paramName) ? $"@{paramName}" : paramName;
    }
    
    private static string GetCleanName(string name)
    {
        // Remove common field prefixes
        if (name.StartsWith("_"))
            return name.Substring(1);
        else if (name.StartsWith("m_"))
            return name.Substring(2);
            
        return name;
    }
    
    private static string GetCleanMethodName(string name)
    {
        var cleanName = GetCleanName(name);
        
        // If the cleaned name is empty or starts with a digit, use the original
        if (string.IsNullOrEmpty(cleanName) || char.IsDigit(cleanName[0]))
            return name;
            
        // Ensure first letter is uppercase for method name
        return char.ToUpper(cleanName[0]) + (cleanName.Length > 1 ? cleanName.Substring(1) : "");
    }

    private static void GenerateSequenceEqualityHelper(StringBuilder sb)
    {
        sb.AppendLine("        private static bool SequenceEquals<T>(IEnumerable<T>? first, IEnumerable<T>? second, bool orderMatters)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (ReferenceEquals(first, second)) return true;");
        sb.AppendLine("            if (first is null || second is null) return false;");
        sb.AppendLine();
        sb.AppendLine("            return orderMatters");
        sb.AppendLine("                ? first.SequenceEqual(second)");
        sb.AppendLine("                : first.OrderBy(x => x).SequenceEqual(second.OrderBy(x => x));");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static bool IsClassWithValueObjectAttribute(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
            return false;

        return classDeclaration.AttributeLists.Count > 0;
    }

    private static (ClassDeclarationSyntax Class, Location Location)? GetValueObjectWithoutInheritance(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        
        if (classSymbol == null)
            return null;
            
        // Check if the class has [ValueObject] attribute
        var valueObjectAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "DomainBase.ValueObjectAttribute");
            
        if (valueObjectAttribute == null)
            return null;
            
        // Check if the class inherits from ValueObject<T>
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && 
                baseType.ConstructedFrom.ToDisplayString() == "DomainBase.ValueObject<TSelf>")
                return null; // It does inherit, so no error
            baseType = baseType.BaseType;
        }
        
        // Has [ValueObject] but doesn't inherit from ValueObject<T>
        return (classDeclarationSyntax, valueObjectAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? classDeclarationSyntax.Identifier.GetLocation());
    }

    private static void ReportValueObjectWithoutInheritance(SourceProductionContext context, Compilation compilation, 
        ImmutableArray<(ClassDeclarationSyntax Class, Location Location)?> validationResults)
    {
        foreach (var data in validationResults)
        {
            if (data == null)
                continue;
                
            var semanticModel = compilation.GetSemanticModel(data.Value.Class.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(data.Value.Class);
            
            if (classSymbol != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ValueObjectAttributeWithoutInheritanceError,
                    data.Value.Location,
                    classSymbol.Name));
            }
        }
    }

    private class MemberWithEqualityAttribute
    {
        public ISymbol Member { get; set; } = null!;
        public string AttributeName { get; set; } = "";
        public INamedTypeSymbol ContainingClass { get; set; } = null!;
        public Location AttributeLocation { get; set; } = null!;
    }

    private static MemberWithEqualityAttribute? GetMembersWithEqualityAttributes(GeneratorSyntaxContext context)
    {
        ISymbol? member = null;
        
        if (context.Node is PropertyDeclarationSyntax property)
        {
            member = context.SemanticModel.GetDeclaredSymbol(property);
        }
        else if (context.Node is FieldDeclarationSyntax field)
        {
            var variable = field.Declaration.Variables.FirstOrDefault();
            if (variable != null)
                member = context.SemanticModel.GetDeclaredSymbol(variable);
        }
        
        if (member == null)
            return null;
            
        var equalityAttributes = new[] { "IncludeInEqualityAttribute", "CustomEqualityAttribute", "SequenceEqualityAttribute", "IgnoreEqualityAttribute" };
        
        foreach (var attribute in member.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (attributeName != null && equalityAttributes.Any(ea => attributeName.EndsWith(ea)))
            {
                var containingType = member.ContainingType;
                if (containingType != null)
                {
                    // Check if the containing class has [ValueObject] attribute
                    var hasValueObjectAttribute = containingType.GetAttributes()
                        .Any(a => a.AttributeClass?.ToDisplayString() == "DomainBase.ValueObjectAttribute");
                    
                    if (!hasValueObjectAttribute)
                    {
                        return new MemberWithEqualityAttribute
                        {
                            Member = member,
                            AttributeName = attribute.AttributeClass!.Name,
                            ContainingClass = containingType,
                            AttributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? member.Locations.FirstOrDefault() ?? Location.None
                        };
                    }
                }
            }
        }
        
        return null;
    }

    private static void ValidateEqualityAttributeUsage(SourceProductionContext context, MemberWithEqualityAttribute data)
    {
        var memberKind = data.Member is IPropertySymbol ? "property" : "field";
        
        context.ReportDiagnostic(Diagnostic.Create(
            EqualityAttributeOnNonValueObjectError,
            data.AttributeLocation,
            memberKind,
            data.Member.Name,
            data.AttributeName,
            data.ContainingClass.Name));
    }

    private class MemberInfo
    {
        public string Name { get; set; } = "";
        public ITypeSymbol Type { get; set; } = null!;
        public string MemberKind { get; set; } = "";
        public EqualityKind EqualityKind { get; set; }
        public int Priority { get; set; }
        public bool OrderMatters { get; set; }
        public bool DeepEquality { get; set; }
        public ISymbol Symbol { get; set; } = null!;
    }

    private enum EqualityKind
    {
        Include,
        Custom,
        Sequence
    }
}