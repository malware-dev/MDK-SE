using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// Flags altering the behavior of <see cref="AnalysisExtensions.GetFullName(ISymbol, DeclarationFullNameFlags)"/> and <see cref="AnalysisExtensions.GetFullName(TypeDeclarationSyntax, DeclarationFullNameFlags)"/>
    /// </summary>
    [Flags]
    public enum DeclarationFullNameFlags
    {
        /// <summary>
        /// Default behavior. Returns a complete, fully qualified name, including the namespace.
        /// </summary>
        Default = 0b0000,

        /// <summary>
        /// Only generates the name up to the outermost type declaration. Does not include the namespace.
        /// </summary>
        WithoutNamespaceName = 0b0001
    }
}
