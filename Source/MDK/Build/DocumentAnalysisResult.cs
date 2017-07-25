using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    public class DocumentAnalysisResult
    {
        public DocumentAnalysisResult(ImmutableArray<ScriptPart> parts, ImmutableArray<UsingDirectiveSyntax> usingDirectives)
        {
            Parts = parts;
            UsingDirectives = usingDirectives;
        }

        public ImmutableArray<ScriptPart> Parts { get; }
        public ImmutableArray<UsingDirectiveSyntax> UsingDirectives { get; }
    }
}