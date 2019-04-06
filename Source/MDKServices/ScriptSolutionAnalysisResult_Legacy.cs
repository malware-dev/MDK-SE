using System.Collections.Immutable;

namespace Malware.MDKServices
{
    /// <summary>
    /// Represents the results of an analysis made by <see cref="ScriptUpgrades_Legacy.Analyze"/>.
    /// </summary>
    public class ScriptSolutionAnalysisResult_Legacy
    {
        /// <summary>
        /// Gets the results of a solution which has no script projects at all.
        /// </summary>
        public static readonly ScriptSolutionAnalysisResult_Legacy NoScriptProjectsResult = new ScriptSolutionAnalysisResult_Legacy(ImmutableArray<ScriptProjectAnalysisResult_Legacy>.Empty) {HasScriptProjects = false};

        /// <summary>
        /// Creates a new instance of <see cref="ScriptSolutionAnalysisResult"/>
        /// </summary>
        /// <param name="badProjects"></param>
        public ScriptSolutionAnalysisResult_Legacy(ImmutableArray<ScriptProjectAnalysisResult_Legacy> badProjects)
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
        public ImmutableArray<ScriptProjectAnalysisResult_Legacy> BadProjects { get; }
    }
}