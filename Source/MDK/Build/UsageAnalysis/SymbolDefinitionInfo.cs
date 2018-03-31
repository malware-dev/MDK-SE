using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace MDK.Build.UsageAnalysis
{
    /// <summary>
    /// Contains information about a given symbol definition
    /// </summary>
    public class SymbolDefinitionInfo
    {
        /// <summary>
        /// Creates a new <see cref="SymbolDefinitionInfo"/>
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="syntaxNode"></param>
        /// <param name="isProtected"></param>
        /// <param name="usage"></param>
        public SymbolDefinitionInfo(ISymbol symbol, SyntaxNode syntaxNode, bool isProtected = false, ImmutableArray<ReferencedSymbol> usage = default(ImmutableArray<ReferencedSymbol>))
        {
            FullName = symbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName);
            Symbol = symbol;
            SyntaxNode = syntaxNode;
            IsProtected = isProtected;
            Usage = usage;
        }

        /// <summary>
        /// The name of this symbol (no namespace, but includes nested type parents)
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// The defined symbol 
        /// </summary>
        public ISymbol Symbol { get; }

        /// <summary>
        /// The syntax node representing the definition of this symbol in the source
        /// </summary>
        public SyntaxNode SyntaxNode { get; }

        /// <summary>
        /// Whether this symbol is protected from changes
        /// </summary>
        public bool IsProtected { get; }

        /// <summary>
        /// Optional usage information
        /// </summary>
        public ImmutableArray<ReferencedSymbol> Usage { get; }

        /// <summary>
        /// Creates a protected copy of this symbol definition
        /// </summary>
        /// <returns></returns>
        public SymbolDefinitionInfo AsProtected()
        {
            return new SymbolDefinitionInfo(Symbol, SyntaxNode, true, Usage);
        }

        /// <summary>
        /// Creates an unprotected copy of this symbol definition
        /// </summary>
        /// <returns></returns>
        public SymbolDefinitionInfo AsUnprotected()
        {
            return new SymbolDefinitionInfo(Symbol, SyntaxNode, false, Usage);
        }

        /// <summary>
        /// Creates a copy of this symbol definition with the given symbol usage
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public SymbolDefinitionInfo WithUsageData(ImmutableArray<ReferencedSymbol> usage)
        {
            return new SymbolDefinitionInfo(Symbol, SyntaxNode, IsProtected, usage);
        }
    }
}