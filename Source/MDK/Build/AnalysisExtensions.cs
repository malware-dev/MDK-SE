using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// Extra utility functions while dealing with Roslyn code analysis
    /// </summary>
    public static class AnalysisExtensions
    {
        /// <summary>
        /// Determines whether the given symbol represents an interface implementation.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsInterfaceImplementation(this ISymbol symbol)
        {
            if (symbol.ContainingType == null)
                return false;
            return symbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers()).Any(member => symbol.ContainingType.FindImplementationForInterfaceMember(member).Equals(symbol));
        }

        /// <summary>
        /// Removes indentations from the given node if they are equal to or larger than the indicated number of indentations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="indentations"></param>
        /// <returns></returns>
        public static T Unindented<T>(this T node, int indentations) where T: SyntaxNode
        {
            var rewriter = new UnindentRewriter(indentations);
            return (T)rewriter.Visit(node);
        }

        /// <summary>
        /// Retrieves the fully qualified name of the given symbol.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Retrieves the fully qualified name of the given type declaration.
        /// </summary>
        /// <param name="typeDeclaration"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Determines whether the given syntax node is a symbol declaration.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsSymbolDeclaration(this SyntaxNode node)
        {
            return node is ClassDeclarationSyntax
                   || node is PropertyDeclarationSyntax
                   || node is EventDeclarationSyntax
                   || node is VariableDeclaratorSyntax
                   || node is EnumDeclarationSyntax
                   || node is EnumMemberDeclarationSyntax
                   || node is ConstructorDeclarationSyntax
                   || node is DelegateDeclarationSyntax
                   || node is MethodDeclarationSyntax
                   || node is StructDeclarationSyntax
                   || node is InterfaceDeclarationSyntax
                   || node is TypeParameterSyntax
                   || node is ParameterSyntax
                   || node is AnonymousObjectMemberDeclaratorSyntax;
        }

        /// <summary>
        /// Determines whether the given syntax node is a symbol declaration.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsTypeDeclaration(this SyntaxNode node)
        {
            return node is ClassDeclarationSyntax
                   || node is EnumDeclarationSyntax
                   || node is DelegateDeclarationSyntax
                   || node is StructDeclarationSyntax
                   || node is InterfaceDeclarationSyntax
                   || node is TypeParameterSyntax;
        }

        class UnindentRewriter : CSharpSyntaxRewriter
        {
            string _tabs;
            string _spaces;

            public UnindentRewriter(int indentations)
            {
                _tabs = new string('\t', indentations);
                _spaces = new string(' ', indentations * 4);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                return ReplaceTrivia(base.VisitTrivia(trivia));
            }

            SyntaxTrivia ReplaceTrivia(SyntaxTrivia triv)
            {
                if (!triv.IsKind(SyntaxKind.WhitespaceTrivia))
                    return triv;

                var loc = triv.GetLocation();
                var ls = loc.GetLineSpan();
                if (ls.StartLinePosition.Character != 0)
                    return triv;

                var triviaString = triv.ToFullString();
                if (triviaString.Equals(_tabs) || triviaString.Equals(_spaces))
                {
                    return SyntaxFactory.Whitespace("");
                }

                if (triviaString.StartsWith(_tabs))
                {
                    return SyntaxFactory.Whitespace(triviaString.Substring(_tabs.Length));
                }

                if (triviaString.StartsWith(_spaces))
                {
                    return SyntaxFactory.Whitespace(triviaString.Substring(_spaces.Length));
                }

                return triv;
            }
        }
    }
}
