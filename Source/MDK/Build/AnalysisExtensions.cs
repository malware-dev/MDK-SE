using System;
using System.Collections.Generic;
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
        /// Adds MDK annotations to everything. The MDK regions are removed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="macros">An optional dictionary of macros to replace within macro regions</param>
        /// <returns></returns>
        public static T TransformAndAnnotate<T>(this T node, IDictionary<string, string> macros = null) where T: SyntaxNode
        {
            var symbolDeclarations = new List<SyntaxNode>();
            var rewriter = new MdkAnnotationRewriter(macros, symbolDeclarations);
            var root = (T)rewriter.Visit(node);
            return root;
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

        class MdkAnnotationRewriter : CSharpSyntaxRewriter
        {
            static readonly char[] TagSeparators = {' '};

            List<SyntaxNode> _symbolDeclarations;
            readonly IDictionary<string, string> _macros;
            Regex _regionRegex = new Regex(@"\s*#region\s+mdk\s+([^\r\n]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Stack<RegionInfo> _stack = new Stack<RegionInfo>();
            Regex _macroRegex = new Regex(@"\$\w+\$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public MdkAnnotationRewriter(IDictionary<string, string> macros, List<SyntaxNode> symbolDeclarations) : base(true)
            {
                _symbolDeclarations = symbolDeclarations;
                _macros = macros;
                _stack.Push(new RegionInfo());
            }

            string ReplaceMacros(string content)
            {
                return _macroRegex.Replace(content, match =>
                {
                    if (_macros.TryGetValue(match.Value, out var replacement))
                        return replacement;
                    return "";
                });
            }

            bool IsSymbolDeclaration(SyntaxNode node)
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

            public override SyntaxNode Visit(SyntaxNode node)
            {
                node = base.Visit(node);
                if (node == null)
                    return null;
                var region = _stack.Peek();
                if (region.Annotation != null)
                    return node.WithAdditionalAnnotations(region.Annotation);

                if (IsSymbolDeclaration(node))
                    _symbolDeclarations.Add(node);
                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                token = base.VisitToken(token);
                var region = _stack.Peek();
                if (token.IsKind(SyntaxKind.StringLiteralToken) && region.ExpandsMacros)
                {
                    token = SyntaxFactory.Literal(ReplaceMacros(token.Text));
                }

                if (region.Annotation != null)
                    token = token.WithAdditionalAnnotations(region.Annotation);

                if (token.HasStructuredTrivia)
                {
                    if (token.HasLeadingTrivia)
                    {
                        var originalTrivia = token.LeadingTrivia;
                        var trimmedTrivia = TrimTrivia(originalTrivia);
                        token = token.WithLeadingTrivia(trimmedTrivia);
                    }

                    if (token.HasTrailingTrivia)
                    {
                        var originalTrivia = token.TrailingTrivia;
                        var trimmedTrivia = TrimTrivia(originalTrivia);
                        token = token.WithTrailingTrivia(trimmedTrivia);
                    }
                }

                return token;
            }

            SyntaxTriviaList TrimTrivia(SyntaxTriviaList source)
            {
                var list = new List<SyntaxTrivia>();
                foreach (var trivia in source)
                {
                    if (trivia.HasStructure)
                    {
                        if (trivia.GetStructure() is RegionDirectiveTriviaSyntax regionDirective)
                        {
                            list.AddRange(regionDirective.GetLeadingTrivia());
                            list.AddRange(regionDirective.GetTrailingTrivia());
                            continue;
                        }

                        if (trivia.GetStructure() is EndRegionDirectiveTriviaSyntax endRegionDirective)
                        {
                            list.AddRange(endRegionDirective.GetLeadingTrivia());
                            list.AddRange(endRegionDirective.GetTrailingTrivia());
                            continue;
                        }
                    }

                    list.Add(trivia);
                }

                return SyntaxFactory.TriviaList(list);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                trivia = base.VisitTrivia(trivia);
                var region = _stack.Peek();
                if (region.ExpandsMacros)
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    {
                        return SyntaxFactory.Comment(ReplaceMacros(trivia.ToFullString()))
                            .WithAdditionalAnnotations(region.Annotation);
                    }

                    if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                    {
                        //return SyntaxFactory.DocumentationCommentTrivia(ReplaceMacros(trivia.ToFullString()))
                        //    .WithAdditionalAnnotations(region.Annotation);
                    }

                    if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        return SyntaxFactory.Comment(ReplaceMacros(trivia.ToFullString()))
                            .WithAdditionalAnnotations(region.Annotation);
                    }

                    if (trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    { }
                }

                if (region.Annotation != null)
                    trivia = trivia.WithAdditionalAnnotations(region.Annotation);
                return trivia;
            }

            public override SyntaxNode VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
            {
                node = (RegionDirectiveTriviaSyntax)base.VisitRegionDirectiveTrivia(node);
                var region = _stack.Peek();
                var content = node.ToString().Trim();
                var match = _regionRegex.Match(content);
                if (match.Success)
                {
                    var tags = match.Groups[1].Value.Trim().Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries);
                    var tagString = string.Join(" ", tags);
                    if (region.Annotation != null)
                        tagString = region.Annotation.Data + " " + tagString;
                    region = new RegionInfo(new SyntaxAnnotation("MDK", tagString));
                    _stack.Push(region);
                    return node;
                }

                _stack.Push(region.AsCopy());
                if (region.Annotation != null)
                    return node.WithAdditionalAnnotations(region.Annotation);
                return node;
            }

            public override SyntaxNode VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
            {
                var region = _stack.Pop();
                if (region.IsDeclaration)
                    return null;
                region = _stack.Peek();
                if (region.Annotation != null)
                    return node.WithAdditionalAnnotations(region.Annotation);
                return node;
            }

            struct RegionInfo
            {
                public SyntaxAnnotation Annotation { get; }
                public bool IsDeclaration { get; }
                public bool ExpandsMacros { get; }

                public RegionInfo(SyntaxAnnotation annotation, bool isDeclaration = true)
                {
                    Annotation = annotation;
                    IsDeclaration = isDeclaration;
                    ExpandsMacros = annotation != null && Regex.IsMatch(annotation.Data, @"\bmacros\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                }

                public RegionInfo AsCopy() => new RegionInfo(Annotation, false);
            }
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
