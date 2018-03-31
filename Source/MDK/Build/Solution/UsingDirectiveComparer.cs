using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// A comparer designed to check the equality of two using directives.
    /// </summary>
    class UsingDirectiveComparer : IEqualityComparer<UsingDirectiveSyntax>
    {
        /// <summary>
        /// Determines whether the two using directives are equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(UsingDirectiveSyntax x, UsingDirectiveSyntax y)
        {
            return x.ToString().Equals(y.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the hash code of the given using directive.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(UsingDirectiveSyntax obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
