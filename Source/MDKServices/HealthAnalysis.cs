using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;

namespace Malware.MDKServices
{
    /// <summary>
    /// A service to analyze the validity and health of a project, determining whether or not it's an MDK project in the first place
    /// and the general health state of the project if it is.
    /// </summary>
    public class HealthAnalysis
    {
        /// <summary>
        /// Analyze an individual project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<HealthAnalysis> AnalyzeAsync(Project project, HealthAnalysisOptions options) => Task.Run(() => Analyze(project, options));

        /// <summary>
        /// Analyze an entire solution's worth of projects
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<HealthAnalysis[]> AnalyzeAsync(Solution solution, HealthAnalysisOptions options) => Task.WhenAll(solution.Projects.Cast<Project>().Select(project => Task.Run(() => Analyze(project, options))));

        static HealthAnalysis Analyze(Project project, HealthAnalysisOptions options)
        {
            if (!project.IsLoaded())
                return new HealthAnalysis(project, null, options);
            var projectInfo = MDKProjectProperties.Load(project.FullName, project.Name);
            if (!projectInfo.IsValid && !projectInfo.Options.IsValid)
                return new HealthAnalysis(project, null, options);

            var analysis = new HealthAnalysis(project, projectInfo, options);
            if (projectInfo.Options.Version < new Version(1, 2))
                analysis._problems.Add(new HealthProblem(HealthCode.Outdated, HealthSeverity.Critical, "This project format is outdated."));

            if (!projectInfo.Paths.IsValid)
                analysis._problems.Add(new HealthProblem(HealthCode.MissingPathsFile, HealthSeverity.Critical, "Missing paths file"));
            else
            {
                var installPath = projectInfo.Paths.InstallPath.TrimEnd('/', '\\');
                var expectedInstallPath = options.InstallPath.TrimEnd('/', '\\');

                if (!string.Equals(installPath, expectedInstallPath, StringComparison.CurrentCultureIgnoreCase))
                    analysis._problems.Add(new HealthProblem(HealthCode.BadInstallPath, HealthSeverity.Warning, "Invalid install path, can't find analyzer"));
            }


            return analysis;
        }

        List<HealthProblem> _problems = new List<HealthProblem>();

        HealthAnalysis(Project project, MDKProjectProperties properties, HealthAnalysisOptions analysisOptions)
        {
            Project = project;
            try
            {
                FileName = project.FileName;
            }
            catch (NotImplementedException)
            {
                // Ignored. Ugly but necessary hack.
            }
            Properties = properties;
            AnalysisOptions = analysisOptions;
            IsMDKProject = properties != null;
            Problems = new ReadOnlyCollection<HealthProblem>(_problems);
            if (!IsMDKProject)
                _problems.Add(new HealthProblem(HealthCode.NotAnMDKProject, HealthSeverity.Critical, "This is not an MDK project."));
        }

        /// <summary>
        /// Gets the file name of the analyzed project.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The project this health analysis is about
        /// </summary>
        public Project Project { get; }

        /// <summary>
        /// MDK project properties
        /// </summary>
        public MDKProjectProperties Properties { get; }

        public HealthAnalysisOptions AnalysisOptions { get; }

        /// <summary>
        /// Whether or not this is an MDK project
        /// </summary>
        public bool IsMDKProject { get; }

        /// <summary>
        /// Determines overall whether this project is a healthy MDK project.
        /// </summary>
        public bool IsHealthy => _problems.Count == 0;

        /// <summary>
        /// A list of problems in this project (if any)
        /// </summary>
        public ReadOnlyCollection<HealthProblem> Problems { get; }
    }
}
