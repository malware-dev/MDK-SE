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
            TrimTrivia(trivia, newTrivia, false);
            var previousToken = token.GetPreviousToken();
            if ((previousToken != null) && TokenCollisionDetector.IsColliding(token.Kind(), previousToken.Kind()))
                if (token.LeadingTrivia.Sum(t => t.FullSpan.Length) + previousToken.TrailingTrivia.Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Sum(t => t.FullSpan.Length) == 0)
                    newTrivia.Add(SyntaxFactory.Whitespace(" "));
            token = token.WithLeadingTrivia(newTrivia);
            trivia = token.TrailingTrivia;
            TrimTrivia(trivia, newTrivia, true);
            token = token.WithTrailingTrivia(newTrivia);

            return token;
        }
        /// <summary>
        /// Removes trivia surrounding a meaningful token.
        /// </summary>
        /// <param name="trivia">List of trivia tokens to be processed.</param>
        /// <param name="newTrivia">List of trivia tokens to be left in the script.</param>
        /// <param name="trailingMode">If true, use special behaviour for trailing trivia.</param>
        void TrimTrivia(SyntaxTriviaList trivia, List<SyntaxTrivia> newTrivia, bool trailingMode)
        {
            bool lastPreserved = false;
            newTrivia.Clear();
            for (var index = 0; index < trivia.Count; index++)
            {
                var triviaItem = trivia[index];
                if (triviaItem.ShouldBePreserved())
                {
                    lastPreserved = true;
                    newTrivia.Add(triviaItem);
                    continue;
                }
                if (lastPreserved && triviaItem.Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    lastPreserved = false;
                    continue;
                }
                switch (triviaItem.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.DocumentationCommentExteriorTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        while (index < trivia.Count && trivia[index].Kind() != SyntaxKind.EndOfLineTrivia)
                            index++;
                        if (trailingMode)
                            index--;
                        while (newTrivia.Count > 0 && newTrivia[newTrivia.Count - 1].Kind() == SyntaxKind.WhitespaceTrivia)
                            newTrivia.RemoveAt(newTrivia.Count - 1);
                        continue;

                    default:
                        newTrivia.Add(triviaItem);
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
