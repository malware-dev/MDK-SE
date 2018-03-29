using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
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
        static string DirectoryOf(Document document)
        {
            return Path.GetDirectoryName(document.FilePath);
        }

        static string NameOf(Document document)
        {
            return Path.GetFileNameWithoutExtension(document.FilePath);
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
        public async Task<ProgramComposition> ComposeAsync(Project project, ProjectScriptInfo config)
        {
            var content = await LoadContent(project, config).ConfigureAwait(false);
            var document = CreateProgramDocument(project, content);
            return new ProgramComposition(document, content.Readme);
        }

        bool IsDebugDocument(string filePath, ProjectScriptInfo config)
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

            return config.IsIgnoredFilePath(filePath);
        }

        async Task<ProjectContent> LoadContent(Project project, ProjectScriptInfo config)
        {
                var usingDirectives = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
                var parts = ImmutableArray.CreateBuilder<ScriptPart>();
                var documents = project.Documents
                    .Where(document => !IsDebugDocument(document.FilePath, config))
                    .ToList();

                var readmeDocument = project.Documents
                    .Where(document => DirectoryOf(document)?.Equals(Path.GetDirectoryName(project.FilePath), StringComparison.CurrentCultureIgnoreCase) ?? false)
                    .FirstOrDefault(document => NameOf(document).Equals("readme", StringComparison.CurrentCultureIgnoreCase));

                string readme = null;
                if (readmeDocument != null)
                {
                    documents.Remove(readmeDocument);
                    readme = (await readmeDocument.GetTextAsync()).ToString().Replace("\r\n", "\n");
                    if (!readme.EndsWith("\n"))
                        readme += "\n";
                }

                for (var index = 0; index < documents.Count; index++)
                {
                    var document = documents[index];
                    var result = await _analyzer.Analyze(document).ConfigureAwait(false);
                    if (result == null)
                        continue;
                    usingDirectives.AddRange(result.UsingDirectives);
                    parts.AddRange(result.Parts);
                }

                var comparer = new UsingDirectiveComparer();
                return new ProjectContent(usingDirectives.Distinct(comparer).ToImmutableArray(), parts.ToImmutable(), readme);
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
                        .NormalizeWhitespace();
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

                var extensionDeclarations = content.Parts.OfType<ExtensionScriptPart>().OrderBy(part => part, _partComparer).Select(p => p.PartRoot.Unindented(1)).Cast<MemberDeclarationSyntax>().ToArray();

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

                var document = compilationProject.AddDocument("Program.cs", unit.TransformAndAnnotate(Macros));

                return document;
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_CreateProgramDocument_Error, project.FilePath), e);
            }
        }
    }
}
