using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MDK.Services;
using Microsoft.CodeAnalysis;
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
        /// <param name="progress"></param>
        public BuildModule(MDKPackage package, [NotNull] string solutionFileName, IProgress<float> progress = null)
        {
            _progress = progress;
            Package = package;
            SolutionFileName = Path.GetFullPath(solutionFileName ?? throw new ArgumentNullException(nameof(solutionFileName)));
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
        /// Starts the build.
        /// </summary>
        /// <returns></returns>
        public Task Run()
        {
            return Task.Run(async () =>
            {
                var scriptProjects = _scriptProjects ?? await LoadScriptProjects();
                await Task.WhenAll(scriptProjects.Select(Build)).ConfigureAwait(false);
                _scriptProjects = null;
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
                throw new BuildException($"Error loading script projects from {SolutionFileName}", e);
            }
        }

        async Task Build(Project project)
        {
            var config = LoadConfig(project);
            var content = await LoadContent(project, config).ConfigureAwait(false);
            Steps++;

            var document = CreateProgramDocument(project, content);

            var minifyResult = await PreMinify(project, config, document);
            var minifier = minifyResult.Minifier;
            document = minifyResult.Document;

            var script = await GenerateScript(project, document).ConfigureAwait(false);
            Steps++;

            script = PostMinify(project, minifier, script);

            if (content.Readme != null)
            {
                script = content.Readme + script;
            }

            WriteScript(project, config.OutputPath, script);
            Steps++;
        }

        async Task<(Minifier Minifier, Document Document)> PreMinify(Project project, ProjectScriptInfo config, Document document)
        {
            try
            {
                var minifier = config.Minify ? new Minifier() : null;
                if (minifier != null)
                    document = await minifier.PreMinify(document);
                return (minifier, document);
            }
            catch (Exception e)
            {
                throw new BuildException($"Error minifying {project.FilePath} (stage 1)", e);
            }
        }

        string PostMinify(Project project, Minifier minifier, string script)
        {
            try
            {
                if (minifier != null)
                    script = minifier.PostMinify(script);
                return script;
            }
            catch (Exception e)
            {
                throw new BuildException($"Error minifying {project.FilePath} (stage 1)", e);
            }
        }

        async Task<string> GenerateScript(Project project, Document document)
        {
            try
            {
                var generator = new ScriptGenerator();
                var script = await generator.Generate(document).ConfigureAwait(false);
                return script;
            }
            catch (Exception e)
            {
                throw new BuildException($"Error minifying {project.FilePath} (stage 1)", e);
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
            }
            catch (Exception e)
            {
                throw new BuildException($"Error writing final script for {project.FilePath}", e);
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
                throw new BuildException($"Error loading configuration for {project.FilePath}", e);
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

        Document CreateProgramDocument(Project project, ProjectContent content)
        {
            try
            {
                var usings = string.Join(Environment.NewLine, content.UsingDirectives.Select(d => d.ToString()));
                var solution = project.Solution;
                var programCode = string.Join(Environment.NewLine, content.Parts.OfType<ProgramScriptPart>().SelectMany(p => p.ContentNodes()).Select(n => n.ToString()));
                var programContent = $"public class Program: MyGridProgram {{{Environment.NewLine}{programCode}{Environment.NewLine}}}";

                var extensionContent = string.Join(Environment.NewLine, content.Parts.OfType<ExtensionScriptPart>().SelectMany(p => p.ContentNodes()).Select(n => n.ToString()));

                var finalContent = $"{usings}{Environment.NewLine}{programContent}{Environment.NewLine}{extensionContent}";

                var compilationProject = solution.AddProject("__ScriptCompilationProject", "__ScriptCompilationProject.dll", LanguageNames.CSharp)
                    .WithCompilationOptions(project.CompilationOptions)
                    .WithMetadataReferences(project.MetadataReferences);

                return compilationProject.AddDocument("Program.cs", finalContent);
            }
            catch (Exception e)
            {
                throw new BuildException($"Error generating the combined script for {project.FilePath}", e);
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
                throw new BuildException($"Error loading the content for {project.FilePath}", e);
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
