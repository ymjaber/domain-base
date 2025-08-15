using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace DomainBase.Generators.CodeFixes;

/// <summary>
/// Provides a code fix that generates the required static custom equality and hash code methods
/// for members annotated with <c>[CustomEquality]</c> when they are missing.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenerateCustomEqualityMethodsCodeFixProvider)), Shared]
public class GenerateCustomEqualityMethodsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => 
        ImmutableArray.Create("DBVO003", "DBVO004");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;
            
        // Track processed properties to avoid duplicate code fixes
        var processedProperties = new HashSet<string>();
        
        foreach (var diagnostic in context.Diagnostics)
        {
            var propertyName = ExtractPropertyNameFromMessage(diagnostic.GetMessage());
            if (string.IsNullOrEmpty(propertyName))
                continue;
                
            // Skip if we've already registered a fix for this property
            if (!processedProperties.Add(propertyName!))
                continue;
                
            var classDeclaration = root.FindNode(diagnostic.Location.SourceSpan)
                .AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (classDeclaration == null)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Generate custom equality methods",
                    createChangedDocument: c => GenerateCustomMethodsAsync(context.Document, classDeclaration, propertyName!, c),
                    equivalenceKey: $"GenerateCustomEqualityMethods_{propertyName}"),
                diagnostic);
        }
    }

    private string? ExtractPropertyNameFromMessage(string message)
    {
        // Message format: "The property 'Name' has [CustomEquality]..."
        var startIndex = message.IndexOf('\'');
        if (startIndex == -1) return null;
        
        var endIndex = message.IndexOf('\'', startIndex + 1);
        if (endIndex == -1) return null;
        
        return message.Substring(startIndex + 1, endIndex - startIndex - 1);
    }

    private async Task<Document> GenerateCustomMethodsAsync(Document document, ClassDeclarationSyntax classDeclaration, 
        string propertyName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;
            
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;
        
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol == null)
            return document;
            
        // Find the property or field by name
        var property = classSymbol.GetMembers(propertyName).FirstOrDefault();
        
        if (property == null)
            return document;

        var propertyType = property switch
        {
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            _ => null
        };

        if (propertyType == null)
            return document;

        var typeName = propertyType.ToDisplayString();

        // Check which methods are missing - look for static methods
        var cleanMethodName = GetCleanMethodName(propertyName);
        var hasEqualsMethod = classSymbol.GetMembers($"Equals_{cleanMethodName}")
            .Any(m => m is IMethodSymbol method && method.IsStatic);
        var hasHashCodeMethod = classSymbol.GetMembers($"GetHashCode_{cleanMethodName}")
            .Any(m => m is IMethodSymbol method && method.IsStatic);

        var newClassDeclaration = classDeclaration;
        var methodsToAdd = new List<MemberDeclarationSyntax>();

        if (!hasEqualsMethod)
        {
            var equalsMethod = GenerateCustomEqualsMethod(propertyName, cleanMethodName, typeName, propertyType);
            methodsToAdd.Add(equalsMethod);
        }

        if (!hasHashCodeMethod)
        {
            var hashCodeMethod = GenerateCustomHashCodeMethod(propertyName, cleanMethodName, typeName, propertyType);
            methodsToAdd.Add(hashCodeMethod);
        }

        // Only update if we have methods to add
        if (methodsToAdd.Count == 0)
            return document;

        newClassDeclaration = newClassDeclaration.AddMembers(methodsToAdd.ToArray());
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private MethodDeclarationSyntax GenerateCustomEqualsMethod(string propertyName, string cleanMethodName, string typeName, ITypeSymbol propertyType)
    {
        // Generate parameter names based on property name
        var cleanName = GetCleanName(propertyName);
        var paramName = GetSafeParameterName(cleanName.Length > 0 ? cleanName : propertyName);
        var capitalizedCleanName = string.IsNullOrEmpty(cleanName) 
            ? propertyName 
            : char.ToUpper(cleanName[0]) + (cleanName.Length > 1 ? cleanName.Substring(1) : "");
        var otherParamName = GetSafeParameterName($"other{capitalizedCleanName}");
        
        // For EqualityComparer, we need the non-nullable type
        var equalityComparerType = propertyType.IsValueType 
            ? typeName.TrimEnd('?')  // Remove ? for nullable value types
            : typeName.TrimEnd('?'); // Also trim for nullable reference types
            
        var methodBody = SyntaxFactory.Block(
            SyntaxFactory.ParseStatement($"return EqualityComparer<{equalityComparerType}>.Default.Equals({paramName}, {otherParamName});")
                .WithAdditionalAnnotations(Formatter.Annotation));

        // Use 'in' for value types (structs) to prevent copying
        var paramModifiers = propertyType.IsValueType 
            ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword))
            : SyntaxFactory.TokenList();

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                SyntaxFactory.Identifier($"Equals_{cleanMethodName}"))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                        .WithType(SyntaxFactory.ParseTypeName(typeName))
                        .WithModifiers(paramModifiers),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(otherParamName))
                        .WithType(SyntaxFactory.ParseTypeName(typeName))
                        .WithModifiers(paramModifiers)
                })))
            .WithBody(methodBody)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private MethodDeclarationSyntax GenerateCustomHashCodeMethod(string propertyName, string cleanMethodName, string typeName, ITypeSymbol propertyType)
    {
        // Generate parameter name based on property name
        var cleanName = GetCleanName(propertyName);
        var paramName = GetSafeParameterName(cleanName.Length > 0 ? cleanName : propertyName);
        
        var methodBody = SyntaxFactory.Block(
            SyntaxFactory.ParseStatement($"return System.Collections.Generic.EqualityComparer<{typeName.TrimEnd('?')}>.Default.GetHashCode({paramName});")
                .WithAdditionalAnnotations(Formatter.Annotation));

        // Use 'in' for value types (structs) to prevent copying
        var paramModifiers = propertyType.IsValueType 
            ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword))
            : SyntaxFactory.TokenList();

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                SyntaxFactory.Identifier($"GetHashCode_{cleanMethodName}"))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                        .WithType(SyntaxFactory.ParseTypeName(typeName))
                        .WithModifiers(paramModifiers)
                })))
            .WithBody(methodBody)
            .WithAdditionalAnnotations(Formatter.Annotation);
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
}