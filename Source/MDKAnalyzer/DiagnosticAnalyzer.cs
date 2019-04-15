using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Malware.MDKServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Malware.MDKAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ScriptAnalyzer : DiagnosticAnalyzer
    {
        const string DefaultNamespaceName = "IngameScript";

        internal static readonly DiagnosticDescriptor NoWhitelistCacheRule
            = new DiagnosticDescriptor("MissingWhitelistRule", "Missing Or Corrupted Whitelist Cache", "The whitelist cache could not be loaded. Please run Tools | MDK | Refresh Whitelist Cache to attempt repair.", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor NoOptionsRule
            = new DiagnosticDescriptor("MissingOptionsRule", "Missing Or Corrupted Options", "The MDK.options.props could not be loaded.", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor ProhibitedMemberRule
            = new DiagnosticDescriptor("ProhibitedMemberRule", "Prohibited Type Or Member", "The type or member '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor ProhibitedLanguageElementRule
            = new DiagnosticDescriptor("ProhibitedLanguageElement", "Prohibited Language Element", "The language element '{0}' is prohibited in Space Engineers", "Whitelist", DiagnosticSeverity.Error, true);

        internal static readonly DiagnosticDescriptor InconsistentNamespaceDeclarationRule
            = new DiagnosticDescriptor("InconsistentNamespaceDeclaration", "Inconsistent Namespace Declaration", "All ingame script code should be within the {0} namespace in order to avoid problems.", "Whitelist", DiagnosticSeverity.Warning, true);

        Whitelist _whitelist = new Whitelist();
        List<Uri> _ignoredFolders = new List<Uri>();
        List<Uri> _ignoredFiles = new List<Uri>();
        Uri _basePath;
        string _namespaceName;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } 
            = ImmutableArray.Create(
                ProhibitedMemberRule, 
                ProhibitedLanguageElementRule, 
                NoWhitelistCacheRule,
                NoOptionsRule,
                InconsistentNamespaceDeclarationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(LoadWhitelist);
        }  

        void LoadWhitelist(CompilationStartAnalysisContext context)
        {
            var mdkOptions = context.Options.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("mdk.options.props", StringComparison.CurrentCultureIgnoreCase));
            if (mdkOptions == null || !LoadOptions(context, mdkOptions))
            {
                context.RegisterSemanticModelAction(c =>
                {
                    var diagnostic = Diagnostic.Create(NoOptionsRule, c.SemanticModel.SyntaxTree.GetRoot().GetLocation());
                    c.ReportDiagnostic(diagnostic);
                });
                _whitelist.IsEnabled = false;
                return;
            }

            var whitelistCache = context.Options.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("whitelist.cache", StringComparison.CurrentCultureIgnoreCase));
            if (whitelistCache != null)
            {
                var content = whitelistCache.GetText(context.CancellationToken);
                _whitelist.IsEnabled = true;
                _whitelist.Load(content.Lines.Select(l => l.ToString()).ToArray());
            }
            else
            {
                context.RegisterSemanticModelAction(c =>
                {
                    var diagnostic = Diagnostic.Create(NoWhitelistCacheRule, c.SemanticModel.SyntaxTree.GetRoot().GetLocation());
                    c.ReportDiagnostic(diagnostic);
                });
                _whitelist.IsEnabled = false;
                return;
            }

            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.QualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName,
                SyntaxKind.DestructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeDeclaration,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.VariableDeclaration,
                SyntaxKind.Parameter);
            context.RegisterSyntaxNodeAction(AnalyzeNamespace,
                SyntaxKind.ClassDeclaration);
        }

        void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            if (IsIgnorableNode(context))
                return;
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.Parent is TypeDeclarationSyntax)
                return;

            var namespaceDeclaration = classDeclaration.Parent as NamespaceDeclarationSyntax;
            var namespaceName = namespaceDeclaration?.Name.ToString();
            if (!_namespaceName.Equals(namespaceName, StringComparison.Ordinal))
            {
                var diagnostic = Diagnostic.Create(InconsistentNamespaceDeclarationRule, 
                    namespaceDeclaration?.Name.GetLocation() ?? classDeclaration.Identifier.GetLocation(), _namespaceName);
                context.ReportDiagnostic(diagnostic);
            }
        }

#pragma warning disable RS1012 // Start action has no registered actions.
        bool LoadOptions(CompilationStartAnalysisContext context, AdditionalText mdkOptions)
#pragma warning restore RS1012 // Start action has no registered actions.
        {
            try
            {
                var content = mdkOptions.GetText(context.CancellationToken);
                var properties = MDKProjectOptions.Parse(content.ToString(), mdkOptions.Path);
                var basePath = Path.GetFullPath(Path.GetDirectoryName(mdkOptions.Path) ?? ".").TrimEnd('\\') + "\\..\\";
                if (!basePath.EndsWith("\\"))
                    basePath += "\\";

                _basePath = new Uri(basePath);
                _namespaceName = properties.Namespace ?? DefaultNamespaceName;
                lock (_ignoredFolders)
                lock (_ignoredFiles)
                {
                    _ignoredFolders.Clear();
                    _ignoredFiles.Clear();
                    foreach (var folder in properties.IgnoredFolders)
                    {
                        if (!folder.EndsWith("\\"))
                            _ignoredFolders.Add(new Uri(_basePath, new Uri(folder + "\\", UriKind.RelativeOrAbsolute)));
                        else
                            _ignoredFolders.Add(new Uri(_basePath, new Uri(folder, UriKind.RelativeOrAbsolute)));
                    }

                    foreach (var file in properties.IgnoredFiles)
                        _ignoredFiles.Add(new Uri(_basePath, new Uri(file, UriKind.RelativeOrAbsolute)));
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (IsIgnorableNode(context))
                return;

            if (!_whitelist.IsEnabled)
            {
                var diagnostic = Diagnostic.Create(NoOptionsRule, context.SemanticModel.SyntaxTree.GetRoot().GetLocation());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            IdentifierNameSyntax identifier;

            switch (node.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                    identifier = ((PropertyDeclarationSyntax)node).Type as IdentifierNameSyntax;
                    break;
                case SyntaxKind.VariableDeclaration:
                    identifier = ((VariableDeclarationSyntax)node).Type as IdentifierNameSyntax;
                    break;
                case SyntaxKind.Parameter:
                    identifier = ((ParameterSyntax)node).Type as IdentifierNameSyntax;
                    break;
                default:
                    identifier = null;
                    break;
            }
            if (identifier == null)
                return;
            var name = identifier.Identifier.ToString();
            if (name == "dynamic")
            {
                var diagnostic = Diagnostic.Create(ProhibitedLanguageElementRule, identifier.Identifier.GetLocation(), name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (IsIgnorableNode(context))
                return;

            if (!_whitelist.IsEnabled)
            {
                var diagnostic = Diagnostic.Create(NoOptionsRule, context.SemanticModel.SyntaxTree.GetRoot().GetLocation());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Destructors are unpredictable so they cannot be allowed
            if (node.Kind() == SyntaxKind.DestructorDeclaration)
            {
                var kw = ((DestructorDeclarationSyntax)node).Identifier;
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
            lock (_ignoredFiles)
            {
                foreach (var ignoredUri in _ignoredFiles)
                {
                    if (string.Equals(uri.AbsolutePath, ignoredUri.AbsolutePath, StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }

            lock (_ignoredFolders)
            {
                foreach (var ignoredUri in _ignoredFolders)
                {
                    if (uri.AbsolutePath.StartsWith(ignoredUri.AbsolutePath, StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
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