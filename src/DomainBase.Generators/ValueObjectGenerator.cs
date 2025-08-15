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

/// <summary>
/// Source generator that augments value object classes marked with <c>[DomainBase.ValueObject]</c> and inheriting
/// from <c>DomainBase.ValueObject&lt;TSelf&gt;</c> with optimized equality and hash code implementations, sequence helpers,
/// and diagnostics for missing/invalid equality attributes and conventions.
/// </summary>
[Generator]
public class ValueObjectGenerator : IIncrementalGenerator
{
    // No diagnostics here; authoring rules are enforced by analyzers

    

    

    

    

    

    

    
        
    

    

    

    /// <summary>
    /// Configures the incremental generation and validation pipelines.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
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

    /// <summary>
    /// Determines whether the syntax node is a candidate for generation (class with attributes and a base list).
    /// </summary>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0 && c.BaseList != null;

    /// <summary>
    /// Verifies the semantic target has <c>[ValueObject]</c> and inherits from <c>ValueObject&lt;TSelf&gt;</c>.
    /// </summary>
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

    /// <summary>
    /// Emits generated members and diagnostics for the discovered value object classes.
    /// </summary>
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

            // Analyzer handles partial class validation
            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                continue;

            var members = AnalyzeMembers(classSymbol);
            if (members == null)
                continue;
                
