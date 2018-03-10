using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace MDK.Build
{
    /// <summary>
    /// A service designed to combine C# class files into a coherent Space Engineers script.
    /// </summary>
    public class BuildModule
    {
        readonly IProgress<float> _progress;
        Project[] _scriptProjects;
        int _steps;

        /// <summary>
        /// Creates a new instance of <see cref="BuildModule"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="solutionFileName"></param>
        /// <param name="selectedProjectFullName"></param>
        /// <param name="progress"></param>
        public BuildModule(MDKPackage package, [NotNull] string solutionFileName, string selectedProjectFullName = null, IProgress<float> progress = null)
        {
            _progress = progress;
            Package = package;
            SolutionFileName = Path.GetFullPath(solutionFileName ?? throw new ArgumentNullException(nameof(solutionFileName)));
            SelectedProjectFileName = selectedProjectFullName != null ? Path.GetFullPath(selectedProjectFullName) : null;
            SynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// The synchronization context the service will use to invoke any callbacks, as it runs asynchronously.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// The <see cref="MDKPackage"/>
        /// </summary>
        public MDKPackage Package { get; }

        /// <summary>
        /// The file name of the solution to build
        /// </summary>
        public string SolutionFileName { get; }

        /// <summary>
        /// The file name of the specific project to build, or <c>null</c> if the entire solution should be built
        /// </summary>
        public string SelectedProjectFileName { get; }

        /// <summary>
        /// The document analysis utility
        /// </summary>
        public DocumentAnalyzer Analyzer { get; } = new DocumentAnalyzer();

        /// <summary>
        /// The current step index for the build. Moves towards <see cref="TotalSteps"/>.
        /// </summary>
        protected int Steps
        {
            get => _steps;
            private set
            {
                if (_steps == value)
                    return;
                _steps = value;
                SynchronizationContext.Post(o => OnProgressChanged(), null);
            }
        }

        /// <summary>
        /// The total number of steps to reach before the build is complete.
        /// </summary>
        protected int TotalSteps { get; private set; }

        /// <summary>
        /// Gets the comparer used to determine the order of parts (files) when building.
        /// </summary>
        public IComparer<ScriptPart> PartComparer { get; } = new WeightedPartSorter();

        /// <summary>
        /// Starts the build.
        /// </summary>
        /// <returns>The number of deployed projects</returns>
        public Task<ProjectScriptInfo[]> Run()
        {
            return Task.Run(async () =>
            {
                var scriptProjects = _scriptProjects ?? await LoadScriptProjects();
                var builtScripts = (await Task.WhenAll(scriptProjects.Select(Build)).ConfigureAwait(false))
                    .Where(item => item != null)
                    .ToArray();
                _scriptProjects = null;
                return builtScripts;
            });
        }

        async Task<Project[]> LoadScriptProjects()
        {
            try
            {
                var workspace = MSBuildWorkspace.Create();
                var solution = await workspace.OpenSolutionAsync(SolutionFileName);
                var result = solution.Projects.ToArray();
                TotalSteps = result.Length * 3;
                return result;
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_LoadScriptProjects_Error, SolutionFileName), e);
            }
        }

        async Task<ProjectScriptInfo> Build(Project project)
        {
            var config = LoadConfig(project);
            if (!config.IsValid)
                return null;

            if (SelectedProjectFileName != null)
            {
                if (!string.Equals(config.FileName, SelectedProjectFileName, StringComparison.CurrentCultureIgnoreCase))
                    return null;
            }

            var macros = CreateMacros();

            var content = await LoadContent(project, config).ConfigureAwait(false);
            Steps++;

            var document = CreateProgramDocument(project, content, macros);

            //var minifyResult = await PreMinify(project, config, document);
            //var minifier = minifyResult.Minifier;
            //document = minifyResult.Document;

            var composer = config.Minify ? (ScriptComposer)new MinifyingComposer() : new SimpleComposer();
            var script = await Compose(project, document, composer).ConfigureAwait(false);
            Steps++;

            if (content.Readme != null)
            {
                script = content.Readme + script;
            }

            WriteScript(project, config.OutputPath, script);
            Steps++;
            return config;
        }

        IDictionary<string, string> CreateMacros()
        {
            return new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                ["%MDK_DATETIME%"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ["%MDK_DATE%"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["%MDK_TIME%"] = DateTime.Now.ToString("HH:mm"),
            };
        }

        //async Task<(Minifier Minifier, Document Document)> PreMinify(Project project, ProjectScriptInfo config, Document document)
        //{
        //    try
        //    {
        //        var minifier = config.Minify ? new Minifier() : null;
        //        if (minifier != null)
        //            document = await minifier.PreMinify(document);
        //        return (minifier, document);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new BuildException(string.Format(Text.BuildModule_PreMinify_Error, project.FilePath), e);
        //    }
        //}

        //string PostMinify(Project project, Minifier minifier, string script)
        //{
        //    try
        //    {
        //        if (minifier != null)
        //            script = minifier.PostMinify(script);
        //        return script;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new BuildException(string.Format(Text.BuildModule_PostMinify_Error, project.FilePath), e);
        //    }
        //}

        async Task<string> Compose(Project project, Document document, ScriptComposer composer)
        {
            try
            {
                var script = await composer.Generate(document).ConfigureAwait(false);
                return script;
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_GenerateScript_ErrorGeneratingScript, project.FilePath), e);
            }
        }

        void WriteScript(Project project, string output, string script)
        {
            try
            {
                var outputInfo = new DirectoryInfo(ExpandMacros(project, Path.Combine(output, project.Name)));
                if (!outputInfo.Exists)
                    outputInfo.Create();
                File.WriteAllText(Path.Combine(outputInfo.FullName, "script.cs"), script.Replace("\r\n", "\n"), Encoding.UTF8);

                var thumbFile = new FileInfo(Path.Combine(Path.GetDirectoryName(project.FilePath) ?? ".", "thumb.png"));
                if (thumbFile.Exists)
                    thumbFile.CopyTo(Path.Combine(outputInfo.FullName, "thumb.png"), true);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new UnauthorizedAccessException(string.Format(Text.BuildModule_WriteScript_UnauthorizedAccess, project.FilePath), e);
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_WriteScript_Error, project.FilePath), e);
            }
        }

        ProjectScriptInfo LoadConfig(Project project)
        {
            try
            {
                return ProjectScriptInfo.Load(project.FilePath, project.Name);
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_LoadConfig_Error, project.FilePath), e);
            }
        }

        string ExpandMacros(Project project, string input)
        {
            return Regex.Replace(input, @"\$\(ProjectName\)", match =>
            {
                switch (match.Value.ToUpper())
                {
                    case "$(PROJECTNAME)":
                        return project.Name;
                    default:
                        return match.Value;
                }
            });
        }

        Document CreateProgramDocument(Project project, ProjectContent content, IDictionary<string, string> macros)
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
                var programParts = content.Parts.OfType<ProgramScriptPart>().OrderBy(part => part, PartComparer).ToArray();
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

                var extensionDeclarations = content.Parts.OfType<ExtensionScriptPart>().OrderBy(part => part, PartComparer).Select(p => p.PartRoot.Unindented(1)).Cast<MemberDeclarationSyntax>().ToArray();

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

                var document = compilationProject.AddDocument("Program.cs", unit.WithMdkAnnotations(macros));

                return document;
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_CreateProgramDocument_Error, project.FilePath), e);
            }
        }

        async Task<ProjectContent> LoadContent(Project project, ProjectScriptInfo config)
        {
            try
            {
                var usingDirectives = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
                var parts = ImmutableArray.CreateBuilder<ScriptPart>();
                var documents = project.Documents
                    .Where(document => !IsDebugDocument(document.FilePath, config))
                    .ToList();

                var readmeDocument = project.Documents
                    .Where(document => Path.GetDirectoryName(document.FilePath)?.Equals(Path.GetDirectoryName(project.FilePath), StringComparison.CurrentCultureIgnoreCase) ?? false)
                    .FirstOrDefault(document => Path.GetFileNameWithoutExtension(document.FilePath).Equals("readme", StringComparison.CurrentCultureIgnoreCase));

                string readme = null;
                if (readmeDocument != null)
                {
                    documents.Remove(readmeDocument);
                    readme = (await readmeDocument.GetTextAsync()).ToString().Replace("\r\n", "\n");
                    if (!readme.EndsWith("\n"))
                        readme += "\n";
                }

                foreach (var document in documents)
                {
                    var result = await Analyzer.Analyze(document).ConfigureAwait(false);
                    if (result == null)
                        continue;
                    usingDirectives.AddRange(result.UsingDirectives);
                    parts.AddRange(result.Parts);
                }

                var comparer = new UsingDirectiveComparer();
                return new ProjectContent(usingDirectives.Distinct(comparer).ToImmutableArray(), parts.ToImmutable(), readme);
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_LoadContent_Error, project.FilePath), e);
            }
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

        /// <summary>
        /// Called when the current build progress changes.
        /// </summary>
        protected virtual void OnProgressChanged()
        {
            var progress = (float)Steps / TotalSteps;
            _progress?.Report(progress);
        }
    }
}
