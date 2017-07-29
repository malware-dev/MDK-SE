using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;

namespace Malware.MDKAnalyzer
{
    class Whitelist
    {
        HashSet<string> _symbolKeys = new HashSet<string>();

        public bool IsEnabled { get; set; }


        public void Load(string[] symbolKeys)
        {
            _symbolKeys.Clear();
            foreach (var symbolKey in symbolKeys)
                _symbolKeys.Add(symbolKey);
        }

        public bool IsEmpty()
        {
            return _symbolKeys.Count == 0;
        }

        public bool IsWhitelisted(ISymbol symbol)
        {
            var typeSymbol = symbol as INamedTypeSymbol;
            if (typeSymbol != null)
            {
                return IsWhitelisted(typeSymbol) != TypeKeyQuantity.None;
            }

            if (symbol.IsMemberSymbol())
            {
                return IsMemberWhitelisted(symbol);
            }

            // This is not a symbol we need concern ourselves with.
            return true;
        }

        TypeKeyQuantity IsWhitelisted(INamespaceSymbol namespaceSymbol)
        {
            if (_symbolKeys.Contains(namespaceSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers)))
                return TypeKeyQuantity.AllMembers;
            return TypeKeyQuantity.None;
        }

        TypeKeyQuantity IsWhitelisted(INamedTypeSymbol typeSymbol)
        {
            var result = IsWhitelisted(typeSymbol.ContainingNamespace);
            if (result == TypeKeyQuantity.AllMembers)
            {
                return result;
            }

            if (_symbolKeys.Contains(typeSymbol.GetWhitelistKey(TypeKeyQuantity.AllMembers)))
            {
                return TypeKeyQuantity.AllMembers;
            }

            if (_symbolKeys.Contains(typeSymbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly)))
            {
                return TypeKeyQuantity.ThisOnly;
            }

            return TypeKeyQuantity.None;
        }

        bool IsMemberWhitelisted(ISymbol memberSymbol)
        {
            while (true)
            {
                var result = IsWhitelisted(memberSymbol.ContainingType);
                if (result == TypeKeyQuantity.AllMembers)
                {
                    return true;
                }

                if (_symbolKeys.Contains(memberSymbol.GetWhitelistKey(TypeKeyQuantity.ThisOnly)))
                {
                    return true;
                }

                if (memberSymbol.IsOverride)
                {
                    memberSymbol = memberSymbol.GetOverriddenSymbol();
                    if (memberSymbol != null)
                    {
                        continue;
                    }
                }

                return false;
            }
        }
    }
}
