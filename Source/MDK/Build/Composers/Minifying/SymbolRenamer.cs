using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build.Solution;
using MDK.Build.UsageAnalysis;
using Microsoft.CodeAnalysis.Rename;

namespace MDK.Build.Composers.Minifying
{
    partial class SymbolRenamer
    {
        //static char[] GetSymbolChars()
        //{
        //    var chars = new List<char>(50000);
        //    for (var u = 0; u <= ushort.MaxValue; u++)
        //    {
        //        var ch = (char)u;
        //        if (IsDeniedCharacter(ch))
        //            continue;
        //        if (char.IsLetter(ch))
        //            chars.Add(ch);
        //    }
        //    return chars.ToArray();
        //}

        //static bool IsDeniedCharacter(char ch)
        //{
        //    switch (ch)
        //    {
        //        case '\u180E':
        //        case '\u0600':
        //        case '\u00AD':
        //            return true;
        //    }

        //    return false;
        //}

        //static readonly char[] BaseNChars = GetSymbolChars();

        public async Task<ProgramComposition> ProcessAsync([NotNull] ProgramComposition composition, [NotNull] ProjectScriptInfo config)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var analyzer = new SymbolAnalyzer();
            var symbolDefinitions = analyzer.FindSymbols(composition, config).ToList();
            symbolDefinitions.Sort((a, b) => a.SyntaxNode.FullSpan.Start);
            var distinctSymbolNames = new HashSet<string>(symbolDefinitions
                .Where(s => !s.IsProtected)
                .Select(s => s.Symbol.Name)
                .Distinct());
            var symbolSrc = 0;
            var minifiedSymbolNames = distinctSymbolNames
                .ToDictionary(n => n, n => GenerateNewName(distinctSymbolNames, ref symbolSrc));

            while (true)
            {
                SymbolDefinitionInfo definition = null;
                string newName = null;
                foreach (var symbolDefinition in symbolDefinitions)
                {
                    if (symbolDefinition.IsProtected)
                        continue;
                    if (!minifiedSymbolNames.TryGetValue(symbolDefinition.Symbol.Name, out newName))
                        continue;
                    definition = symbolDefinition;
                    break;
                }
                if (definition == null)
                    break;

                var document = composition.Document;
                var documentId = document.Id;
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, definition.Symbol, newName, document.Project.Solution.Options);
                composition = await composition.WithDocumentAsync(newSolution.GetDocument(documentId));
                symbolDefinitions = analyzer.FindSymbols(composition, config).ToList();
                symbolDefinitions.Sort((a, b) => a.SyntaxNode.FullSpan.Start);
            }

            return composition;
        }

        static string GenerateNewName(HashSet<string> distinctSymbolNames, ref int symbolSrc)
        {
            string name;
            do
            {
                name = symbolSrc.ToNBaseString(BaseNChars);
                symbolSrc++;
            }
            while (distinctSymbolNames.Contains(name));

            return name;
        }
    }

}
