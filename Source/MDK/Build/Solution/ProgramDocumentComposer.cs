using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Annotations;
using MDK.Build.DocumentAnalysis;
using MDK.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build.Solution
{
    /// <summary>
    /// Composes a script solution into a single <c>Program</c> document
    /// </summary>
    public class ProgramDocumentComposer
    {
        static string DirectoryOf(TextDocument document)
        {
            return Path.GetDirectoryName(document.FilePath);
        }

        static string NameOf(TextDocument document)
        {
            return Path.GetFileNameWithoutExtension(document.FilePath);
        }

        static string ExtensionOf(TextDocument document)
        {
            return (Path.GetExtension(document.FilePath) ?? "").Trim('.');
        }

        readonly DocumentAnalyzer _analyzer = new DocumentAnalyzer();
        readonly IComparer<ScriptPart> _partComparer = new WeightedPartSorter();

        /// <summary>
        /// A dictionary of macros to replace during the composition process.
        /// </summary>
        public Dictionary<string, string> Macros { get; }
            = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                ["$MDK_DATETIME$"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ["$MDK_DATE$"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["$MDK_TIME$"] = DateTime.Now.ToString("HH:mm")
            };

        /// <summary>
        /// Composes a script solution into a single <c>Program</c> document
        /// </summary>
        /// <param name="project"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<ProgramComposition> ComposeAsync(Project project, MDKProjectProperties config)
        {
            var content = await LoadContentAsync(project, config).ConfigureAwait(false);
            var document = CreateProgramDocument(project, content);
            return await ProgramComposition.CreateAsync(document, content.Readme).ConfigureAwait(false);
        }

        bool IsDebugDocument(string filePath, MDKProjectProperties config)
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            if (fileName.Contains(".NETFramework,Version="))
                return true;

            if (fileName.EndsWith(".debug", StringComparison.CurrentCultureIgnoreCase))
                return true;

            if (fileName.IndexOf(".debug.", StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;

            if (filePath.Contains("Bootstrapper.cs"))
            {
                Debugger.Break();
            }

            return config.IsIgnoredFilePath(filePath);
        }

        async Task<ProjectContent> LoadContentAsync(Project project, MDKProjectProperties config)
        {
            var usingDirectives = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
            var parts = ImmutableArray.CreateBuilder<ScriptPart>();
            var documents = project.Documents
                .Where(document => !IsDebugDocument(document.FilePath, config))
                .ToList();

            var readmeDocuments = project.AdditionalDocuments
                .Where(document => DirectoryOf(document)?.Equals(Path.GetDirectoryName(project.FilePath), StringComparison.CurrentCultureIgnoreCase) ?? false)
                .Where(document => ExtensionOf(document).Equals("readme", StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(NameOf)
                .ToList();

            var readMe = new StringBuilder();
            for (int i = 0, n = readmeDocuments.Count - 1; i <= n; i++)
            {
                var document = readmeDocuments[i];
                if (i > 0)
                    readMe.Append('\n');
                readMe.Append(await LoadReadMeContentsAsync(document));
            }
            documents.RemoveAll(d => readmeDocuments.Contains(d));
            WrapAsComment(readMe);

            var legacyReadmeDocument = project.Documents
                .Where(document => DirectoryOf(document)?.Equals(Path.GetDirectoryName(project.FilePath), StringComparison.CurrentCultureIgnoreCase) ?? false)
                .FirstOrDefault(document => NameOf(document).Equals("readme", StringComparison.CurrentCultureIgnoreCase));
            if (legacyReadmeDocument != null)
            {
                documents.Remove(legacyReadmeDocument);
                if (readMe.Length > 0)
                    readMe.Append('\n');
                readMe.Append(await LoadReadMeContentsAsync(legacyReadmeDocument));
            }

            readmeDocuments.Add(legacyReadmeDocument);

            for (var index = 0; index < documents.Count; index++)
            {
                var document = documents[index];
                var result = await _analyzer.AnalyzeAndTransformAsync(document, Macros).ConfigureAwait(false);
                if (result == null)
                    continue;
                usingDirectives.AddRange(result.UsingDirectives);
                parts.AddRange(result.Parts);
            }

            var comparer = new UsingDirectiveComparer();
            return new ProjectContent(usingDirectives.Distinct(comparer).ToImmutableArray(), parts.ToImmutable(), readMe.Length > 0? readMe.ToString() : null);
        }

        void WrapAsComment(StringBuilder readMe)
        {
            if (readMe.Length == 0)
                return;
            readMe.Insert(0, "/*\n");
            readMe.Replace("\n", "\n * ");
            readMe.Length--;
            readMe.Append("/\n");
        }

        async Task<string> LoadReadMeContentsAsync(TextDocument document)
        {
            string readme = null;
            readme = (await document.GetTextAsync()).ToString().Replace("\r\n", "\n").TrimEnd(' ');
            if (string.IsNullOrWhiteSpace(readme))
                return null;
            if (!readme.EndsWith("\n"))
                readme += "\n";
            return readme;
        }

        Document CreateProgramDocument(Project project, ProjectContent content)
        {
            try
            {
                var solution = project.Solution;

                var programDeclaration =
                    SyntaxFactory.ClassDeclaration("Program")
                        .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithBaseList(SyntaxFactory.BaseList(
                            SyntaxFactory.SeparatedList<BaseTypeSyntax>()
                                .Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("MyGridProgram"))
                                )))
                        .NormalizeWhitespace("", "\n");

                var pendingTrivia = new List<SyntaxTrivia>();
                var programParts = content.Parts.OfType<ProgramScriptPart>().OrderBy(part => part, _partComparer).ToArray();
                var members = new List<MemberDeclarationSyntax>();
                foreach (var part in programParts)
                {
                    pendingTrivia.AddRange(part.GetLeadingTrivia());

                    var nodes = part.Content().ToArray();
                    if (pendingTrivia.Count > 0)
                    {
                        var firstNode = nodes.FirstOrDefault();
                        if (firstNode != null)
                        {
                            nodes[0] = firstNode.WithLeadingTrivia(pendingTrivia.Concat(firstNode.GetLeadingTrivia()));
                            pendingTrivia.Clear();
                        }
                    }

                    pendingTrivia.AddRange(part.GetTrailingTrivia());
                    if (pendingTrivia.Count > 0)
                    {
                        var lastNode = nodes.LastOrDefault();
                        if (lastNode != null)
                        {
                            nodes[nodes.Length - 1] = lastNode.WithTrailingTrivia(pendingTrivia.Concat(lastNode.GetTrailingTrivia()));
                            pendingTrivia.Clear();
                        }
                    }

                    members.AddRange(nodes.Select(node => node.Unindented(2)));
                }

                programDeclaration = programDeclaration.WithMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(members));

                var extensionDeclarations = content.Parts
                    .OfType<ExtensionScriptPart>()
                    .OrderBy(part => part, _partComparer)
                    .Select(p => p.PartRoot
                        .Unindented(1))
                    .Cast<MemberDeclarationSyntax>()
                    .ToArray();

                if (extensionDeclarations.Any())
                {
                    programDeclaration = programDeclaration
                        .WithCloseBraceToken(
                            programDeclaration.CloseBraceToken
                                .WithLeadingTrivia(SyntaxFactory.EndOfLine("\n"))
                                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"), SyntaxFactory.EndOfLine("\n")
                                )
                        );
                }

                var compilationProject = solution.AddProject("__ScriptCompilationProject", "__ScriptCompilationProject.dll", LanguageNames.CSharp)
                    .WithCompilationOptions(project.CompilationOptions)
                    .WithMetadataReferences(project.MetadataReferences);

                var unit = SyntaxFactory.CompilationUnit()
                    .WithUsings(SyntaxFactory.List(content.UsingDirectives))
                    .AddMembers(programDeclaration)
                    .AddMembers(extensionDeclarations);

                return compilationProject.AddDocument("Program.cs", unit);
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_CreateProgramDocument_Error, project.FilePath), e);
            }
        }
    }
}
