using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build.Composers;
using MDK.Build.Composers.Default;
using MDK.Build.Composers.Minifying;
using MDK.Build.Solution;
using MDK.Build.TypeTrimming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Task = System.Threading.Tasks.Task;

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
        /// <param name="solutionFileName"></param>
        /// <param name="selectedProjectFullName"></param>
        /// <param name="progress"></param>
        public BuildModule([NotNull] string solutionFileName, string selectedProjectFullName = null, IProgress<float> progress = null)
        {
            _progress = progress;
            SolutionFileName = Path.GetFullPath(solutionFileName ?? throw new ArgumentNullException(nameof(solutionFileName)));
            SelectedProjectFileName = selectedProjectFullName != null ? Path.GetFullPath(selectedProjectFullName) : null;
            SynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// The synchronization context the service will use to invoke any callbacks, as it runs asynchronously.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// The file name of the solution to build
        /// </summary>
        public string SolutionFileName { get; }

        /// <summary>
        /// The file name of the specific project to build, or <c>null</c> if the entire solution should be built
        /// </summary>
        public string SelectedProjectFileName { get; }

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
                //FireProgressChange();
            }
        }

        /// <summary>
        /// The total number of steps to reach before the build is complete.
        /// </summary>
        protected int TotalSteps { get; private set; }

        async Task<ProgramComposition> ComposeDocumentAsync(Project project, MDKProjectProperties config)
        {
            var documentComposer = new ProgramDocumentComposer();
            return await documentComposer.ComposeAsync(project, config).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the build.
        /// </summary>
        /// <returns>The number of deployed projects</returns>
        public Task<MDKProjectProperties[]> RunAsync()
        {
            return Task.Run(async () =>
            {
                var scriptProjects = _scriptProjects ?? await LoadScriptProjectsAsync();
                var builtScripts = (await Task.WhenAll(scriptProjects.Select(BuildAsync)).ConfigureAwait(false))
                    .Where(item => item != null)
                    .ToArray();
                _scriptProjects = null;
                return builtScripts;
            });
        }

        async Task<Project[]> LoadScriptProjectsAsync()
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(SolutionFileName);
            var result = solution.Projects.ToArray();
            TotalSteps = result.Length * 3;
            return result;
        }

        async Task<MDKProjectProperties> BuildAsync(Project project)
        {
            var config = LoadConfig(project);
            if (!config.IsValid)
                return null;

            if (SelectedProjectFileName != null)
            {
                if (!string.Equals(config.FileName, SelectedProjectFileName, StringComparison.CurrentCultureIgnoreCase))
                    return null;
            }

            if (config.Options.ExcludeFromDeployAll)
            {
                return null;
            }

            var composition = await ComposeDocumentAsync(project, config);
            Steps++;

            if (config.Options.TrimTypes)
            {
                var processor = new TypeTrimmer();
                composition = await processor.ProcessAsync(composition, config);
            }
            ScriptComposer composer;
            switch (config.Options.MinifyLevel)
            {
                case MinifyLevel.Full: composer = new MinifyingComposer(); break;
                case MinifyLevel.StripComments: composer = new StripCommentsComposer(); break;
                case MinifyLevel.Lite: composer = new LiteComposer(); break;
                default: composer = new DefaultComposer(); break;
            }
            var script = await ComposeScriptAsync(composition, composer, config).ConfigureAwait(false);
            Steps++;

            if (composition.Readme != null)
            {
                script = composition.Readme + script;
            }

            WriteScript(project, config.Paths.OutputPath, script);
            Steps++;
            return config;
        }

        async Task<string> ComposeScriptAsync(ProgramComposition composition, ScriptComposer composer, MDKProjectProperties config)
        {
            var script = await composer.GenerateAsync(composition, config).ConfigureAwait(false);
            return script;
        }

        void WriteScript(Project project, string output, string script)
        {
            var outputInfo = new DirectoryInfo(ExpandMacros(project, Path.Combine(output, project.Name)));
            if (!outputInfo.Exists)
                outputInfo.Create();
            File.WriteAllText(Path.Combine(outputInfo.FullName, "script.cs"), script.Replace("\r\n", "\n"), Encoding.UTF8);

            var thumbFile = new FileInfo(Path.Combine(Path.GetDirectoryName(project.FilePath) ?? ".", "thumb.png"));
            if (thumbFile.Exists)
                thumbFile.CopyTo(Path.Combine(outputInfo.FullName, "thumb.png"), true);
        }

        MDKProjectProperties LoadConfig(Project project)
        {
            return MDKProjectProperties.Load(project.FilePath, project.Name);
        }

        public static string ExpandMacros(Project project, string input)
        {
            var replacements = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
            {
                ["$(projectname)"] = project.Name ?? ""
            };
            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
                replacements[$"%{envVar.Key}%"] = (string)envVar.Value;
            return Regex.Replace(input, @"\$\(ProjectName\)|%[^%]+%", match =>
            {
                if (replacements.TryGetValue(match.Value, out var value))
                {
                    return value;
                }

                return match.Value;
            }, RegexOptions.IgnoreCase);
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
