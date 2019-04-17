using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Resources;

namespace MDK.Views.ProjectHealth
{
    /// <summary>
    /// The view model for the <see cref="ProjectHealthDialog"/> view.
    /// </summary>
    public class ProjectHealthDialogModel : DialogViewModel
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";
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
        /// Requests the project options dialog to allow the user to make changes to the output path.
        /// </summary>
        public event EventHandler<ProjectOptionsRequestedEventArgs> ProjectOptionsRequested;

        /// <summary>
        /// Informs the view that the upgrade process is complete.
        /// </summary>
        public event EventHandler UpgradeCompleted;

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
                SaveAndCloseCommand.IsEnabled = false;
                CancelCommand.IsEnabled = false;
                foreach (var project in Projects)
                {
                    Backup(project);

                    var handle = project.Project.Unload();
                    Repair(project);
                    handle.Reload();
                }
                UpgradeCompleted?.Invoke(this, EventArgs.Empty);
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
            var zipFileName = $"{Path.GetFileNameWithoutExtension(project.FileName)}_Backup_{DateTime.Now:yyyy-MM-dd-HHmmssfff}.zip";
            var tmpZipName = Path.Combine(Path.GetTempPath(), zipFileName);
            ZipFile.CreateFromDirectory(directory, tmpZipName, CompressionLevel.Fastest, false);
            var backupDirectory = new DirectoryInfo(Path.Combine(directory, "..\\"));
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

            var showOptions = false;
            var addPropertiesFile = false;
            foreach (var problem in project.Problems)
            {
                switch (problem.Code)
                {
                    case HealthCode.NotAnMDKProject:
                    case HealthCode.Outdated:
                    case HealthCode.Healthy:
                        break;

                    case HealthCode.MissingPathsFile:
                        showOptions = true;
                        project.Properties.Paths.InstallPath = project.AnalysisOptions.InstallPath;
                        project.Properties.Paths.GameBinPath = project.AnalysisOptions.DefaultGameBinPath;
                        project.Properties.Paths.OutputPath = project.AnalysisOptions.DefaultOutputPath;
                        foreach (var reference in MDKProjectPaths.DefaultAssemblyReferences)
                            project.Properties.Paths.AssemblyReferences.Add(reference);
                        foreach (var reference in MDKProjectPaths.DefaultAnalyzerReferences)
                            project.Properties.Paths.AnalyzerReferences.Add(reference);
                        addPropertiesFile = true;
                        break;

                    case HealthCode.BadInstallPath:
                        project.Properties.Paths.InstallPath = project.AnalysisOptions.InstallPath;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            project.Properties.Paths.Save();
            if (addPropertiesFile)
            {
                Include(project.FileName, project.Properties.Paths.FileName);
            }

            if (showOptions)
            {
                var args = new ProjectOptionsRequestedEventArgs(Package, project.Properties);
                ProjectOptionsRequested?.Invoke(this, args);
            }
        }

        void Include(string projectFileName, string fileName)
        {
            XDocument document;
            XmlNameTable nameTable;
            using (var streamReader = File.OpenText(projectFileName))
            {
                var readerSettings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true
                };

                var xmlReader = XmlReader.Create(streamReader, readerSettings);
                document = XDocument.Load(xmlReader);
                nameTable = xmlReader.NameTable;
                if (nameTable == null)
                    throw new InvalidOperationException(Text.UpgradeFrom1_1_Upgrade_ErrorLoadingProject);
            }

            var nsm = new XmlNamespaceManager(nameTable);
            nsm.AddNamespace("m", Xmlns);

            var projectBasePath = Path.GetDirectoryName(Path.GetFullPath(projectFileName)) ?? ".";
            if (!projectBasePath.EndsWith("\\"))
                projectBasePath += "\\";
            fileName = Path.Combine(projectBasePath, fileName);
            if (fileName.StartsWith(projectBasePath))
                fileName = fileName.Substring(projectBasePath.Length);
            else
                fileName = Path.GetFullPath(fileName);

            var existingElement = document.Descendants(XName.Get("AdditionalFiles", Xmlns))
                .FirstOrDefault(e => string.Equals((string)e.Attribute("Include"), fileName, StringComparison.CurrentCultureIgnoreCase));
            if (existingElement != null)
                return;

            var itemGroupElement = document.Descendants(XName.Get("ItemGroup", Xmlns)).LastOrDefault();
            if (itemGroupElement == null)
            {
                itemGroupElement = new XElement(XName.Get("ItemGroup", Xmlns));
                document.Root.Add(itemGroupElement);
            }

            var fileElement = new XElement(XName.Get("AdditionalFiles", Xmlns),
                new XAttribute("Include", fileName),
                new XElement(XName.Get("CopyToOutputDirectory", Xmlns), new XText("Always"))
            );
            itemGroupElement.Add(fileElement);
            document.Save(projectFileName);
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
