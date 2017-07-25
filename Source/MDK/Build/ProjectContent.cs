using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// Contains the total content of a script project.
    /// </summary>
    public class ProjectContent
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProjectContent"/>
        /// </summary>
        /// <param name="usingDirectives"></param>
        /// <param name="parts"></param>
        /// <param name="readme"></param>
        public ProjectContent(ImmutableArray<UsingDirectiveSyntax> usingDirectives, ImmutableArray<ScriptPart> parts, string readme)
        {
            UsingDirectives = usingDirectives;
            Parts = parts;
            Readme = readme;
        }

        /// <summary>
        /// The combined list of using directives
        /// </summary>
        public ImmutableArray<UsingDirectiveSyntax> UsingDirectives { get; }

        /// <summary>
        /// All the <see cref="ScriptPart"/> in the project
        /// </summary>
        public ImmutableArray<ScriptPart> Parts { get; }

        /// <summary>
        /// The content of the ReadMe.cs file, if it exists
        /// </summary>
        public string Readme { get; }
    }
}
