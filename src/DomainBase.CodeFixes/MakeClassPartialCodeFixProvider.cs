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
/// Provides a code fix to add the <c>partial</c> modifier to classes that require it for source generation.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeClassPartialCodeFixProvider)), Shared]
public class MakeClassPartialCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DBVO001", "DBEN003");

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

        var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>().FirstOrDefault();
            
        if (classDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Make class partial",
                createChangedDocument: c => MakeClassPartialAsync(context.Document, classDeclaration, c),
                equivalenceKey: "MakeClassPartial"),
            diagnostic);
    }

    private async Task<Document> MakeClassPartialAsync(Document document, ClassDeclarationSyntax classDeclaration, 
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var newModifiers = classDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);

        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}