using System.Collections.Immutable;
using EnvDTE;

namespace MDK.Services
{
    /// <summary>
    /// Represents the results of an analysis made by <see cref="ScriptUpgrades.Analyze(Project,ScriptUpgradeAnalysisOptions,MDKPackage)"/>.
    /// </summary>
    public class ScriptSolutionAnalysisResult
    {
        /// <summary>
        /// Gets the results of a solution which has no script projects at all.
        /// </summary>
        public static readonly ScriptSolutionAnalysisResult NoScriptProjectsResult = new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty) {HasScriptProjects = false};

        /// <summary>
        /// Creates a new instance of <see cref="ScriptSolutionAnalysisResult"/>
        /// </summary>
        /// <param name="badProjects"></param>
        public ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult> badProjects)
        {
            BadProjects = badProjects;
            IsValid = BadProjects.Length == 0;
            HasScriptProjects = true;
        }

        /// <summary>
        /// Determines whether this solution has any script projects.
        /// </summary>
        public bool HasScriptProjects { get; protected set; }

        /// <summary>
        /// Determines whether all analyzed projects in the solution are fully valid and do not require any updates.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Contains a list of projects in need of updating.
        /// </summary>
        public ImmutableArray<ScriptProjectAnalysisResult> BadProjects { get; }
    }
}
