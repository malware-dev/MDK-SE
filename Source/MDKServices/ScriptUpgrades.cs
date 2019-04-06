using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;

namespace Malware.MDKServices
{
    /// <summary>
    /// A service determined to analyze a script project for problems, and repair them if necessary (and possible).
    /// </summary>
    public class ScriptUpgrades
    {
        int _busyCount;

        /// <summary>
        /// Fired whenever the <see cref="IsBusy"/> property changes.
        /// </summary>
        public event EventHandler IsBusyChanged;

        /// <summary>
        /// Determines whether the service is currently busy working.
        /// </summary>
        public bool IsBusy => _busyCount > 0;

        /// <summary>
        /// Called to begin a work load block. Manages the <see cref="IsBusy"/> property and <see cref="IsBusyChanged"/> event.
        /// </summary>
        protected void BeginBusy()
        {
            _busyCount++;
            if (_busyCount == 1)
                IsBusyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called to end a work load block. Manages the <see cref="IsBusy"/> property and <see cref="IsBusyChanged"/> event.
        /// </summary>
        protected void EndBusy()
        {
            if (_busyCount == 0)
                return;
            _busyCount--;
            if (_busyCount == 0)
                IsBusyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Analyzes a single project for problems.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult> AnalyzeAsync(Project project, ScriptUpgradeAnalysisOptions options)
        {
            BeginBusy();
            try
            {
                return await Task.Run(() =>
                {
                    var result = AnalyzeProject(project, options);
                    if (!result.IsScriptProject)
                        return ScriptSolutionAnalysisResult.NoScriptProjectsResult;
                    if (!result.IsValid)
                        return new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty.Add(result));
                    else
                        return new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty);
                });
            }
            finally
            {
                EndBusy();
            }
        }

        /// <summary>
        /// Analyzes a solution for script projects and potentially their problems.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult> AnalyzeAsync(Solution solution, ScriptUpgradeAnalysisOptions options)
        {
            BeginBusy();
            try
            {
                var results = (await Task.WhenAll(solution.Projects.Cast<Project>().Select(project => Task.Run(() => AnalyzeProject(project, options)))))
                    .Where(a => a.IsScriptProject)
                    .ToArray();
                if (!results.Any())
                    return ScriptSolutionAnalysisResult.NoScriptProjectsResult;
                return new ScriptSolutionAnalysisResult(results.Where(r => !r.IsValid).ToImmutableArray());
            }
            finally
            {
                EndBusy();
            }
        }

        ScriptProjectAnalysisResult AnalyzeProject(Project project, ScriptUpgradeAnalysisOptions options)
        {
            if (!project.IsLoaded())
                return ScriptProjectAnalysisResult.NotAScriptProject;
            var projectInfo = MDKProjectProperties.Load(project.FullName, project.Name);
            if (!projectInfo.IsValid)
                return ScriptProjectAnalysisResult.NotAScriptProject;

            if (projectInfo.Options.Version < new Version(1, 2))
                return ScriptProjectAnalysisResult.For(project, projectInfo).AsLegacyProject();

            return ScriptProjectAnalysisResult.For(project, projectInfo);
        }
    }
}
