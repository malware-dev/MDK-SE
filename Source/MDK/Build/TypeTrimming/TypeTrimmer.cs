using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build.Solution;
using MDK.Build.UsageAnalysis;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MDK.Build.TypeTrimming
{
    class TypeTrimmer
    {
        public async Task<ProgramComposition> Process([NotNull] ProgramComposition composition, [NotNull] ProjectScriptInfo config)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var analyzer = new UsageAnalyzer();
            while (true)
            {
                var symbolDefinitions = await analyzer.ProcessAsync(composition, config);
                var symbol = symbolDefinitions.FirstOrDefault(s => !s.IsProtected && s.SyntaxNode.IsTypeDeclaration() && HasNoUsage(s));
                if (symbol == null)
                    break;

                var rootNode = RemoveDefinition(composition.RootNode, symbol);
                composition = await composition.WithNewDocumentRootAsync(rootNode);
            }

            return composition;
        }

        static SyntaxNode RemoveDefinition(SyntaxNode rootNode, SymbolDefinitionInfo symbol)
        {
            return rootNode.RemoveNode(symbol.SyntaxNode, SyntaxRemoveOptions.KeepUnbalancedDirectives);
        }

        bool HasNoUsage(SymbolDefinitionInfo symbolDefinitionInfo) => 
            symbolDefinitionInfo.Usage.Sum(u => u.Locations.Count()) == 0;
    }
}
