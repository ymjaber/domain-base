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
/// Provides a code fix to replace <c>[SequenceEquality]</c> with <c>[IncludeInEquality]</c>
/// when applied to non-sequence members.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReplaceSequenceEqualityCodeFixProvider)), Shared]
public class ReplaceSequenceEqualityCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DBVO006");

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

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with [IncludeInEquality]",
                createChangedDocument: c => ReplaceAttributeAsync(context.Document, memberDeclaration, c),
                equivalenceKey: "ReplaceWithIncludeInEquality"),
            diagnostic);
    }

    private async Task<Document> ReplaceAttributeAsync(Document document, MemberDeclarationSyntax memberDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        AttributeListSyntax? sequenceAttributeList = null;
        AttributeSyntax? sequenceAttribute = null;

        // Find the SequenceEquality attribute
        foreach (var attributeList in memberDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name == "SequenceEquality" || name == "SequenceEqualityAttribute")
                {
                    sequenceAttributeList = attributeList;
                    sequenceAttribute = attribute;
                    break;
                }
            }
            if (sequenceAttribute != null)
                break;
        }

        if (sequenceAttribute == null || sequenceAttributeList == null)
            return document;

        // Create new IncludeInEquality attribute
        var newAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("IncludeInEquality"));
        
        // Replace the attribute
        var newAttributeList = sequenceAttributeList.WithAttributes(
            sequenceAttributeList.Attributes.Replace(sequenceAttribute, newAttribute));

        var newMemberDeclaration = memberDeclaration.ReplaceNode(sequenceAttributeList, newAttributeList);
        var newRoot = root.ReplaceNode(memberDeclaration, newMemberDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }
}