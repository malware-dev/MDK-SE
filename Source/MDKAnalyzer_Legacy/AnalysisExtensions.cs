using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Malware.MDKAnalyzer
{
    /// <summary>
    ///     Contains various utilities used by the scripting engine.
    /// </summary>
    static class AnalysisExtensions
    {
        public static ISymbol GetOverriddenSymbol(this ISymbol symbol)
        {
            if (!symbol.IsOverride)
                return null;
            if (symbol is ITypeSymbol typeSymbol)
                return typeSymbol.BaseType;
            if (symbol is IEventSymbol eventSymbol)
                return eventSymbol.OverriddenEvent;
            if (symbol is IPropertySymbol propertySymbol)
                return propertySymbol.OverriddenProperty;
            if (symbol is IMethodSymbol methodSymbol)
                return methodSymbol.OverriddenMethod;
            return null;
        }

        public static bool IsMemberSymbol(this ISymbol symbol)
        {
            return (symbol is IEventSymbol || symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IMethodSymbol);
        }

        public static BaseMethodDeclarationSyntax WithBody(this BaseMethodDeclarationSyntax item, BlockSyntax body)
        {
            if (item is ConstructorDeclarationSyntax cons)
            {
                return cons.WithBody(body);
            }
            if (item is ConversionOperatorDeclarationSyntax conv)
            {
                return conv.WithBody(body);
            }
            if (item is DestructorDeclarationSyntax dest)
            {
                return dest.WithBody(body);
            }
            if (item is MethodDeclarationSyntax meth)
            {
                return meth.WithBody(body);
            }
            if (item is OperatorDeclarationSyntax oper)
            {
                return oper.WithBody(body);
            }
            throw new ArgumentException("Unknown " + typeof(BaseMethodDeclarationSyntax).FullName, nameof(item));
        }

        public static AnonymousFunctionExpressionSyntax WithBody(this AnonymousFunctionExpressionSyntax item, CSharpSyntaxNode body)
        {
            if (item is AnonymousMethodExpressionSyntax anon)
            {
                return anon.WithBody(body);
            }
            if (item is ParenthesizedLambdaExpressionSyntax plam)
            {
                return plam.WithBody(body);
            }
            if (item is SimpleLambdaExpressionSyntax slam)
            {
                return slam.WithBody(body);
            }
            throw new ArgumentException("Unknown " + typeof(AnonymousFunctionExpressionSyntax).FullName, nameof(item));
        }

        public static bool IsInSource(this ISymbol symbol)
        {
            for (var i = 0; i < symbol.Locations.Length; i++)
            {
                if (!symbol.Locations[i].IsInSource)
                {
                    return false;
                }
            }
            return true;
        }
    }
}