using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Annotations;
using MDK.Build.Solution;
using MDK.Build.UsageAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace MDK.Build.Composers.Minifying
{
    class CodeSimplifier : ProgramRewriter
    {
        HashSet<string> _externallyReferencedMembers = new HashSet<string>();

        public CodeSimplifier() : base(true)
        { }
        //bool HasAdvancedUsage(ClassDeclarationSyntax classDeclaration)
        //{
        //    throw new System.NotImplementedException();
        //}

        bool IsImportantTypeModifier(SyntaxToken token)
        {
            if (token.ShouldBePreserved())
                return true;

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
            if (token.ShouldBePreserved())
                return true;

            switch (token.Kind())
            {
                case SyntaxKind.ReadOnlyKeyword:
                    return false;
                default:
                    return true;
            }
        }

        bool ShouldRemoveModifier(SyntaxNode node)
        {
            // If this node is referenced from outside of Program it needs to retain its modifier
            if (node is FieldDeclarationSyntax fieldDeclaration)
            {
                if (fieldDeclaration.Declaration.Variables.Any(v => _externallyReferencedMembers.Contains(v.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName))))
                    return false;
            }
            else if (node is MemberDeclarationSyntax classDeclaration && _externallyReferencedMembers.Contains(classDeclaration.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                return false;

            if (node.Parent is null || node.Parent is CompilationUnitSyntax || node.Parent is NamespaceDeclarationSyntax)
                return true;
            if (IsProgramClassDeclaration(node.Parent))
                return true;
            return false;
        }

        bool IsProgramClassDeclaration(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax classDeclaration && classDeclaration.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program")
                return true;
            return false;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node);

            // Readonly keywords are not required
            node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantFieldModifier)));

            if (shouldRemoveModifier)
            {
                // If this is a top level member, remove the modifier keyword as it's not needed
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));
            }

            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node);

            if (shouldRemoveModifier)
            {
                // If this is a top level member, remove the modifier keyword as it's not needed
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));
            }

            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            if (shouldRemoveModifier)
            {
                // If this is a top level member, remove the modifier keyword as it's not needed
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));
            }

            return node;
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (EventDeclarationSyntax)base.VisitEventDeclaration(node);

            if (shouldRemoveModifier)
            {
                // If this is a top level member, remove the modifier keyword as it's not needed
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));
            }

            return node;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            if (shouldRemoveModifier)
            {
                // If this is a top level type, remove the modifier keyword as it's not needed
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));
            }

            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (shouldRemoveModifier)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (shouldRemoveModifier)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var shouldRemoveModifier = ShouldRemoveModifier(node);

            node = (DelegateDeclarationSyntax)base.VisitDelegateDeclaration(node);

            // If this is a top level type, remove the modifier keyword as it's not needed
            if (shouldRemoveModifier)
                node = node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(IsImportantTypeModifier)));

            return node;
        }

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

        bool IsInProgram(SyntaxNode node)
        {
            while (!(node is null || node is CompilationUnitSyntax))
            {
                if (IsProgramClassDeclaration(node))
                    return true;
                node = node.Parent;
            }

            return false;
        }

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            var newDocument = await Simplifier.ReduceAsync(composition.Document).ConfigureAwait(false);
            composition = await (composition.WithDocumentAsync(newDocument).ConfigureAwait(false));

            _externallyReferencedMembers.Clear();
            var usageAnalyzer = new UsageAnalyzer();
            var typeUsages = await usageAnalyzer.FindUsagesAsync(composition, config);
            foreach (var typeUsage in typeUsages)
            {
                foreach (var part in typeUsage.Usage)
                {
                    foreach (var location in part.Locations)
                    {
                        var node = composition.RootNode.FindNode(location.Location.SourceSpan);
                        if (!IsInProgram(node))
                            _externallyReferencedMembers.Add(typeUsage.FullName);
                    }
                }
            }

            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }
    }
}
