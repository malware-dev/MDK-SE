using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build.Annotations
{
    class MdkAnnotationRewriter : CSharpSyntaxRewriter
    {
        static readonly char[] TagSeparators = { ' ' };

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

        public override SyntaxNode Visit(SyntaxNode node)
        {
            node = base.Visit(node);
            if (node == null)
                return null;
            var region = _stack.Peek();
            if (region.Annotation != null)
                return node.WithAdditionalAnnotations(region.Annotation);

            if (node.IsSymbolDeclaration())
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
                        while (list.Count > 0 && list[list.Count - 1].Kind() == SyntaxKind.WhitespaceTrivia)
                            list.RemoveAt(list.Count - 1);
                        list.AddRange(regionDirective.GetTrailingTrivia().SkipWhile(t => t.Kind() == SyntaxKind.EndOfLineTrivia));
                        continue;
                    }

                    if (trivia.GetStructure() is EndRegionDirectiveTriviaSyntax endRegionDirective)
                    {
                        list.AddRange(endRegionDirective.GetLeadingTrivia());
                        while (list.Count > 0 && list[list.Count - 1].Kind() == SyntaxKind.WhitespaceTrivia)
                            list.RemoveAt(list.Count - 1);
                        list.AddRange(endRegionDirective.GetTrailingTrivia().SkipWhile(t => t.Kind() == SyntaxKind.EndOfLineTrivia));
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
}