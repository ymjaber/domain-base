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
/// Analyzer for value objects marked with <c>[DomainBase.ValueObject]</c> and inheriting from <c>DomainBase.ValueObject&lt;TSelf&gt;</c>.
/// Mirrors diagnostics produced by the generator so they can run without the generator present and enable live analysis.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NonPartialClassError = new(
        id: "DBVO001",
        title: "ValueObject class must be partial",
        messageFormat: "The value object '{0}' must be declared as a partial class to enable source generation",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingEqualityAttributeWarning = new(
        id: "DBVO002",
        title: "Property or field missing equality attribute",
        messageFormat: "The {0} '{1}' in value object '{2}' should have an equality attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingCustomEqualsError = new(
        id: "DBVO003",
        title: "Missing custom equality method",
        messageFormat: "The property '{0}' has [CustomEquality] but is missing the required static method 'Equals_{1}' with signature: private static bool Equals_{1}(in T value, in T otherValue)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingCustomHashCodeError = new(
        id: "DBVO004",
        title: "Missing custom hash code method",
        messageFormat: "The property '{0}' has [CustomEquality] but is missing the required static method 'GetHashCode_{1}' with signature: private static int GetHashCode_{1}(in T value)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MultipleEqualityAttributesError = new(
        id: "DBVO005",
        title: "Multiple equality attributes on member",
        messageFormat: "The {0} '{1}' in value object '{2}' has multiple equality attributes; only one of [IncludeInEquality], [CustomEquality], [SequenceEquality], or [IgnoreEquality] is allowed",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor SequenceOnNonEnumerableError = new(
        id: "DBVO006",
        title: "SequenceEquality on non-sequence type",
        messageFormat: "The {0} '{1}' has [SequenceEquality] but does not implement IEnumerable/IEnumerator. Consider using [IncludeInEquality] instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EqualityAttributeOnNonValueObjectError = new(
        id: "DBVO007",
        title: "Equality attribute on non-ValueObject class",
        messageFormat: "The {0} '{1}' has equality attribute '{2}' but the containing class '{3}' does not have the [ValueObject] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ValueObjectAttributeWithoutInheritanceError = new(
        id: "DBVO008",
        title: "ValueObject attribute without inheritance",
        messageFormat: "The class '{0}' has [ValueObject] attribute but does not inherit from ValueObject<T>",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateMethodNamesError = new(
        id: "DBVO009",
        title: "Duplicate method names after cleaning",
        messageFormat: "The members '{0}' and '{1}' would generate the same method names after removing prefixes. Consider renaming one of them.",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateOrderWarning = new(
        id: "DBVO013",
        title: "Duplicate order on equality members",
        messageFormat: "The {0} '{1}' in value object '{2}' has the same Order value ({3}) as another member. Evaluation order will fall back to declaration order.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ValueObjectAttributeOnSimpleWrapperWarning = new(
            id: "DBVO014",
            title: "[ValueObject] attribute unnecessary on simple wrapper",
            messageFormat: "The class '{0}' inherits from ValueObject<TSelf, TValue>. Applying [ValueObject] is unnecessary and ignored.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EqualityAttributeOnInvalidMemberError = new(
            id: "DBVO015",
            title: "Equality attribute on unsupported member",
            messageFormat: "The equality attribute on '{0}' is invalid. Only fields and auto-properties are supported.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PropertyMustBeInitOnlyOrGetOnlyError = new(
        id: "DBVO010",
        title: "Mutable property in ValueObject",
        messageFormat: "The property '{0}' in value object '{1}' must be get-only or init-only to enforce immutability",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FieldMustBeReadonlyError = new(
        id: "DBVO011",
        title: "Mutable field in ValueObject",
        messageFormat: "The field '{0}' in value object '{1}' must be declared readonly to enforce immutability",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AdditionalMemberInSimpleValueObjectWarning = new(
        id: "DBVO012",
        title: "Additional members in simple ValueObject",
        messageFormat: "The simple value object '{0}' should not declare additional {1} '{2}'. Only the 'Value' member is allowed.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// The set of diagnostics that this analyzer can produce.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        NonPartialClassError,
        MissingEqualityAttributeWarning,
        MissingCustomEqualsError,
        MissingCustomHashCodeError,
        MultipleEqualityAttributesError,
        SequenceOnNonEnumerableError,
        EqualityAttributeOnNonValueObjectError,
        ValueObjectAttributeWithoutInheritanceError,
        DuplicateMethodNamesError,
        PropertyMustBeInitOnlyOrGetOnlyError,
        FieldMustBeReadonlyError,
        AdditionalMemberInSimpleValueObjectWarning,
        DuplicateOrderWarning,
        ValueObjectAttributeOnSimpleWrapperWarning,
        EqualityAttributeOnInvalidMemberError
    );

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAttributesOnNonValueObjects, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
            return;

        var hasValueObjectAttribute = HasAttribute(typeSymbol, "DomainBase.ValueObjectAttribute");
        var inheritsValueObject = InheritsFromValueObject(typeSymbol);

        if (hasValueObjectAttribute && !inheritsValueObject)
        {
            var attributeLocation = GetAttributeLocation(typeSymbol, "DomainBase.ValueObjectAttribute") ?? GetTypeLocation(typeSymbol);
            context.ReportDiagnostic(Diagnostic.Create(ValueObjectAttributeWithoutInheritanceError, attributeLocation, typeSymbol.Name));
        }

        // [ValueObject] on simple wrapper ValueObject<TSelf, TValue> is unnecessary
        if (hasValueObjectAttribute && InheritsFromSimpleValueObject(typeSymbol))
        {
            var attributeLocation = GetAttributeLocation(typeSymbol, "DomainBase.ValueObjectAttribute") ?? GetTypeLocation(typeSymbol);
            context.ReportDiagnostic(Diagnostic.Create(ValueObjectAttributeOnSimpleWrapperWarning, attributeLocation, typeSymbol.Name));
        }

        // Immutability should be enforced for any class inheriting from ValueObject<T>
        if (inheritsValueObject)
        {
            AnalyzeImmutability(context, typeSymbol);
        }

        // Additional members should not be declared for simple value objects
        if (InheritsFromSimpleValueObject(typeSymbol))
        {
            AnalyzeSimpleValueObjectAdditionalMembers(context, typeSymbol);
        }

        if (hasValueObjectAttribute && inheritsValueObject)
        {
            // Check partial modifier on all declarations
            foreach (var decl in typeSymbol.DeclaringSyntaxReferences)
            {
                if (decl.GetSyntax() is ClassDeclarationSyntax cds)
                {
                    if (!cds.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        var location = cds.Identifier.GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, location, typeSymbol.Name));
                    }
                }
            }

            AnalyzeValueObjectMembers(context, typeSymbol);
        }
    }

    private static void AnalyzeImmutability(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property && IsAutoProperty(property))
            {
                if (property.SetMethod != null && !property.SetMethod.IsInitOnly)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        PropertyMustBeInitOnlyOrGetOnlyError,
                        property.Locations.FirstOrDefault() ?? Location.None,
                        property.Name,
                        typeSymbol.Name));
                }
            }
            else if (member is IFieldSymbol field && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared)
            {
                if (field.AssociatedSymbol is IPropertySymbol)
                    continue; // skip backing fields

                if (!field.IsReadOnly)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FieldMustBeReadonlyError,
                        field.Locations.FirstOrDefault() ?? Location.None,
                        field.Name,
                        typeSymbol.Name));
                }
            }
        }
    }

    private static void AnalyzeSimpleValueObjectAdditionalMembers(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property && !property.IsStatic && !property.IsImplicitlyDeclared)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AdditionalMemberInSimpleValueObjectWarning,
                    property.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.Name,
                    "property",
                    property.Name));
            }
            else if (member is IFieldSymbol field && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared)
            {
                if (field.AssociatedSymbol is IPropertySymbol)
                    continue; // skip backing fields

                context.ReportDiagnostic(Diagnostic.Create(
                    AdditionalMemberInSimpleValueObjectWarning,
                    field.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.Name,
                    "field",
                    field.Name));
            }
        }
    }

    private static void AnalyzeValueObjectMembers(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol)
    {
        // Collect member info and perform checks
        var members = new List<(ISymbol Member, ITypeSymbol Type, string Name, string Kind, AttributeData? Include, AttributeData? Custom, AttributeData? Sequence, AttributeData? Ignore)>();

        // First pass: report equality attributes on invalid members (non-field or non-auto property)
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                var (include, custom, sequence, ignore) = GetEqualityAttributes(property);
                if (include != null || custom != null || sequence != null || ignore != null)
                {
                    if (!IsAutoPropertySyntax(property))
                    {
                        var location = property.Locations.FirstOrDefault() ?? Location.None;
                        context.ReportDiagnostic(Diagnostic.Create(
                            EqualityAttributeOnInvalidMemberError,
                            location,
                            property.Name));
                    }
                }
            }
        }

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property && IsAutoProperty(property))
            {
                var (include, custom, sequence, ignore) = GetEqualityAttributes(property);
                members.Add((property, property.Type, property.Name, "property", include, custom, sequence, ignore));
            }
            else if (member is IFieldSymbol field && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared)
            {
                if (field.AssociatedSymbol is IPropertySymbol)
                    continue; // skip backing fields

                var (include, custom, sequence, ignore) = GetEqualityAttributes(field);
                members.Add((field, field.Type, field.Name, "field", include, custom, sequence, ignore));
            }
        }

        // Missing or multiple attributes; custom method checks; sequence checks
        var customMembers = new List<(ISymbol Symbol, string Name)>();
        var cleanedNameToOriginal = new Dictionary<string, string>(StringComparer.Ordinal);

        // Track orders to report duplicates
        var explicitOrders = new Dictionary<int, (ISymbol Symbol, string Kind, string Name)>(capacity: members.Count);

        foreach (var m in members)
        {
            var attributeCount = new[] { m.Include, m.Custom, m.Sequence, m.Ignore }.Count(a => a != null);

            if (attributeCount == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingEqualityAttributeWarning,
                    m.Member.Locations.FirstOrDefault() ?? Location.None,
                    m.Kind,
                    m.Name,
                    typeSymbol.Name));
                continue;
            }

            if (attributeCount > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MultipleEqualityAttributesError,
                    m.Member.Locations.FirstOrDefault() ?? Location.None,
                    m.Kind,
                    m.Name,
                    typeSymbol.Name));
                continue;
            }

            if (m.Ignore != null)
                continue; // explicitly ignored

            if (m.Custom != null)
            {
                customMembers.Add((m.Member, m.Name));

                var suffix = GetCleanMethodName(m.Name);

                var equalsMethod = typeSymbol.GetMembers($"Equals_{suffix}").OfType<IMethodSymbol>().FirstOrDefault(s => s.IsStatic);
                var hashCodeMethod = typeSymbol.GetMembers($"GetHashCode_{suffix}").OfType<IMethodSymbol>().FirstOrDefault(s => s.IsStatic);

                if (equalsMethod == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingCustomEqualsError,
                        m.Member.Locations.FirstOrDefault() ?? Location.None,
                        m.Name,
                        suffix));
                }

                if (hashCodeMethod == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingCustomHashCodeError,
                        m.Member.Locations.FirstOrDefault() ?? Location.None,
                        m.Name,
                        suffix));
                }

                // Duplicate clean method names
                if (cleanedNameToOriginal.TryGetValue(suffix, out var existing))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DuplicateMethodNamesError,
                        m.Member.Locations.FirstOrDefault() ?? Location.None,
                        existing,
                        m.Name));
                }
                else
                {
                    cleanedNameToOriginal[suffix] = m.Name;
                }
            }
            else if (m.Sequence != null)
            {
                if (!IsEnumerableOrEnumerator(m.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SequenceOnNonEnumerableError,
                        m.Member.Locations.FirstOrDefault() ?? Location.None,
                        m.Kind,
                        m.Name));
                }
            }

            // Duplicate order detection (Include/Custom/Sequence only)
            var attr = m.Include ?? m.Custom ?? m.Sequence;
            if (attr != null)
            {
                var (order, hasExplicit) = GetOrderFromAttribute(attr);
                if (hasExplicit)
                {
                    if (explicitOrders.TryGetValue(order, out var existing))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DuplicateOrderWarning,
                            m.Member.Locations.FirstOrDefault() ?? Location.None,
                            m.Kind,
                            m.Name,
                            typeSymbol.Name,
                            order));
                    }
                    else
                    {
                        explicitOrders[order] = (m.Member, m.Kind, m.Name);
                    }
                }
            }
        }
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

    private static void AnalyzeMemberAttributesOnNonValueObjects(SyntaxNodeAnalysisContext context)
    {
        // Report DBVO007 for any member with equality attribute in a class without [ValueObject]
        ISymbol? memberSymbol = null;
        SyntaxList<AttributeListSyntax> attributeLists;

        if (context.Node is PropertyDeclarationSyntax property)
        {
            memberSymbol = context.SemanticModel.GetDeclaredSymbol(property);
            attributeLists = property.AttributeLists;
        }
        else if (context.Node is FieldDeclarationSyntax field)
        {
            var variable = field.Declaration.Variables.FirstOrDefault();
            memberSymbol = variable != null ? context.SemanticModel.GetDeclaredSymbol(variable) : null;
            attributeLists = field.AttributeLists;
        }
        else
        {
            return;
        }

        if (memberSymbol == null)
            return;

        var containingType = memberSymbol.ContainingType;
        if (containingType == null)
            return;

        var equalityAttributes = new[] { "IncludeInEqualityAttribute", "CustomEqualityAttribute", "SequenceEqualityAttribute", "IgnoreEqualityAttribute" };

        foreach (var list in attributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                var attrSymbol = context.SemanticModel.GetSymbolInfo(attr).Symbol as IMethodSymbol;
                var attrType = attrSymbol?.ContainingType;
                var attrName = attrType?.Name;
                if (attrName != null && equalityAttributes.Any(n => string.Equals(n, attrName, StringComparison.Ordinal)))
                {
                    var hasValueObjectAttribute = HasAttribute(containingType, "DomainBase.ValueObjectAttribute");
                    if (!hasValueObjectAttribute)
                    {
                        var memberKind = memberSymbol is IPropertySymbol ? "property" : "field";
                        var attrLocation = attr.GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(
                            EqualityAttributeOnNonValueObjectError,
                            attrLocation,
                            memberKind,
                            memberSymbol.Name,
                            attrType!.Name,
                            containingType.Name));
                    }
                }
            }
        }
    }

    private static (AttributeData? Include, AttributeData? Custom, AttributeData? Sequence, AttributeData? Ignore) GetEqualityAttributes(ISymbol symbol)
    {
        AttributeData? include = null, custom = null, sequence = null, ignore = null;
        foreach (var a in symbol.GetAttributes())
        {
            var name = a.AttributeClass?.Name;
            if (name == null) continue;
            if (name == "IncludeInEqualityAttribute") include = a;
            else if (name == "CustomEqualityAttribute") custom = a;
            else if (name == "SequenceEqualityAttribute") sequence = a;
            else if (name == "IgnoreEqualityAttribute") ignore = a;
        }

        return (include, custom, sequence, ignore);
    }

    private static bool HasAttribute(INamedTypeSymbol type, string fullyQualifiedAttributeName)
        => type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);

    private static bool InheritsFromValueObject(INamedTypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.ConstructedFrom.ToDisplayString() == "DomainBase.ValueObject<TSelf>")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool InheritsFromSimpleValueObject(INamedTypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType &&
                baseType.Name == "ValueObject" &&
                baseType.ContainingNamespace?.ToDisplayString() == "DomainBase" &&
                baseType.TypeArguments.Length == 2)
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool IsEnumerableOrEnumerator(ITypeSymbol type)
    {
        // Strings are enumerable but should not be considered sequences for this rule
        if (type.SpecialType == SpecialType.System_String)
            return false;

        // Direct interface type checks
        if (type.SpecialType == SpecialType.System_Collections_IEnumerable ||
            type.SpecialType == SpecialType.System_Collections_IEnumerator)
            return true;

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var constructed = namedType.ConstructedFrom.SpecialType;
            if (constructed == SpecialType.System_Collections_Generic_IEnumerable_T ||
                constructed == SpecialType.System_Collections_Generic_IEnumerator_T)
                return true;
        }

        // Implemented interfaces
        foreach (var i in type.AllInterfaces)
        {
            if (i.SpecialType == SpecialType.System_Collections_IEnumerable ||
                i.SpecialType == SpecialType.System_Collections_IEnumerator)
                return true;

            if (i.IsGenericType)
            {
                var constructedFrom = i.ConstructedFrom.SpecialType;
                if (constructedFrom == SpecialType.System_Collections_Generic_IEnumerable_T ||
                    constructedFrom == SpecialType.System_Collections_Generic_IEnumerator_T)
                    return true;
            }
        }

        return false;
    }

    private static bool IsAutoProperty(IPropertySymbol property)
    {
        if (property.IsStatic || property.IsIndexer || property.IsAbstract || property.IsWriteOnly || property.GetMethod is null)
            return false;

        return IsAutoPropertySyntax(property);
    }

    private static bool IsAutoPropertySyntax(IPropertySymbol property)
    {
        foreach (var declRef in property.DeclaringSyntaxReferences)
        {
            if (declRef.GetSyntax() is PropertyDeclarationSyntax p)
            {
                // Expression-bodied properties are not auto-properties
                if (p.ExpressionBody != null)
                    return false;

                if (p.AccessorList == null)
                    return false;

                // Auto-property accessors have no bodies
                if (p.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null))
                    return true;

                return false;
            }
        }
        return false;
    }

    private static string GetCleanName(string name)
    {
        if (name.StartsWith("_")) return name.Substring(1);
        if (name.StartsWith("m_")) return name.Substring(2);
        return name;
    }

    private static string GetCleanMethodName(string name)
    {
        var clean = GetCleanName(name);
        if (string.IsNullOrEmpty(clean) || char.IsDigit(clean[0]))
            return name; // fall back to original
        return char.ToUpper(clean[0]) + (clean.Length > 1 ? clean.Substring(1) : "");
    }

    private static Location GetTypeLocation(INamedTypeSymbol type)
        => type.Locations.FirstOrDefault() ?? Location.None;

    private static Location? GetAttributeLocation(INamedTypeSymbol type, string fullyQualifiedAttributeName)
    {
        foreach (var a in type.GetAttributes())
        {
            if (a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
            {
                return a.ApplicationSyntaxReference?.GetSyntax().GetLocation();
            }
        }
        return null;
    }
}

