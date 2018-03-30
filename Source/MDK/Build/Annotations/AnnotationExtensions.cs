using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MDK.Build.Annotations
{
    public static class AnnotationExtensions
    {
        public static bool ShouldBePreserved(this SyntaxNode syntaxNode)
        {
            return syntaxNode.GetAnnotations("MDK").Any(a => a.Data.Contains("preserve"));
        }

        public static bool ShouldBePreserved(this SyntaxTrivia syntaxTrivia)
        {
            return syntaxTrivia.GetAnnotations("MDK").Any(a => a.Data.Contains("preserve"));
        }

        public static bool ShouldBePreserved(this SyntaxToken token)
        {
            return token.GetAnnotations("MDK").Any(a => a.Data.Contains("preserve"));
        }

        /// <summary>
        /// Adds MDK annotations to everything. The MDK regions are removed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="macros">An optional dictionary of macros to replace within macro regions</param>
        /// <returns></returns>
        public static T TransformAndAnnotate<T>(this T node, IDictionary<string, string> macros = null) where T : SyntaxNode
        {
            var symbolDeclarations = new List<SyntaxNode>();
            var rewriter = new MdkAnnotationRewriter(macros, symbolDeclarations);
            var root = (T)rewriter.Visit(node);
            return root;
        }
    }
}
