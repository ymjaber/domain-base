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
/// Provides code fixes for DBVO005 (multiple equality attributes). Offers to keep one and remove the others.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ResolveMultipleEqualityAttributesCodeFixProvider)), Shared]
public class ResolveMultipleEqualityAttributesCodeFixProvider : CodeFixProvider
{
    private static readonly string[] EqualityAttributes = new[]
    {
        "IncludeInEquality", "IncludeInEqualityAttribute",
        "CustomEquality", "CustomEqualityAttribute",
        "SequenceEquality", "SequenceEqualityAttribute",
        "IgnoreEquality", "IgnoreEqualityAttribute"
    };

    /// <summary>
    /// Gets the diagnostic IDs this code fix provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DBVO005");

    /// <summary>
    /// Gets the FixAll provider used to fix all occurrences of the diagnostic.
    /// </summary>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the specified context.
    /// </summary>
    /// <param name="context">The code fix context.</param>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDeclaration == null) return;

        // Offer a fix per attribute kind to keep
        var kinds = new[] { "IncludeInEquality", "CustomEquality", "SequenceEquality", "IgnoreEquality" };
        foreach (var kind in kinds)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Keep [{kind}]",
                    createChangedDocument: c => KeepOnlyAsync(context.Document, memberDeclaration, kind, c),
                    equivalenceKey: $"KeepOnly_{kind}"),
                diagnostic);
        }
    }

    private async Task<Document> KeepOnlyAsync(Document document, MemberDeclarationSyntax memberDeclaration, string keepKind, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var newMember = memberDeclaration;
        var lists = memberDeclaration.AttributeLists;

        var newLists = new SyntaxList<AttributeListSyntax>();
        foreach (var list in lists)
        {
            var filtered = list.Attributes.Where(a => IsAttributeOfKind(a, keepKind)).ToArray();
            if (filtered.Length > 0)
            {
                var newList = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(filtered));
                newLists = newLists.Add(newList);
            }
        }

        // If no attribute of keepKind existed, just remove all equality attributes
        if (newLists.Count == 0)
        {
            var stripped = StripEqualityAttributes(memberDeclaration);
            var newRootStripped = root.ReplaceNode(memberDeclaration, stripped);
            return document.WithSyntaxRoot(newRootStripped);
        }

        newMember = memberDeclaration.WithAttributeLists(newLists);
        var newRoot = root.ReplaceNode(memberDeclaration, newMember);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsAttributeOfKind(AttributeSyntax attribute, string kind)
    {
        var name = attribute.Name.ToString();
        if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - 9);
        return string.Equals(name, kind, System.StringComparison.Ordinal);
    }

    private static MemberDeclarationSyntax StripEqualityAttributes(MemberDeclarationSyntax member)
    {
        var keepLists = new SyntaxList<AttributeListSyntax>();
        foreach (var list in member.AttributeLists)
        {
            var nonEquality = list.Attributes.Where(a => !EqualityAttributes.Contains(a.Name.ToString())).ToArray();
            if (nonEquality.Length > 0)
            {
                keepLists = keepLists.Add(list.WithAttributes(SyntaxFactory.SeparatedList(nonEquality)));
            }
        }
        return member.WithAttributeLists(keepLists);
    }
}

