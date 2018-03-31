using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MDK.Build.UsageAnalysis
{
    class UsageAnalyzer
    {
        SymbolAnalyzer _symbolAnalyzer = new SymbolAnalyzer();

        public async Task<ImmutableArray<SymbolDefinitionInfo>> FindUsagesAsync(ProgramComposition composition, ProjectScriptInfo config)
        {
            var symbolDefinitions = _symbolAnalyzer.FindSymbols(composition, config).ToArray();

            for (var index = 0; index < symbolDefinitions.Length; index++)
                symbolDefinitions[index] = await WithUsageDataAsync(symbolDefinitions[index], composition);

            return symbolDefinitions.ToImmutableArray();
        }

        async Task<SymbolDefinitionInfo> WithUsageDataAsync(SymbolDefinitionInfo definition, ProgramComposition composition)
        {
            var references = (await SymbolFinder.FindReferencesAsync(definition.Symbol, composition.Document.Project.Solution))
                .ToImmutableArray();
            return definition.WithUsageData(references);
        }
    }
}
