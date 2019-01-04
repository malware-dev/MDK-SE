using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build.Solution;
using MDK.Build.UsageAnalysis;
using MDK.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Build.TypeTrimming
{
    class TypeTrimmer
    {
        static SyntaxNode RemoveDefinition(SyntaxNode rootNode, SymbolDefinitionInfo symbol)
        {
            return rootNode.RemoveNode(symbol.SyntaxNode, SyntaxRemoveOptions.KeepUnbalancedDirectives);
        }

        static bool IsEligibleForRemoval(SymbolDefinitionInfo definition)
        {
            if (definition.IsProtected)
                return false;
            var symbol = definition.Symbol;
            if (!symbol.IsDefinition)
                return false;
            if (!(symbol is ITypeSymbol typeSymbol))
                return false;
            if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                return false;
            return true;
        }

        static ISymbol FindTypeSymbol(ISymbol symbol)
        {
            if (symbol is ITypeSymbol)
                return symbol;
            return symbol.ContainingType;
        }

        static async Task<ISymbol> FindTypeSymbolAsync(SyntaxNode rootNode, ReferenceLocation location)
        {
            var semanticModel = await location.Document.GetSemanticModelAsync();
            var syntaxNode = rootNode.FindNode(location.Location.SourceSpan);
            var typeDeclarationNode = syntaxNode.AncestorsAndSelf().FirstOrDefault(node => node is TypeDeclarationSyntax);
            if (typeDeclarationNode != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(typeDeclarationNode);
                if (symbol != null)
                    return symbol;
            }
            //if (TryFindTypeDefinition(syntaxNode, out var typeDefinitionSymbol))
            //    return typeDefinitionSymbol;

            return FindTypeSymbol(semanticModel.GetEnclosingSymbol(location.Location.SourceSpan.Start));
        }

        public async Task<ProgramComposition> ProcessAsync([NotNull] ProgramComposition composition, [NotNull] MDKProjectProperties config)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var nodes = new Dictionary<ISymbol, Node>();
            var analyzer = new UsageAnalyzer();
            var symbolDefinitions = await analyzer.FindUsagesAsync(composition, config);
            var symbolLookup = symbolDefinitions.GroupBy(d => d.Symbol).ToDictionary(g => g.Key, g => g.ToList());
            var rootNode = composition.RootNode;
            foreach (var definition in symbolDefinitions)
            {
                if (!(definition.Symbol is ITypeSymbol typeSymbol))
                    continue;
                if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                    continue;

                if (!nodes.TryGetValue(definition.Symbol, out var node))
                    nodes[definition.Symbol] = node = new Node(definition);
                else
                    node.Definitions.Add(definition);

                foreach (var usage in definition.Usage)
                {
                    foreach (var location in usage.Locations)
                    {
                        var enclosingSymbol = await FindTypeSymbolAsync(rootNode, location);
                        var enclosingSymbolDefinitions = symbolLookup[enclosingSymbol];
                        if (!nodes.TryGetValue(enclosingSymbol, out var referencingNode))
                            nodes[enclosingSymbol] = referencingNode = new Node(enclosingSymbolDefinitions);
                        if (node != referencingNode)
                            referencingNode.References.Add(node);
                    }
                }
            }

            var program = symbolDefinitions.FirstOrDefault(d => d.FullName == "Program");
            if (program == null || !nodes.TryGetValue(program.Symbol, out var programNode))
            {
                throw new BuildException(string.Format(Text.TypeTrimmer_ProcessAsync_NoEntryPoint, composition.Document.Project.FilePath));
            }

            var usedNodes = new List<Node>();
            var queue = new Queue<Node>();
            var visitedNodes = new HashSet<Node>();
            queue.Enqueue(programNode);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!visitedNodes.Add(node))
                    continue;
                usedNodes.Add(node);
                foreach (var reference in node.References)
                    queue.Enqueue(reference);
            }

            var usedSymbolDefinitions = usedNodes.SelectMany(n => n.Definitions).ToImmutableHashSet();
            var unusedSymbolDefinitions = symbolDefinitions.Where(definition => IsEligibleForRemoval(definition) && !usedSymbolDefinitions.Contains(definition)).ToList();
            var nodesToRemove = unusedSymbolDefinitions.Select(definition => definition.FullName).ToImmutableHashSet();

            var walker = new RemovalWalker(nodesToRemove);
            rootNode = walker.Visit(rootNode);
            foreach (var symbol in unusedSymbolDefinitions)
                rootNode = RemoveDefinition(rootNode, symbol);

            return await composition.WithNewDocumentRootAsync(rootNode);
        }

        class RemovalWalker : CSharpSyntaxRewriter
        {
            readonly ImmutableHashSet<string> _nodesToRemove;

            public RemovalWalker(ImmutableHashSet<string> nodesToRemove)
            {
                _nodesToRemove = nodesToRemove;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                Debug.WriteLine(node.Identifier.ToString());
                var result = base.VisitClassDeclaration(node);
                if (_nodesToRemove.Contains(node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                    return null;
                return result;
            }

            public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
            {
                Debug.WriteLine(node.Identifier.ToString());
                var result = base.VisitStructDeclaration(node);
                if (_nodesToRemove.Contains(node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                    return null;
                return result;
            }

            public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                Debug.WriteLine(node.Identifier.ToString());
                var result = base.VisitInterfaceDeclaration(node);
                if (_nodesToRemove.Contains(node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                    return null;
                return result;
            }

            public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
            {
                Debug.WriteLine(node.Identifier.ToString());
                var result = base.VisitDelegateDeclaration(node);
                if (_nodesToRemove.Contains(node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName)))
                    return null;
                return result;
            }
        }


        class Node
        {
            public Node(SymbolDefinitionInfo definition)
            {
                Definitions = new HashSet<SymbolDefinitionInfo>()
                {
                    definition
                };
            }

            public Node(IEnumerable<SymbolDefinitionInfo> definitions)
            {
                Definitions = new HashSet<SymbolDefinitionInfo>(definitions);
            }

            public HashSet<SymbolDefinitionInfo> Definitions { get; }

            public HashSet<Node> References { get; } = new HashSet<Node>();

            public override string ToString() => Definitions.FirstOrDefault()?.FullName ?? "";
        }
    }
}
