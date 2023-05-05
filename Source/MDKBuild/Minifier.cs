using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace MDK.Build
{
    /// <summary>
    /// Attempts to make the passed-in C# code as small as possible in terms of characters.
    /// </summary>
    public class Minifier
    {
        static char[] GetSymbolChars()
        {
            var chars = new List<char>(50000);
            for (int u = 0; u <= ushort.MaxValue; u++)
            {
                if (char.IsLetter((char)u))
                    chars.Add((char)u);
            }
            return chars.ToArray();
        }
        static readonly char[] BaseNChars = GetSymbolChars();

        /// <summary>
        /// Determines whether the given symbol represents an interface implementation.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsInterfaceImplementation(ISymbol symbol)
        {
            if (symbol.ContainingType == null)
                return false;
            return symbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers()).Any(member => symbol.ContainingType.FindImplementationForInterfaceMember(member).Equals(symbol));
        }

        /// <summary>
        /// Minification is performed in multiple steps in relation to the build process. This runs the first step.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public async Task<Document> PreMinify(Document document)
        {
            document = await TrimSyntax(document).ConfigureAwait(false);
            document = await Refactor(document).ConfigureAwait(false);
            document = await TrimTrivia(document).ConfigureAwait(false);
            return document;
        }

        async Task<Document> TrimTrivia(Document document)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var triviaTrimmer = new TriviaTrimmer();
            root = triviaTrimmer.Visit(root);
            root = root.NormalizeWhitespace("", "\n", true);
            return document.WithSyntaxRoot(root);
        }

        async Task<Document> Refactor(Document document)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

            var protectedSymbols = new HashSet<string>
            {
                "Program",
                "Program.Main",
                "Program.Save"
            };
            var protectedNames = new HashSet<string>
            {
                ".ctor"
            };
            var symbolSrc = 0;
            var semanticModel = await document.GetSemanticModelAsync();

            var declaredSymbols = root.DescendantNodes().Where(node => node.IsSymbolDeclaration())
                .Select(n => semanticModel.GetDeclaredSymbol(n))
                .Where(s => s != null)
                .Where(s => !protectedNames.Contains(s.Name))
                .Where(s => !protectedSymbols.Contains(s.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                .Where(s => !s.IsOverride)
                .Where(s => !IsInterfaceImplementation(s))
                .ToArray();

            var allDeclaredSymbols = new HashSet<string>(declaredSymbols.Select(s => s.Name).Where(n => n != null));
            var maxSymbolLength = allDeclaredSymbols.Count.ToNBaseString(BaseNChars).Length;
            var distinctSymbolNames = new HashSet<string>(allDeclaredSymbols.Distinct());
            var minifiedSymbolNames = distinctSymbolNames
                .ToDictionary(n => n, n => n.Length <= maxSymbolLength ? n : (symbolSrc++).ToNBaseString(BaseNChars));

            var documentId = document.Id;
            var minStart = -1;
            while (true)
            {
                semanticModel = await document.GetSemanticModelAsync();
                root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                var symbolNode = root.DescendantNodes()
                    .Where(node => node.IsSymbolDeclaration())
                    .FirstOrDefault(node => node.FullSpan.Start > minStart);
                if (symbolNode == null)
                    break;
                var symbol = semanticModel.GetDeclaredSymbol(symbolNode);
                if (symbol == null)
                    throw new InvalidOperationException($"Error retrieving the symbol for {symbolNode}");
                if (protectedNames.Contains(symbol.Name) || protectedSymbols.Contains(symbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                {
                    minStart = symbolNode.FullSpan.Start;
                    continue;
                }
                minStart = symbolNode.FullSpan.Start;
                if (!minifiedSymbolNames.TryGetValue(symbol.Name, out string newName))
                    continue;
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, newName, document.Project.Solution.Options);
                document = newSolution.GetDocument(documentId);
            }

            return document;
        }

        async Task<Document> TrimSyntax(Document document)
        {
            var trimmer = new SyntaxTrimmer();
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

            return document.WithSyntaxRoot(trimmer.Visit(root));
        }

        /// <summary>
        /// Minification is performed in multiple steps in relation to the build process. This runs the second step.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public string PostMinify(string script)
        {
            var regex = new Regex(@"(?<string>\$?((@""[^""]*(""""[^""]*)*"")|(""[^""\\\r\n]*(?:\\.[^""\\\r\n]*)*"")|('[^'\\\r\n]*(?:\\.[^'\\\r\n]*)*')))|(?<significant>\b\s+\b)|(?<insignificant>\s+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
            var lastNewlineIndex = 0;
            var indexOffset = 0;
            var cleanedCode = regex.Replace(script, match =>
            {
                if (match.Groups["string"].Success)
                    return match.Value;

                var index = match.Index + indexOffset;
                if (index - lastNewlineIndex > 100)
                {
                    indexOffset -= match.Length - 1;
                    lastNewlineIndex = index;
                    return "\n";
                }

                if (match.Groups["significant"].Success)
                {
                    indexOffset -= match.Length - 1;
                    return " ";
                }
                indexOffset -= match.Length;
                return "";
            });
            return cleanedCode;
        }

        class TriviaTrimmer : CSharpSyntaxRewriter
        {
            public TriviaTrimmer() : base(true)
            { }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                return default(SyntaxTrivia);
            }
        }

        class SyntaxTrimmer : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var changed = base.VisitFieldDeclaration(node);
                var modifiers = node.Modifiers.Select(m => m.Kind()).ToList();
                if (modifiers.Remove(SyntaxKind.ReadOnlyKeyword))
                {
                    return node.WithModifiers(
                        SyntaxFactory.TokenList(modifiers.Select(SyntaxFactory.Token).ToArray())
                        );
                }
                return changed;
            }
        }
    }
}
