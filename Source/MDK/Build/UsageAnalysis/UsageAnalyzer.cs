using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MDK.Build.UsageAnalysis
{
    class UsageAnalyzer
    {
        SymbolAnalyzer _symbolAnalyzer = new SymbolAnalyzer();

        public async Task<ImmutableArray<SymbolDefinitionInfo>> FindUsagesAsync(ProgramComposition composition, MDKProjectProperties config)
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
            definition = definition.WithUsageData(references);

            // Check for extension class usage
            var symbol = definition.Symbol;
            if (symbol.IsDefinition && symbol is ITypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Class && typeSymbol.IsStatic && typeSymbol.ContainingType == null)
            {
                var members = typeSymbol.GetMembers().Where(m => m is IMethodSymbol methodSymbol && methodSymbol.IsStatic && methodSymbol.IsExtensionMethod).ToArray();
                foreach (var member in members)
                    references = references.AddRange((await SymbolFinder.FindReferencesAsync(member, composition.Document.Project.Solution)));
                definition = definition.WithUsageData(references);
            }

            return definition;
        }
    }
}
