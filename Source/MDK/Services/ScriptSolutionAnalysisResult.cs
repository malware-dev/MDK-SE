using System;
using System.Collections.Immutable;
using EnvDTE;

namespace MDK.Services
{
    /// <summary>
    /// Represents the results of an analysis made by <see cref="ScriptUpgrades.Analyze(Solution, Version)"/>.
    /// </summary>
    public class ScriptSolutionAnalysisResult
    {
        /// <summary>
        /// Contains a list of projects in need of updating.
        /// </summary>
        public ImmutableArray<ScriptProjectAnalysisResult> BadProjects { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ScriptSolutionAnalysisResult"/>
        /// </summary>
        /// <param name="badProjects"></param>
        public ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult> badProjects)
        {
            BadProjects = badProjects;
            IsValid = BadProjects.Length == 0;
        }

        /// <summary>
        /// Determines whether the analyzed solution is fully valid and do not require any updates.
        /// </summary>
        public bool IsValid { get; set; }
    }
}