using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    class UsingDirectiveComparer : IEqualityComparer<UsingDirectiveSyntax>
    {
        public bool Equals(UsingDirectiveSyntax x, UsingDirectiveSyntax y)
        {
            return x.ToString().Equals(y.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        public int GetHashCode(UsingDirectiveSyntax obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}