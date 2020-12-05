using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using JetBrains.Annotations;
using MDK.Build.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build.DocumentAnalysis
{
    /// <summary>
    /// Analyses a document, finding <see cref="ScriptPart"/> and other useful information related to a build operation.
    /// </summary>
    public class DocumentAnalyzer
    {
        /// <summary>
        /// Analyse the given document, then also transform macros and annotate with MDK annotations.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="macros"></param>
        /// <returns></returns>
        public async Task<DocumentAnalysisResult> AnalyzeAndTransformAsync([NotNull] Document document, [NotNull] Dictionary<string, string> macros)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (macros == null)
                throw new ArgumentNullException(nameof(macros));
            return await AnalyseCoreAsync(document, macros);
        }

        async Task<DocumentAnalysisResult> AnalyseCoreAsync(Document document, Dictionary<string, string> macros)
        {
            var text = await document.GetTextAsync();
            string mdkOptions = null;
            for (var i = 0; i < text.Lines.Count; i++)
            {
                var line = text.Lines[i].ToString().Trim();
                if (Regex.IsMatch(line, @"^//\s*<debug\s*/>\s*$"))
                    return null;
                if (mdkOptions == null && Regex.IsMatch(line, @"^//\s*<mdk\s*((?!/>).)*/>\s*$"))
                    mdkOptions = line.Substring(2);
                if (line.Length == 0 || line.StartsWith("//"))
                    continue;
                break;
            }
            
            int? sortWeight = null;
            if (mdkOptions != null)
            {
                try
                {
                    var options = XElement.Parse(mdkOptions);
                    var sortOrderValue = (int?)options.Attribute("sortorder");
                    if (sortOrderValue != null)
                        sortWeight = -sortOrderValue.Value;
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            var parts = ImmutableArray.CreateBuilder<ScriptPart>();
            var usings = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            if (macros != null)
                root = root.TransformAndAnnotate(macros);
            var walker = new Walker(sortWeight, document, parts, usings);
            walker.Visit(root);
            return new DocumentAnalysisResult(parts.ToImmutable(), usings.ToImmutable());
        }

        /// <summary>
        /// Analyse the given document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public Task<DocumentAnalysisResult> AnalyzeAsync([NotNull] Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return AnalyseCoreAsync(document, null);
        }

        class Walker : CSharpSyntaxWalker
        {
            int? _sortWeight;
            readonly Document _document;
            readonly ImmutableArray<ScriptPart>.Builder _parts;
            readonly ImmutableArray<UsingDirectiveSyntax>.Builder _usings;

            public Walker(int? sortWeight, Document document, ImmutableArray<ScriptPart>.Builder parts, ImmutableArray<UsingDirectiveSyntax>.Builder usings)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                _sortWeight = sortWeight;
                _document = document;
                _parts = parts;
                _usings = usings;
            }

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                base.VisitUsingDirective(node);
                _usings.Add(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node, _sortWeight));
                _sortWeight++;
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node, _sortWeight));
                _sortWeight++;
            }

            public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node, _sortWeight));
                _sortWeight++;
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node, _sortWeight));
                _sortWeight++;
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program")
                    _parts.Add(new ProgramScriptPart(_document, MoveBraceTrivia(node), _sortWeight));
                else
                    _parts.Add(new ExtensionScriptPart(_document, node, _sortWeight));
                _sortWeight++;
            }

            ClassDeclarationSyntax MoveBraceTrivia(ClassDeclarationSyntax node)
            {
                // If there is some node to move the brace trivia to, do so in order
                // to have it treated correctly by the composers. Otherwise it doesn't
                // matter.
                var firstChild = node.ChildNodes().FirstOrDefault();
                if (firstChild != null)
                {
                    var trailingTrivia = node.OpenBraceToken.TrailingTrivia;

                    // Skip the whitespace and line the brace itself is on
                    var i = 0;
                    while (i < trailingTrivia.Count && trailingTrivia[i].Kind() == SyntaxKind.WhitespaceTrivia)
                        i++;
                    if (i < trailingTrivia.Count && trailingTrivia[i].Kind() == SyntaxKind.EndOfLineTrivia)
                        i++;
                    node = node.WithOpenBraceToken(node.OpenBraceToken.WithTrailingTrivia(SyntaxFactory.EndOfLine("\n")));
                    if (i < trailingTrivia.Count)
                        node = node.ReplaceNode(firstChild, firstChild.WithLeadingTrivia(firstChild.GetLeadingTrivia().InsertRange(0, trailingTrivia.Skip(i))));
                }

                var lastChild = node.ChildNodes().LastOrDefault();
                if (lastChild != null)
                {
                    var leadingTrivia = node.CloseBraceToken.LeadingTrivia;
                    node = node.WithCloseBraceToken(node.CloseBraceToken.WithLeadingTrivia());
                    node = node.ReplaceNode(lastChild, lastChild.WithTrailingTrivia(lastChild.GetTrailingTrivia().AddRange(leadingTrivia)));
                }

                return node;
            }
        }
    }
}
