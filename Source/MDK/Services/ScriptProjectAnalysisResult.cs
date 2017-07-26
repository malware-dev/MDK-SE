using System;
using System.Collections.Immutable;
using System.Xml.Linq;
using EnvDTE;

namespace MDK.Services
{
    /// <summary>
    /// Represents the results for a single project of an analysis made by <see cref="ScriptUpgrades.Analyze(Project, Version)"/>.
    /// </summary>
    public class ScriptProjectAnalysisResult
    {
        /// <summary>
        /// Represents the results of an analysis which was ignored and should be disregarded.
        /// </summary>
        public static readonly ScriptProjectAnalysisResult Ignored = new ScriptProjectAnalysisResult();

        ScriptProjectAnalysisResult()
        {
            IsIgnored = true;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ScriptProjectAnalysisResult"/>
        /// </summary>
        /// <param name="projectInfo">Basic information about the analyzed project</param>
        /// <param name="projectDocument">The source XML document of the project file</param>
        /// <param name="badReferences">A list of bad file- or assembly references</param>
        public ScriptProjectAnalysisResult(ProjectScriptInfo projectInfo, XDocument projectDocument, ImmutableArray<BadReference> badReferences)
        {
            ProjectInfo = projectInfo;
            ProjectDocument = projectDocument;
            BadReferences = badReferences;
            IsValid = BadReferences.Length == 0;
        }

        /// <summary>
        /// This analysis was ignored and should be disregarded.
        /// </summary>
        public bool IsIgnored { get; set; }

        /// <summary>
        /// Basic information about the analyzed project.
        /// </summary>
        public ProjectScriptInfo ProjectInfo { get; }

        /// <summary>
        /// The source XML document of the project file.
        /// </summary>
        public XDocument ProjectDocument { get; }

        /// <summary>
        /// Returns a list of bad file- or assembly references.
        /// </summary>
        public ImmutableArray<BadReference> BadReferences { get; }

        /// <summary>
        /// Determines whether the analyzed project is fully valid and do not require any updates.
        /// </summary>
        public bool IsValid { get; }
    }
}
