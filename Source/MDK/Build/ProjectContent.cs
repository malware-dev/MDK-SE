using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    public class ProjectContent
    {
        public ImmutableArray<UsingDirectiveSyntax> UsingDirectives { get; }
        public ImmutableArray<ScriptPart> Parts { get; }
        public string Readme { get; }

        public ProjectContent(ImmutableArray<UsingDirectiveSyntax> usingDirectives, ImmutableArray<ScriptPart> parts, string readme)
        {
            UsingDirectives = usingDirectives;
            Parts = parts;
            Readme = readme;
        }
    }
}