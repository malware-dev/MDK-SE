using System;
using System.Collections.Generic;
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
        static readonly char[] BaseNChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        //async Task<Document> TrimWhitespace(Document document)
        //{
        //    var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
        //    var regex = new Regex(@"(?<string>\$?((@""[^""]*(""""[^""]*)*"")|(""[^""\\\r\n]*(?:\\.[^""\\\r\n]*)*"")))|(?<whitespace>\s+)");
        //    var originalCode = root.ToString();
        //    var lastNewlineIndex = 0;
        //    var cleanedCode = regex.Replace(originalCode, match =>
        //    {
        //        if (match.Groups["string"].Success)
        //            return match.Value;

        //        if (match.Index - lastNewlineIndex > 100)
        //        {
        //            lastNewlineIndex = match.Index;
        //            return "\n";
        //        }
        //        return " ";
        //    });
        //    return document.WithText(SourceText.From(cleanedCode));
        //}

        /// <summary>
        /// Determines whether the given symbol represents an interface implementation.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsInterfaceImplementation(ISymbol symbol)
        {
            return symbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers()).Any(member => symbol.ContainingType.FindImplementationForInterfaceMember(member).Equals(symbol));
        }

        //static bool IsMemberDeclaration(ISymbol symbol)
        //{
        //    return symbol is IMethodSymbol
        //           || symbol is IPropertySymbol
        //           || symbol is IEventSymbol;
        //}

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
            //document = await TrimWhitespace(document).ConfigureAwait(false);
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

            //var typeDeclarations = root.DescendantNodes().Where(n => n is ClassDeclarationSyntax || n is StructDeclarationSyntax).Cast<TypeDeclarationSyntax>()
            //    .SelectMany(d => d.BaseList.Types.Where(t => IsInterfaceReference(t, semanticModel)))
            //    .ToArray();

            var declaredSymbols = root.DescendantNodes().Where(IsSymbolDeclaration)
                .Select(n => semanticModel.GetDeclaredSymbol(n))
                .Where(s => s != null)
                .Where(s => !protectedNames.Contains(s.Name))
                .Where(s => !protectedSymbols.Contains(s.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                .Where(s => !s.IsOverride)
                .Where(s => !IsInterfaceImplementation(s))
                //.Where(s => !s.Name.Contains("."))
                .ToArray();

            //var implementedInterfaces = declaredSymbols.Where(IsMemberDeclaration).SelectMany(m => m.ContainingType.Interfaces).Distinct().ToArray();

            //var z = declaredSymbols.Where(IsInterfaceImplementation).ToArray();

            //foreach (var iface in implementedInterfaces)
            //{
            //    var symbols = await SymbolFinder.FindImplementedInterfaceMembersAsync(iface, document.Project.Solution, ImmutableHashSet.Create(document.Project));
            //}

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
                    .Where(IsSymbolDeclaration)
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
                // If a symbol name is less than or equal to the maximum minified symbol length, just leave it
                if (symbol.Name.Length <= maxSymbolLength)
                {
                    minStart = symbolNode.FullSpan.Start;
                    continue;
                }
                minStart = symbolNode.FullSpan.Start;
                if (!minifiedSymbolNames.TryGetValue(symbol.Name, out string newName))
                    continue;
                //if (symbol.Name.Contains("."))
                //{
                //    minStart = symbolNode.FullSpan.Start;
                //    continue;
                //}
                //var newName = minifiedSymbolNames[symbol.Name];
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
                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    case SyntaxKind.WhitespaceTrivia:
                    case SyntaxKind.EndOfLineTrivia:
                        return default(SyntaxTrivia);
                    default:
                        return base.VisitTrivia(trivia);
                }
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
                    return node.WithModifiers(modifiers.ToArray());
                }
                return changed;
            }
        }
    }
}
