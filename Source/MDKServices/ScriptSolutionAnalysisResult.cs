using System.Collections.Immutable;
using EnvDTE;

namespace Malware.MDKServices
{
    /// <summary>
    /// Represents the current health statistics of an MDK solution, as detected via the <see cref="ScriptUpgrades"/> service.
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

    /// <summary>
    /// Represents the current health statistics of an MDK project, as detected via the <see cref="ScriptUpgrades"/> service.
    /// </summary>
    public class ScriptProjectAnalysisResult
    {
        /// <summary>
        /// A result 
        /// </summary>
        public static readonly ScriptProjectAnalysisResult NotAScriptProject = new ScriptProjectAnalysisResult();

        public static ScriptProjectAnalysisResult For(Project project, MDKProjectProperties projectInfo)
        {
            return new ScriptProjectAnalysisResult(project, projectInfo);
        }

        ScriptProjectAnalysisResult()
        { }

        ScriptProjectAnalysisResult(Project project, MDKProjectProperties projectProperties)
        {
            Project = project;
            ProjectProperties = projectProperties;
            IsScriptProject = true;
            IsValid = true;
        }

        /// <summary>
        /// The project in question
        /// </summary>
        public Project Project { get; }

        /// <summary>
        /// The MDK project properties
        /// </summary>
        public MDKProjectProperties ProjectProperties { get; }

        /// <summary>
        /// Determines whether this is a script project.
        /// </summary>
        public bool IsScriptProject { get; }

        /// <summary>
        /// Determines if this is a valid project (<c>true</c>) or whether there are problems (<c>false</c>).
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Determines whether this is a legacy project. Legacy projects must be updated before they can be used.
        /// </summary>
        public bool IsLegacyProject { get; private set; }

        /// <summary>
        /// Creates a result representing this project as a legacy project
        /// </summary>
        /// <returns></returns>
        public ScriptProjectAnalysisResult AsLegacyProject()
        {
            return new ScriptProjectAnalysisResult(Project, ProjectProperties)
            {
                IsLegacyProject = true
            };
        }
    }
}
