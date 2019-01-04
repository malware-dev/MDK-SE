using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Resources;

namespace MDK.Views.ProjectIntegrity
{
    /// <summary>
    /// The view model for the <see cref="RequestUpgradeDialog"/> view.
    /// </summary>
    public class RequestUpgradeDialogModel : DialogViewModel
    {
        string _message = "The following MDK projects must be upgraded in order to function correctly. Do you wish to do this now?";

        /// <summary>
        /// Creates a new instance of this view model.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="analysisResults"></param>
        public RequestUpgradeDialogModel([NotNull] MDKPackage package, [NotNull] ScriptSolutionAnalysisResult analysisResults)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            AnalysisResults = analysisResults ?? throw new ArgumentNullException(nameof(analysisResults));
            if (analysisResults.BadProjects.IsDefaultOrEmpty)
                Projects = new ReadOnlyCollection<MDKProjectProperties>(new List<MDKProjectProperties>());
            else
            {
                if (analysisResults.BadProjects.Any(p => !p.HasValidGamePath))
                {
                    Projects = new ReadOnlyCollection<MDKProjectProperties>(analysisResults.BadProjects.Where(p => !p.HasValidGamePath) .Select(p => p.ProjectProperties).ToArray());
                    Message = "The Space Engineers game folder could not be determined. Automatic upgrades cannot be completed. Please verify that the game is installed and that the MDK configuration is correct, and then reload the projects. This affects the following MDK projects:";
                    SaveAndCloseCommand.IsEnabled = false;
                }
                else
                {
                    Projects = new ReadOnlyCollection<MDKProjectProperties>(analysisResults.BadProjects.Select(p => p.ProjectProperties).ToArray());
                }
            }
        }

        /// <summary>
        /// Contains the message to display in the dialog.
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                if (value == _message)
                    return;
                _message = value;
                OnPropertyChanged();
            }
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
        public ReadOnlyCollection<MDKProjectProperties> Projects { get; }

        /// <summary>
        /// Upgrades the projects.
        /// </summary>
        protected override bool OnSave()
        {
            if (!SaveAndCloseCommand.IsEnabled)
                return false;
            try
            {
                Package.ScriptUpgrades.Repair(AnalysisResults);
            }
            catch (Exception e)
            {
                Package.ShowError(Text.RequestUpgradeDialogModel_OnSave_Error, Text.RequestUpgradeDialogModel_OnSave_Error_Description, e);
            }
            return true;
        }
    }
}
