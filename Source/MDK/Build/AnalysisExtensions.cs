using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    public static class AnalysisExtensions
    {
        public static string GetFullName(this ISymbol symbol, DeclarationFullNameFlags flags = DeclarationFullNameFlags.Default)
        {
            var ident = new List<string>(10)
            {
                symbol.Name
            };
            var parent = symbol.ContainingSymbol;
            while (parent != null)
            {
                if (parent is INamespaceSymbol ns && ns.IsGlobalNamespace)
                    break;
                if ((flags & DeclarationFullNameFlags.WithoutNamespaceName) != 0 && parent is INamespaceSymbol)
                    break;
                ident.Add(parent.Name);
                parent = parent.ContainingSymbol;
            }
            ident.Reverse();
            return string.Join(".", ident);
        }

        public static string GetFullName(this TypeDeclarationSyntax typeDeclaration, DeclarationFullNameFlags flags = DeclarationFullNameFlags.Default)
        {
            var ident = new List<string>(10)
            {
                typeDeclaration.Identifier.ToString()
            };
            var parent = typeDeclaration.Parent;
            while (parent != null)
            {
                if (parent is TypeDeclarationSyntax type)
                {
                    ident.Add(type.Identifier.ToString());
                    parent = parent.Parent;
                    continue;
                }
                if ((flags & DeclarationFullNameFlags.WithoutNamespaceName) == 0 && parent is NamespaceDeclarationSyntax ns)
                {
                    ident.Add(ns.Name.ToString());
                    parent = parent.Parent;
                    continue;
                }
                break;
            }
            ident.Reverse();
            return string.Join(".", ident);
        }
    }
}