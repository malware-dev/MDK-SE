using System;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using MDK.Services;

namespace MDK.Views
{
    /// <summary>
    /// The view model for the <see cref="RequestUpgradeDialog"/> view.
    /// </summary>
    public class RequestUpgradeDialogModel : DialogViewModel
    {
        /// <summary>
        /// Creates a new instance of this view model.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="analysisResults"></param>
        public RequestUpgradeDialogModel([NotNull] MDKPackage package, [NotNull] ScriptSolutionAnalysisResult analysisResults)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            AnalysisResults = analysisResults ?? throw new ArgumentNullException(nameof(analysisResults));
            Projects = new ReadOnlyCollection<ProjectScriptInfo>(analysisResults.BadProjects.Select(p => p.ProjectInfo).ToArray());
        }

        /// <summary>
        /// The analysis reults
        /// </summary>
        public ScriptSolutionAnalysisResult AnalysisResults { get; set; }

        /// <summary>
        /// The associated MDK package
        /// </summary>
        public MDKPackage Package { get; }

        /// <summary>
        /// Contains the list of projects to examine.
        /// </summary>
        public ReadOnlyCollection<ProjectScriptInfo> Projects { get; }

        /// <summary>
        /// Upgrades the projects.
        /// </summary>
        protected override bool OnSave()
        {
            Package.ScriptUpgrades.Upgrade(AnalysisResults);
            return true;
        }
    }
}
