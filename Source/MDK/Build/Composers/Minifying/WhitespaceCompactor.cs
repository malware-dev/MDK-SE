using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Annotations;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MDK.Build.Composers.Minifying
{
    class WhitespaceCompactor : ProgramRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            trivia = base.VisitTrivia(trivia);
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia) && trivia.ToString() == "\r\n")
                trivia = trivia.CopyAnnotationsTo(SyntaxFactory.EndOfLine("\n"));

            return trivia;
        }

        public override SyntaxToken VisitToken(SyntaxToken currentToken)
        {
            var previousToken = currentToken.GetPreviousToken();
            currentToken = base.VisitToken(currentToken);

            currentToken = currentToken.WithLeadingTrivia(currentToken.LeadingTrivia.Where(t => t.ShouldBePreserved()));
            currentToken = currentToken.WithTrailingTrivia(currentToken.TrailingTrivia.Where(t => t.ShouldBePreserved()));

            if (currentToken.LeadingTrivia.Sum(t => t.FullSpan.Length) + previousToken.TrailingTrivia.Where(t => t.ShouldBePreserved()).Sum(t => t.FullSpan.Length) == 0)
            {
                if (TokenCollisionDetector.IsColliding(previousToken.Kind(), currentToken.Kind()))
                    currentToken = currentToken.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
            }

            return currentToken;
        }
       
        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }

        public WhitespaceCompactor() : base(false)
        { }
    }
}
