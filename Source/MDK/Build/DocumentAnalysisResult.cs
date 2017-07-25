using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// Contains the results of a document analysis performed by <see cref="DocumentAnalyzer.Analyze"/>.
    /// </summary>
    public class DocumentAnalysisResult
    {
        /// <summary>
        /// Creates a new instance of <see cref="DocumentAnalysisResult"/>
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="usingDirectives"></param>
        public DocumentAnalysisResult(ImmutableArray<ScriptPart> parts, ImmutableArray<UsingDirectiveSyntax> usingDirectives)
        {
            Parts = parts;
            UsingDirectives = usingDirectives;
        }

        /// <summary>
        /// The <see cref="ScriptPart"/> definitions detected in the document
        /// </summary>
        public ImmutableArray<ScriptPart> Parts { get; }

        /// <summary>
        /// The using directives detected in the document
        /// </summary>
        public ImmutableArray<UsingDirectiveSyntax> UsingDirectives { get; }
    }
}
