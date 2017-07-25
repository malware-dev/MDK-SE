using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// Analyses a document, finding <see cref="ScriptPart"/> and other useful information related to a build operation.
    /// </summary>
    public class DocumentAnalyzer
    {
        /// <summary>
        /// Analyse the given document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public async Task<DocumentAnalysisResult> Analyze(Document document)
        {
            var text = await document.GetTextAsync();
            for (var i = 0; i < text.Lines.Count; i++)
            {
                var line = text.Lines[i].ToString().Trim();
                if (Regex.IsMatch(line, @"^//\s*<debug\s*/>\s*$"))
                    return null;
                if (line.Length == 0 || line.StartsWith("//"))
                    continue;
                break;
            }
            var parts = ImmutableArray.CreateBuilder<ScriptPart>();
            var usings = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var walker = new Walker(document, parts, usings);
            walker.Visit(root);
            return new DocumentAnalysisResult(parts.ToImmutable(), usings.ToImmutable());
        }

        class Walker : CSharpSyntaxWalker
        {
            readonly Document _document;
            readonly ImmutableArray<ScriptPart>.Builder _parts;
            readonly ImmutableArray<UsingDirectiveSyntax>.Builder _usings;

            public Walker(Document document, ImmutableArray<ScriptPart>.Builder parts, ImmutableArray<UsingDirectiveSyntax>.Builder usings)
            {
                _document = document;
                _parts = parts;
                _usings = usings;
            }

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                _usings.Add(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node));
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node));
            }

            public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node));
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                _parts.Add(new ExtensionScriptPart(_document, node));
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program")
                    _parts.Add(new ProgramScriptPart(_document, node));
                else
                    _parts.Add(new ExtensionScriptPart(_document, node));
            }
        }
    }
}
