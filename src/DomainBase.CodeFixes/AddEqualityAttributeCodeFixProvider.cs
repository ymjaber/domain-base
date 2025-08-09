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

namespace DomainBase.Generators.CodeFixes;

/// <summary>
/// Provides code fixes for missing equality attributes on value object members.
/// Offers fixes to add <c>[IncludeInEquality]</c>, <c>[IgnoreEquality]</c>, or <c>[SequenceEquality]</c>
/// depending on the member type.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddEqualityAttributeCodeFixProvider)), Shared]
public class AddEqualityAttributeCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DBVO002");

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
            
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();

        if (memberDeclaration == null)
            return;

        // Register multiple code fixes for different attribute options
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [IncludeInEquality] attribute",
                createChangedDocument: c => AddAttributeAsync(context.Document, memberDeclaration, "IncludeInEquality", c),
                equivalenceKey: "AddIncludeInEquality"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [IgnoreEquality] attribute",
                createChangedDocument: c => AddAttributeAsync(context.Document, memberDeclaration, "IgnoreEquality", c),
                equivalenceKey: "AddIgnoreEquality"),
            diagnostic);

        // Check if it's a collection type
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return;
            
        ITypeSymbol? typeSymbol = null;

        if (memberDeclaration is PropertyDeclarationSyntax property)
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
            typeSymbol = propertySymbol?.Type;
        }
        else if (memberDeclaration is FieldDeclarationSyntax field)
        {
            var variable = field.Declaration.Variables.FirstOrDefault();
            if (variable != null)
            {
                var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                typeSymbol = fieldSymbol?.Type;
            }
        }

        if (typeSymbol != null && ImplementsIEnumerable(typeSymbol))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add [SequenceEquality] attribute",
                    createChangedDocument: c => AddAttributeAsync(context.Document, memberDeclaration, "SequenceEquality", c),
                    equivalenceKey: "AddSequenceEquality"),
                diagnostic);
        }
    }

    private async Task<Document> AddAttributeAsync(Document document, MemberDeclarationSyntax memberDeclaration,
        string attributeName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));
        var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

        MemberDeclarationSyntax newMemberDeclaration;
        if (memberDeclaration is PropertyDeclarationSyntax property)
        {
            newMemberDeclaration = property.AddAttributeLists(attributeList);
        }
        else if (memberDeclaration is FieldDeclarationSyntax field)
        {
            newMemberDeclaration = field.AddAttributeLists(attributeList);
        }
        else
        {
            return document;
        }

        var newRoot = root.ReplaceNode(memberDeclaration, newMemberDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private bool ImplementsIEnumerable(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return false;

        var enumerableType = type.AllInterfaces.FirstOrDefault(i =>
            i.SpecialType == SpecialType.System_Collections_IEnumerable ||
            (i.IsGenericType && i.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T));

        return enumerableType != null;
    }
}