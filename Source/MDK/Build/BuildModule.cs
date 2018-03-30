using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build.Minifier;
using MDK.Build.Solution;
using MDK.Build.TypeTrimming;
using MDK.Build.UsageAnalysis;
using MDK.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MDK.Build
{
    /// <summary>
    /// A service designed to combine C# class files into a coherent Space Engineers script.
    /// </summary>
    public class BuildModule
    {
        static async Task<ProgramComposition> ComposeDocument(Project project, ProjectScriptInfo config)
        {
            try
            {
                var documentComposer = new ProgramDocumentComposer();
                return await documentComposer.ComposeAsync(project, config).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_LoadContent_Error, project.FilePath), e);
            }
        }

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

            var composition = await ComposeDocument(project, config);
            Steps++;

            if (config.TrimTypes)
            {
                var processor = new TypeTrimmer();
                composition = await processor.Process(composition, config);
            }

            var composer = config.Minify ? (ScriptComposer)new MinifyingComposer() : new SimpleComposer();
            var script = await ComposeScript(composition, composer, config).ConfigureAwait(false);
            Steps++;

            if (composition.Readme != null)
            {
                script = composition.Readme + script;
            }

            WriteScript(project, config.OutputPath, script);
            Steps++;
            return config;
        }

        async Task<string> ComposeScript(ProgramComposition composition, ScriptComposer composer, ProjectScriptInfo config)
        {
            try
            {
                var script = await composer.Generate(composition, config).ConfigureAwait(false);
                return script;
            }
            catch (Exception e)
            {
                throw new BuildException(string.Format(Text.BuildModule_GenerateScript_ErrorGeneratingScript, composition.Document.Project.FilePath), e);
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
