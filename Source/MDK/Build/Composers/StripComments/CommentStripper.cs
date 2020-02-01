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
    /// <summary>
    /// Simplified version of minifying rewriter that only removes trivia.
    /// </summary>
    class CommentStripper : ProgramRewriter
    {
        HashSet<string> _externallyReferencedMembers = new HashSet<string>();

        public CommentStripper() : base(true)
        { }

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

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            var newDocument = await Simplifier.ReduceAsync(composition.Document).ConfigureAwait(false);
            composition = await (composition.WithDocumentAsync(newDocument).ConfigureAwait(false));

            _externallyReferencedMembers.Clear();

            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }
    }
}