            var typeConverterAttr = classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "DomainBase.GenerateTypeConverterAttribute");
            var generateTypeConverter = typeConverterAttr != null;
            var source = GenerateValueObjectExtensions(classSymbol, members, generateTypeConverter);
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static List<MemberInfo>? AnalyzeMembers(INamedTypeSymbol classSymbol)
    {
        var members = new List<MemberInfo>();
        
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IPropertySymbol property && IsAutoProperty(property))
            {
                var memberInfo = AnalyzeMember(property, property.Type, property.Name, "property", classSymbol);
                if (memberInfo != null)
                {
                    memberInfo.DeclarationIndex = GetDeclarationIndex(member);
                    members.Add(memberInfo);
                }
            }
            else if (member is IFieldSymbol field && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared)
            {
                // Skip backing fields for properties
                if (field.AssociatedSymbol is IPropertySymbol)
                    continue;

                var memberInfo = AnalyzeMember(field, field.Type, field.Name, "field", classSymbol);
                if (memberInfo != null)
                {
                    memberInfo.DeclarationIndex = GetDeclarationIndex(member);
                    members.Add(memberInfo);
                }
            }
            // no-op for other members
        }

        // Sort ALL members by Order (ascending; default 0 for unspecified), then declaration order
        members.Sort((a, b) =>
        {
            var orderCompare = a.Order.CompareTo(b.Order);
            return orderCompare != 0 ? orderCompare : a.DeclarationIndex.CompareTo(b.DeclarationIndex);
        });
        
        // Check for duplicate method names after cleaning (only for custom equality)
        var customMembers = members.Where(m => m.EqualityKind == EqualityKind.Custom).ToList();
        var methodNameMap = new Dictionary<string, MemberInfo>();
        
        foreach (var member in customMembers)
        {
            var cleanMethodName = GetCleanMethodName(member.Name);
            if (methodNameMap.TryGetValue(cleanMethodName, out var existingMember))
            {
                // Analyzer reports duplicate custom method names; skip generation if conflict
                return null;
            }
            methodNameMap[cleanMethodName] = member;
        }
        
        return members;
    }

    private static bool IsAutoProperty(IPropertySymbol property)
    {
        if (property.GetMethod is null || property.IsStatic || property.IsIndexer || property.IsAbstract || property.IsWriteOnly)
            return false;

        // Validate syntax: must be auto-property (no accessor bodies, no expression body)
        foreach (var declRef in property.DeclaringSyntaxReferences)
        {
            if (declRef.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax p)
            {
                if (p.ExpressionBody != null)
                    return false;
                if (p.AccessorList == null)
                    return false;
                if (p.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null))
                    return true;
                return false;
            }
        }
        return false;
    }

    private static MemberInfo? AnalyzeMember(ISymbol member, ITypeSymbol type, string name, string memberKind, 
        INamedTypeSymbol classSymbol)
    {
        var attributes = member.GetAttributes();
        
        var includeAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "IncludeInEqualityAttribute");
        var customAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "CustomEqualityAttribute");
        var sequenceAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "SequenceEqualityAttribute");
        var ignoreAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "IgnoreEqualityAttribute");
        
        var attributeCount = new[] { includeAttr, customAttr, sequenceAttr, ignoreAttr }.Count(a => a != null);
        
        if (attributeCount == 0)
            return null;
        
        if (attributeCount > 1)
            return null;
        
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
            (memberInfo.Order, memberInfo.HasExplicitOrder) = GetOrderFromAttribute(includeAttr);
        }
        else if (customAttr != null)
        {
            memberInfo.EqualityKind = EqualityKind.Custom;
            (memberInfo.Order, memberInfo.HasExplicitOrder) = GetOrderFromAttribute(customAttr);
            
            // Check for static custom methods
            var methodSuffix = GetCleanMethodName(name);
            var equalsMethod = classSymbol.GetMembers($"Equals_{methodSuffix}")
                .FirstOrDefault(m => m is IMethodSymbol method && method.IsStatic);
            var hashCodeMethod = classSymbol.GetMembers($"GetHashCode_{methodSuffix}")
                .FirstOrDefault(m => m is IMethodSymbol method && method.IsStatic);
            
            // Analyzer reports missing methods; generator will still emit calls
        }
        else if (sequenceAttr != null)
        {
            memberInfo.EqualityKind = EqualityKind.Sequence;
            (memberInfo.Order, memberInfo.HasExplicitOrder) = GetOrderFromAttribute(sequenceAttr);
            memberInfo.OrderMatters = GetAttributeValue(sequenceAttr, "OrderMatters", true);
            memberInfo.DeepEquality = GetAttributeValue(sequenceAttr, "DeepEquality", true);
            
            // Analyzer validates sequence applicability
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

    private static (int Order, bool HasExplicitOrder) GetOrderFromAttribute(AttributeData attribute)
    {
        // Prefer named Order
        var orderNamed = attribute.NamedArguments.FirstOrDefault(na => na.Key == "Order");
        if (orderNamed.Value.Value is int orderVal)
            return (orderVal, true);

        // Constructor argument
        if (attribute.ConstructorArguments.Length > 0)
        {
            var arg = attribute.ConstructorArguments[0];
            if (arg.Value is int ctorVal)
                return (ctorVal, true);
        }

        return (0, false);
    }

    private static int GetDeclarationIndex(ISymbol symbol)
    {
        try
        {
            var loc = symbol.Locations.FirstOrDefault();
            if (loc != null && loc.IsInSource)
            {
                return loc.SourceSpan.Start;
            }
        }
        catch
        {
            // ignore and fall through
        }
        return int.MaxValue;
    }

    private static string GenerateValueObjectExtensions(INamedTypeSymbol classSymbol, List<MemberInfo> members, bool generateTypeConverter)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var isGlobalNamespace = classSymbol.ContainingNamespace.IsGlobalNamespace;
        
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        if (generateTypeConverter)
        {
            sb.AppendLine("using System.ComponentModel;");
        }
        sb.AppendLine();
        
        if (!isGlobalNamespace)
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }
        
        var indent = isGlobalNamespace ? "" : "    ";
        if (generateTypeConverter)
        {
            sb.AppendLine($"{indent}[TypeConverter(typeof({className}TypeConverter))]");
        }
        sb.AppendLine($"{indent}partial class {className}");
        sb.AppendLine($"{indent}{{");
        
        GenerateEqualsCore(sb, className, members, indent);
        GenerateGetHashCodeCore(sb, members, indent);
        GenerateCustomPartialMethods(sb, members, indent);
        
        if (members.Any(m => m.EqualityKind == EqualityKind.Sequence))
        {
            GenerateSequenceEqualityHelper(sb, indent);
            GenerateSequenceHashCodeHelper(sb, indent);
            GenerateReferenceEqualityComparerHelper(sb, indent);
        }
        
        sb.AppendLine($"{indent}}}");

        if (generateTypeConverter)
        {
            GenerateVoTypeConverter(sb, namespaceName, className, indent);
        }

        if (!isGlobalNamespace)
        {
            sb.AppendLine("}");
        }
        
        return sb.ToString();
    }

    

    private static void GenerateVoTypeConverter(StringBuilder sb, string namespaceName, string className, string indent)
    {
        sb.AppendLine();
        sb.AppendLine($"{indent}public class {className}TypeConverter : System.ComponentModel.TypeConverter");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, Type sourceType)");
        sb.AppendLine($"{indent}        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);");
        sb.AppendLine();
        sb.AppendLine($"{indent}    public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (value is string s)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            return System.Activator.CreateInstance(typeof({className}), s);");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        return base.ConvertFrom(context, culture, value);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateEqualsCore(StringBuilder sb, string className, List<MemberInfo> members, string indent)
    {
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// Determines whether this {className} is equal to another based on their values.");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    protected override bool EqualsCore({className} other)");
        sb.AppendLine($"{indent}    {{");
        
        // Add ordering documentation (only for explicitly ordered members)
        var orderGroups = members
            .GroupBy(m => m.Order)
            .OrderBy(g => g.Key)
            .ToList();
        if (orderGroups.Count > 0)
        {
            sb.AppendLine($"{indent}        // Members are compared in evaluation order (lowest to highest). Unspecified Order defaults to 0:");
            foreach (var group in orderGroups)
            {
                var memberNames = string.Join(", ", group.OrderBy(m => m.DeclarationIndex).Select(m => m.Name));
                sb.AppendLine($"{indent}        // Order {group.Key}: {memberNames}");
            }
            sb.AppendLine();
        }
        
        // Custom equality helpers now return bool directly
        
        foreach (var member in members)
        {
            switch (member.EqualityKind)
            {
                case EqualityKind.Include:
                    GenerateSimpleEquality(sb, member, indent);
                    break;
                case EqualityKind.Custom:
                    GenerateCustomEquality(sb, member, indent);
                    break;
                case EqualityKind.Sequence:
                    GenerateSequenceEquality(sb, member, indent);
                    break;
            }
        }
        
        sb.AppendLine($"{indent}        return true;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
    }

    private static void GenerateSimpleEquality(StringBuilder sb, MemberInfo member, string indent)
    {
        var memberAccess = member.Name;
        var otherAccess = $"other.{member.Name}";
        
        if (member.Type.IsValueType)
        {
            sb.AppendLine($"{indent}        if (!{memberAccess}.Equals({otherAccess})) return false;");
        }
        else
        {
            sb.AppendLine($"{indent}        if (!EqualityComparer<{member.Type.ToDisplayString()}>.Default.Equals({memberAccess}, {otherAccess})) return false;");
        }
    }

    private static void GenerateCustomEquality(StringBuilder sb, MemberInfo member, string indent)
    {
        var methodSuffix = GetCleanMethodName(member.Name);
        // Add order comment if it's non-zero
        if (member.Order != 0)
        {
            sb.AppendLine($"{indent}        // Order {member.Order}: {member.Name}");
        }
        sb.AppendLine($"{indent}        if (!Equals_{methodSuffix}({member.Name}, other.{member.Name})) return false;");
    }

    private static void GenerateSequenceEquality(StringBuilder sb, MemberInfo member, string indent)
    {
        var orderMatters = member.OrderMatters ? "true" : "false";
        var deepEquality = member.DeepEquality ? "true" : "false";
        sb.AppendLine($"{indent}        if (!SequenceEquals({member.Name}, other.{member.Name}, {orderMatters}, {deepEquality})) return false;");
    }

    private static void GenerateGetHashCodeCore(StringBuilder sb, List<MemberInfo> members, string indent)
    {
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// Returns a hash code for this value object's values.");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    protected override int GetHashCodeCore()");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        var hashCode = new HashCode();");
        sb.AppendLine();
        
        foreach (var member in members)
        {
            if (member.EqualityKind == EqualityKind.Custom)
            {
                var methodSuffix = GetCleanMethodName(member.Name);
                sb.AppendLine($"{indent}        hashCode.Add(GetHashCode_{methodSuffix}({member.Name}));");
            }
            else if (member.EqualityKind == EqualityKind.Sequence)
            {
                var orderMatters = member.OrderMatters ? "true" : "false";
                var deepEquality = member.DeepEquality ? "true" : "false";
                sb.AppendLine($"{indent}        AddSequenceHashCode({member.Name}, ref hashCode, {orderMatters}, {deepEquality});");
            }
            else
            {
                sb.AppendLine($"{indent}        hashCode.Add({member.Name});");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine($"{indent}        return hashCode.ToHashCode();");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
    }

    private static void GenerateCustomPartialMethods(StringBuilder sb, List<MemberInfo> members, string indent)
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

    private static void GenerateSequenceEqualityHelper(StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}    private static bool SequenceEquals<T>(IEnumerable<T>? first, IEnumerable<T>? second, bool orderMatters, bool deepEquality)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (ReferenceEquals(first, second)) return true;");
        sb.AppendLine($"{indent}        if (first is null || second is null) return false;");
        sb.AppendLine();
        sb.AppendLine($"{indent}        var comparer = (!deepEquality && !typeof(T).IsValueType) ? (IEqualityComparer<T>)ReferenceEqualityComparerAdapter<T>.Instance : EqualityComparer<T>.Default;");
        sb.AppendLine($"{indent}        if (orderMatters)");
        sb.AppendLine($"{indent}            return first.SequenceEqual(second, comparer);");
        sb.AppendLine();
        sb.AppendLine($"{indent}        var lookup1 = first.ToLookup(x => x, comparer);");
        sb.AppendLine($"{indent}        var lookup2 = second.ToLookup(x => x, comparer);");
        sb.AppendLine($"{indent}        if (lookup1.Count != lookup2.Count) return false;");
        sb.AppendLine($"{indent}        foreach (var group in lookup1)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            if (!lookup2.Contains(group.Key)) return false;");
        sb.AppendLine($"{indent}            if (group.Count() != lookup2[group.Key].Count()) return false;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        return true;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
    }

    private static void GenerateSequenceHashCodeHelper(StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}    private static void AddSequenceHashCode<T>(IEnumerable<T>? source, ref HashCode hashCode, bool orderMatters, bool deepEquality)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (source is null) return;");
        sb.AppendLine($"{indent}        if (orderMatters)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            foreach (var item in source) hashCode.Add(item); return;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        var comparer = (!deepEquality && !typeof(T).IsValueType) ? (IEqualityComparer<T>)ReferenceEqualityComparerAdapter<T>.Instance : EqualityComparer<T>.Default;");
        sb.AppendLine($"{indent}        var lookup = source.ToLookup(x => x, comparer);");
        sb.AppendLine($"{indent}        foreach (var group in lookup.OrderBy(g => (g.Key is null) ? 0 : comparer.GetHashCode(g.Key)))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            hashCode.Add(group.Count());");
        sb.AppendLine($"{indent}            hashCode.Add(group.Key, comparer);");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
    }

    private static void GenerateReferenceEqualityComparerHelper(StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}    private sealed class ReferenceEqualityComparerAdapter<T> : IEqualityComparer<T>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        public static readonly ReferenceEqualityComparerAdapter<T> Instance = new();");
        sb.AppendLine($"{indent}        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);");
        sb.AppendLine($"{indent}        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj!);");
        sb.AppendLine($"{indent}    }}");
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
            
            // Analyzer reports [ValueObject] without proper inheritance
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
        // Analyzer reports equality attributes on non-ValueObject classes
    }

    private class MemberInfo
    {
        public string Name { get; set; } = "";
        public ITypeSymbol Type { get; set; } = null!;
        public string MemberKind { get; set; } = "";
        public EqualityKind EqualityKind { get; set; }
        public int Order { get; set; }
        public bool HasExplicitOrder { get; set; }
        public bool OrderMatters { get; set; }
        public bool DeepEquality { get; set; }
        public ISymbol Symbol { get; set; } = null!;
        public int DeclarationIndex { get; set; }
    }

    private enum EqualityKind
    {
        Include,
        Custom,
        Sequence
    }
}