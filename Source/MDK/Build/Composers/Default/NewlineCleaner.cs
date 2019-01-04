using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MDK.Build.Composers.Default
{
    class NewlineCleaner : CSharpSyntaxRewriter
    {
        public NewlineCleaner() : base(true)
        { }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            trivia = base.VisitTrivia(trivia);
            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia && trivia.ToString() == "\r\n")
            {
                trivia = SyntaxFactory.EndOfLine("\n");
            }

            return trivia;
        }

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }
    }
}
