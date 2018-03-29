using System;
using System.Collections.Immutable;
using JetBrains.Annotations;
using MDK.Build.UsageAnalysis;
using Microsoft.CodeAnalysis;

namespace MDK.Build.Solution
{
    /// <summary>
    /// Represents the results of a program composition
    /// </summary>
    public class ProgramComposition
    {
        ///// <summary>
        ///// A list of symbol definitions - if defined.
        ///// </summary>
        //public ImmutableArray<SymbolDefinitionInfo> SymbolDefinitions { get; }

        /// <summary>
        /// The document representing the unified <c>Program</c>
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// A specialized Readme document
        /// </summary>
        public string Readme { get; }

        /// <summary>
        /// Creates an instance of <see cref="ProgramComposition"/>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="readme"></param>
        /// <param name="symbolDefinitions"></param>
        public ProgramComposition(Document document, string readme = null, ImmutableArray<SymbolDefinitionInfo> symbolDefinitions = default(ImmutableArray<SymbolDefinitionInfo>))
        {
            Document = document;
            Readme = readme;
            //SymbolDefinitions = symbolDefinitions;
        }

        ///// <summary>
        ///// Creates a new program composition with the given symbol definitions.
        ///// </summary>
        ///// <param name="symbolDefinitions"></param>
        ///// <returns></returns>
        //public ProgramComposition WithSymbolDefinitions(ImmutableArray<SymbolDefinitionInfo> symbolDefinitions)
        //{
        //    return new ProgramComposition(Document, Readme, symbolDefinitions);
        //}
        public ProgramComposition WithDocument([NotNull] Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return new ProgramComposition(document, Readme);
        }
    }
}