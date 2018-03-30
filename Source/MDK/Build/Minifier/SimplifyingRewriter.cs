using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Annotations;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace MDK.Build.Minifier
{
    class SimplifyingRewriter : CSharpSyntaxRewriter
    {
        public SimplifyingRewriter() : base(true)
        { }

        bool IsImportantTypeModifier(SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                    return false;
                default:
                    return true;
            }
        }

        bool IsImportantFieldModifier(SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.ReadOnlyKeyword:
                    return false;
                default:
                    return true;
            }
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node);

            // Readonly keywords are not required
            node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantFieldModifier)));

            return node;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (node.Parent is CompilationUnitSyntax || node.Parent is NamespaceDeclarationSyntax)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (node.Parent is CompilationUnitSyntax || node.Parent is NamespaceDeclarationSyntax)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            node = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (node.Parent is CompilationUnitSyntax || node.Parent is NamespaceDeclarationSyntax)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            node = (DelegateDeclarationSyntax)base.VisitDelegateDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (node.Parent is CompilationUnitSyntax || node.Parent is NamespaceDeclarationSyntax)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

        //public override SyntaxNode Visit(SyntaxNode node)
        //{
        //    node = base.Visit(node);
        //    if (node == null)
        //        return null;

        //    var newTrivia = new List<SyntaxTrivia>();
        //    var trivia = node.GetLeadingTrivia();
        //    TrimTrivia(trivia, newTrivia);
        //    node = node.WithLeadingTrivia(newTrivia);
        //    trivia = node.GetTrailingTrivia();
        //    TrimTrivia(trivia, newTrivia);
        //    node = node.WithTrailingTrivia(newTrivia);

        //    return node;
        //}

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);

            var newTrivia = new List<SyntaxTrivia>();
            var trivia = token.LeadingTrivia;
            TrimTrivia(trivia, newTrivia);
            token = token.WithLeadingTrivia(newTrivia);
            trivia = token.TrailingTrivia;
            TrimTrivia(trivia, newTrivia);
            token = token.WithTrailingTrivia(newTrivia);

            return token;
        }

        void TrimTrivia(SyntaxTriviaList leadingTrivia, List<SyntaxTrivia> newTrivia)
        {
            newTrivia.Clear();

            for (var index = 0; index < leadingTrivia.Count; index++)
            {
                var trivia = leadingTrivia[index];
                if (trivia.ShouldBePreserved())
                {
                    newTrivia.Add(trivia);
                    continue;
                }

                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.DocumentationCommentExteriorTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        while (index < leadingTrivia.Count && leadingTrivia[index].Kind() != SyntaxKind.EndOfLineTrivia)
                            index++;
                        while (newTrivia.Count > 0 && newTrivia[newTrivia.Count - 1].Kind() == SyntaxKind.WhitespaceTrivia)
                            newTrivia.RemoveAt(newTrivia.Count - 1);
                        continue;

                    default:
                        newTrivia.Add(trivia);
                        break;
                }
            }
        }

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, ProjectScriptInfo config)
        {
            var newDocument = await Simplifier.ReduceAsync(composition.Document).ConfigureAwait(false);
            composition = await (composition.WithDocumentAsync(newDocument).ConfigureAwait(false));

            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }
    }
}
