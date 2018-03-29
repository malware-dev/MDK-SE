using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MDK.Build.UsageAnalysis
{
    class UsageAnalyzer : ICompositionPreprocessor
    {
        static bool IsNotSpecialDefinitions(SymbolDefinitionInfo s)
        {
            return s.Symbol != null && !s.Symbol.IsOverride && !s.Symbol.IsInterfaceImplementation();
        }

        HashSet<string> _protectedSymbols = new HashSet<string>
        {
            "Program",
            "Program.Main",
            "Program.Save"
        };

        HashSet<string> _protectedNames = new HashSet<string>
        {
            ".ctor"
        };

        public async Task<ImmutableArray<SymbolDefinitionInfo>> ProcessAsync(ProgramComposition composition, ProjectScriptInfo config)
        {
            var document = composition.Document;
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync();

            var symbolDefinitions = root.DescendantNodes().Where(node => node.IsSymbolDeclaration())
                .Select(n => new SymbolDefinitionInfo(semanticModel.GetDeclaredSymbol(n), n))
                .Where(IsNotSpecialDefinitions)
                .Select(WithUpdatedProtectionFlag)
                .ToArray();

            for (var index = 0; index < symbolDefinitions.Length; index++)
                symbolDefinitions[index] = await WithUsageData(symbolDefinitions[index], semanticModel, composition);

            return symbolDefinitions.ToImmutableArray();
        }

        async Task<SymbolDefinitionInfo> WithUsageData(SymbolDefinitionInfo definition, SemanticModel semanticModel, ProgramComposition composition)
        {
            var references = (await SymbolFinder.FindReferencesAsync(definition.Symbol, composition.Document.Project.Solution))
                .ToImmutableArray();
            return definition.WithUsageData(references);
        }

        SymbolDefinitionInfo WithUpdatedProtectionFlag(SymbolDefinitionInfo d)
        {
            return _protectedNames.Contains(d.Symbol.Name)
                   || _protectedSymbols.Contains(d.Symbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName))
                   || HasPreserveAnnotation(d.SyntaxNode)
                ? d.AsProtected()
                : d.AsUnprotected();
        }

        bool HasPreserveAnnotation(SyntaxNode node)
        {
            if (!node.ContainsAnnotations)
                return false;
            var annotations = node.GetAnnotations("MDK").ToArray();
            var annotation = annotations.FirstOrDefault();
            return annotation?.Data.Contains("preserve") ?? false;
        }
    }
}
