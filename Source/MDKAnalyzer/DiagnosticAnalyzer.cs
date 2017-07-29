using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
        List<Uri> _ignoredFolders = new List<Uri>();
        List<Uri> _ignoredFiles = new List<Uri>();
        Uri _basePath;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ProhibitedMemberRule, ProhibitedLanguageElementRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(LoadWhitelist);
        }  

        void LoadWhitelist(CompilationStartAnalysisContext context)
        {
            var mdkOptions = context.Options.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("mdk.options", StringComparison.CurrentCultureIgnoreCase));
            if (mdkOptions != null)
                LoadOptions(context, mdkOptions);

            var whitelistCache = context.Options.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("whitelist.cache", StringComparison.CurrentCultureIgnoreCase));
            if (whitelistCache != null)
            {
                var content = whitelistCache.GetText(context.CancellationToken);
                _whitelist.IsEnabled = true;
                _whitelist.Load(content.Lines.Select(l => l.ToString()).ToArray());
            }
            else
                _whitelist.IsEnabled = false;

            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName);
        }

#pragma warning disable RS1012 // Start action has no registered actions.
        void LoadOptions(CompilationStartAnalysisContext context, AdditionalText mdkOptions)
#pragma warning restore RS1012 // Start action has no registered actions.
        {
            var content = mdkOptions.GetText(context.CancellationToken);
            var document = XDocument.Parse(content.ToString());
            var ignoredFolders = document.Element("mdk")?.Elements("ignore").SelectMany(e => e.Elements("folder"));
            var ignoredFiles = document.Element("mdk")?.Elements("ignore").SelectMany(e => e.Elements("file")).ToArray();
            var basePath = Path.GetDirectoryName(mdkOptions.Path).TrimEnd('\\') + "\\..\\";
            if (!basePath.EndsWith("\\"))
                basePath += "\\";
            _basePath = new Uri(basePath);

            _ignoredFolders.Clear();
            _ignoredFiles.Clear();
            if (ignoredFolders != null)
            {
                foreach (var folderElement in ignoredFolders)
                {
                    var folder = folderElement.Value;
                    if (!folder.EndsWith("\\"))
                        _ignoredFolders.Add(new Uri(_basePath, new Uri(folder + "\\", UriKind.RelativeOrAbsolute)));
                    else
                        _ignoredFolders.Add(new Uri(_basePath, new Uri(folder, UriKind.RelativeOrAbsolute)));
                }
            }
            if (ignoredFiles != null)
            {
                foreach (var fileElement in ignoredFiles)
                {
                    var file = fileElement.Value;
                    _ignoredFiles.Add(new Uri(_basePath, new Uri(file, UriKind.RelativeOrAbsolute)));
                }
            }
        }

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (IsIgnorableNode(context))
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
            if (!_whitelist.IsEnabled || _whitelist.IsEmpty())
                return true;

            var fileName = Path.GetFileName(context.Node.SyntaxTree.FilePath);

            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            if (fileName.Contains(".NETFramework,Version="))
                return true;

            if (fileName.EndsWith(".debug", StringComparison.CurrentCultureIgnoreCase))
                return true;

            if (fileName.IndexOf(".debug.", StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (_basePath == null)
                return false;

            var uri = new Uri(_basePath, new Uri(context.Node.SyntaxTree.FilePath, UriKind.RelativeOrAbsolute));
            foreach (var ignoredUri in _ignoredFiles)
            {
                if (string.Equals(uri.AbsolutePath, ignoredUri.AbsolutePath, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }

            foreach (var ignoredUri in _ignoredFolders)
            {
                if (uri.AbsolutePath.StartsWith(ignoredUri.AbsolutePath, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }

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