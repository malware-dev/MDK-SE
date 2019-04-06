using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Resources;
using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views.ProjectHealth
{
    /// <summary>
    /// The view model for the <see cref="ProjectHealthDialog"/> view.
    /// </summary>
    public class ProjectHealthDialogModel : DialogViewModel
    {
        string _message = Text.ProjectHealthDialogModel_DefaultMessage;

        /// <summary>
        /// Creates a new instance of this view model.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="analyses"></param>
        public ProjectHealthDialogModel([NotNull] MDKPackage package, [NotNull] HealthAnalysis[] analyses)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            Projects = analyses.ToList().AsReadOnly();
        }

        /// <summary>
        /// A list of projects and their problems
        /// </summary>
        public ReadOnlyCollection<HealthAnalysis> Projects { get; set; }

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
        /// The associated MDK package
        /// </summary>
        public MDKPackage Package { get; }

        /// <inheritdoc />
        protected override bool OnCancel()
        {
            foreach (var analysis in Projects)
            {
                if (analysis.Problems.Any(p => p.Severity == HealthSeverity.Critical))
                    analysis.Project.Unload();
            }

            return base.OnCancel();
        }

        /// <summary>
        /// Upgrades the projects.
        /// </summary>
        protected override bool OnSave()
        {
            if (!SaveAndCloseCommand.IsEnabled)
                return false;
            try
            {
                foreach (var project in Projects)
                {
                    Backup(project);

                    var handle = project.Project.Unload();
                    Repair(project);
                    handle.Reload();
                }
            }
            catch (Exception e)
            {
                Package.ShowError(Text.ProjectHealthDialogModel_OnSave_Error, Text.ProjectHealthDialogModel_OnSave_Error_Description, e);
            }
            return true;
        }

        void Backup(HealthAnalysis project)
        {
            var directory = Path.GetDirectoryName(project.FileName) ?? ".\\";
            var zipFileName = $"Backup_{DateTime.Now:yyyy-MM-dd-HHmmssfff}.zip";
            var tmpZipName = Path.Combine(Path.GetTempPath(), zipFileName);
            ZipFile.CreateFromDirectory(directory, tmpZipName, CompressionLevel.Fastest, false);
            var backupDirectory = new DirectoryInfo(Path.Combine(directory, "Backup"));
            if (!backupDirectory.Exists)
                backupDirectory.Create();
            File.Copy(tmpZipName, Path.Combine(backupDirectory.FullName, zipFileName));
            File.Delete(tmpZipName);
        }

        void Repair(HealthAnalysis project)
        {
            if (project.Problems.Any(p => p.Code == HealthCode.Outdated))
            {
                Upgrade(project);
                return;
            }

            foreach (var problem in project.Problems)
            {
                switch (problem.Code)
                {
                    case HealthCode.NotAnMDKProject:
                    case HealthCode.Outdated:
                    case HealthCode.Healthy:
                        break;

                    case HealthCode.MissingPathsFile:
                        //project.Properties.Paths.GameBinPath = project.AnalysisOptions.DefaultGameBinPath;
                        //project.Properties.Paths.InstallPath = project.AnalysisOptions.InstallPath;
                        //project.Properties.Paths.OutputPath = project.AnalysisOptions.
                        break;

                    case HealthCode.BadInstallPath:
                        project.Properties.Paths.InstallPath = project.AnalysisOptions.InstallPath;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            project.Properties.Paths.Save();
        }

        void Upgrade(HealthAnalysis project)
        {
            if (project.Properties.Options.Version < new Version(1, 2))
            {
                var upgrader = new UpgradeFrom_1_1();
                upgrader.Upgrade(project);
                return;
            }

            throw new InvalidOperationException(string.Format(Text.ProjectHealthDialogModel_Upgrade_BadUpgradeVersion, project.Properties.Options.Version));
        }
    }
}
