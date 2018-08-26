using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MDK.Build.Annotations
{
    /// <summary>
    /// Extensions dealing with the MDK analysis annotations
    /// </summary>
    public static class AnnotationExtensions
    {
        /// <summary>
        /// Determines whether the given syntax node should be preserved from major changes.
        /// </summary>
        /// <param name="syntaxNode"></param>
        /// <returns></returns>
        public static bool ShouldBePreserved(this SyntaxNode syntaxNode)
        {
            return syntaxNode.GetAnnotations("MDK").Any(a => a.Data.Contains("preserve"));
        }

        /// <summary>
        /// Determines whether the given syntax trivia should be preserved from major changes.
        /// </summary>
        /// <param name="syntaxTrivia"></param>
        /// <returns></returns>
        public static bool ShouldBePreserved(this SyntaxTrivia syntaxTrivia)
        {
            return syntaxTrivia.GetAnnotations("MDK").Any(a => a.Data.Contains("preserve"));
        }

        /// <summary>
        /// Determines whether the given syntax token should be preserved from major changes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
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
        public static T TransformAndAnnotate<T>(this T node, IDictionary<string, string> macros = null) where T: SyntaxNode
        {
            var rewriter = new MdkAnnotationRewriter(macros);
            var root = (T)rewriter.Visit(node);
            return root;
        }
    }
}
