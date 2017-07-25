using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Malware.MDKAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ScriptAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor ProhibitedMemberRule
            = new DiagnosticDescriptor("ProhibitedMemberRule", "Prohibited Type Or Member", "The type or member '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor ProhibitedLanguageElementRule
            = new DiagnosticDescriptor("ProhibitedLanguageElement", "Prohibited Language Element", "The language element '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        Whitelist _whitelist = new Whitelist();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ProhibitedMemberRule, ProhibitedLanguageElementRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(LoadWhitelist);
            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName);
        }

        void LoadWhitelist(CompilationStartAnalysisContext context)
        {
            var whitelistCache = context.Options.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("whitelist.cache", StringComparison.CurrentCultureIgnoreCase));
            if (whitelistCache != null)
            {
                var content = whitelistCache.GetText(context.CancellationToken);
                _whitelist.Load(content.Lines.Select(l => l.ToString()).ToArray());
            }

            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName);
            context.RegisterCompilationEndAction(EndAnalysis);
        }

        void EndAnalysis(CompilationAnalysisContext context)
        {
            //_whitelist.Clear();
        }

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;

            if (IsIgnorableNode(context))
                return;

            if (_whitelist.IsEmpty())
                return;

            // The exception finally clause cannot be allowed ingame because it can be used
            // to circumvent the instruction counter exception and crash the game
            if (node.Kind() == SyntaxKind.FinallyClause)
            {
                var kw = ((FinallyClauseSyntax)node).FinallyKeyword;
                var diagnostic = Diagnostic.Create(ProhibitedLanguageElementRule, kw.GetLocation(), kw.ToString());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // We'll check the qualified names on their own.
            if (IsQualifiedName(node.Parent))
            {
                //if (node.Ancestors().Any(IsQualifiedName))
                return;
            }

            var info = context.SemanticModel.GetSymbolInfo(node);
            if (info.Symbol == null)
            {
                return;
            }

            // If they wrote it, they can have it.
            if (info.Symbol.IsInSource())
            {
                return;
            }

            if (!_whitelist.IsWhitelisted(info.Symbol))
            {
                var diagnostic = Diagnostic.Create(ProhibitedMemberRule, node.GetLocation(), info.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                context.ReportDiagnostic(diagnostic);
            }
        }

        bool IsIgnorableNode(SyntaxNodeAnalysisContext context)
        {
            var fileName = Path.GetFileName(context.Node.SyntaxTree.FilePath);
            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            if (fileName.Contains(".NETFramework,Version="))
                return true;

            if (fileName.EndsWith(".debug", StringComparison.CurrentCultureIgnoreCase))
                return true;

            if (fileName.IndexOf(".debug.", StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            return false;
        }

        bool IsQualifiedName(SyntaxNode arg)
        {
            switch (arg.Kind())
            {
                case SyntaxKind.QualifiedName:
                case SyntaxKind.AliasQualifiedName:
                    return true;
            }
            return false;
        }
    }
}