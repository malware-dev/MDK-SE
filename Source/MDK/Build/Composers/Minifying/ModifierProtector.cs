using System.Collections.Generic;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;
using MDK.Build.UsageAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build.Composers.Minifying
{
    class ModifierProtector : CSharpSyntaxRewriter
    {
        HashSet<string> _externallyReferencedTypes = new HashSet<string>();

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            _externallyReferencedTypes.Clear();
            var usageAnalyzer = new UsageAnalyzer();
            var typeUsages = await usageAnalyzer.FindUsagesAsync(composition, config);
            foreach (var typeUsage in typeUsages)
            {
                foreach (var part in typeUsage.Usage)
                {
                    foreach (var location in part.Locations)
                    {
                        var node = composition.RootNode.FindNode(location.Location.SourceSpan);
                        if (IsInProgram(node))
                            _externallyReferencedTypes.Add(typeUsage.FullName);
                    }
                }
            }

            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }

        bool IsInProgram(SyntaxNode node)
        {
            while (!(node is CompilationUnitSyntax))
            {
                if (node is ClassDeclarationSyntax classDeclaration && classDeclaration.GetFullName() == "Program")
                    return true;
                node = node.Parent;
            }

            return false;
        }
    }
}
