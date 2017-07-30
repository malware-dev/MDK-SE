using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace MDK.Services
{
    /// <summary>
    /// Provides information about a given project and its script.
    /// </summary>
    public class ProjectScriptInfo : INotifyPropertyChanged
    {
        /// <summary>
        /// Loads script information from the given project file.
        /// </summary>
        /// <param name="projectFileName">The file name of this project</param>
        /// <param name="projectName">The display name of this project</param>
        /// <returns></returns>
        public static ProjectScriptInfo Load([NotNull] string projectFileName, string projectName = null)
        {
            if (string.IsNullOrEmpty(projectFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(projectFileName));
            var fileName = Path.GetFullPath(projectFileName);
            var name = projectName ?? Path.GetFileNameWithoutExtension(projectFileName);
            if (!File.Exists(fileName))
                return new ProjectScriptInfo(fileName, name, false);
            var mdkOptionsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.options"));
            if (!File.Exists(mdkOptionsFileName))
                return new ProjectScriptInfo(fileName, name, false);

            try
            {
                var document = XDocument.Load(mdkOptionsFileName);
                var root = document.Element("mdk");
                var useManualGameBinPath = ((string)root?.Element("gamebinpath")?.Attribute("enabled") ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);
                var gameBinPath = (string)root?.Element("gamebinpath");
                var installPath = (string)root?.Element("installpath");
                var outputPath = (string)root?.Element("outputpath");
                var minify = ((string)root?.Element("minify") ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);

                var result = new ProjectScriptInfo(fileName, name, true)
                {
                    UseManualGameBinPath = useManualGameBinPath,
                    GameBinPath = gameBinPath,
                    InstallPath = installPath,
                    OutputPath = outputPath,
                    Minify = minify
                };
                result.Commit();
                return result;
            }
            catch (Exception e)
            {
                throw new ProjectScriptInfoException($"An error occurred while attempting to load project information from {fileName}.", e);
            }
        }

        bool _minify;
        bool _hasChanges;
        string _gameBinPath;
        string _installPath;
        bool _useManualGameBinPath;
        string _outputPath;

        ProjectScriptInfo(string fileName, string name, bool isValid)
        {
            FileName = fileName;
            Name = name;
            IsValid = isValid;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the name of the project
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines whether changes have been made to the options for this project
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            private set
            {
                if (value == _hasChanges)
                    return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether this is a valid MDK project
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the project file name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Determines whether <see cref="GameBinPath"/> should be used, or the default value
        /// </summary>
        public bool UseManualGameBinPath
        {
            get => _useManualGameBinPath;
            set
            {
                if (value == _useManualGameBinPath)
                    return;
                _useManualGameBinPath = value;
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the path to the game's installed binaries.
        /// </summary>
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                if (value == _gameBinPath)
                    return;
                _gameBinPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the installation path for the extension.
        /// </summary>
        public string InstallPath
        {
            get => _installPath;
            set
            {
                if (value == _installPath)
                    return;
                _installPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the output path where the finished deployed script will be stored
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (value == _outputPath)
                    return;
                _outputPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether the script generated from this project should be run through the minifier
        /// </summary>
        public bool Minify
        {
            get => _minify;
            set
            {
                if (value == _minify)
                    return;
                _minify = value;
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Commits all changes without saving. <see cref="HasChanges"/> will be false after this. This method is not required when calling <see cref="Save"/>.
        /// </summary>
        public void Commit()
        {
            HasChanges = true;
        }

        /// <summary>
        /// Saves the options of this project
        /// </summary>
        /// <remarks>Warning: If the originating project is not saved first, these changes might be overwritten.</remarks>
        public void Save()
        {
            try
            {
                var mdkOptionsFileName = new FileInfo(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FileName) ?? ".", @"..\mdk\mdk.options")));
                if (!mdkOptionsFileName.Directory?.Exists ?? true)
                    mdkOptionsFileName.Directory?.Create();
                XDocument document;
                XElement gameBinPathElement = null;
                XAttribute useManualGameBinPathAttribute = null;
                XElement installPathElement = null;
                XElement outputPathElement = null;
                XElement minifyElement = null;
                XElement root;
                if (!mdkOptionsFileName.Exists)
                {
                    document = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
                    root = new XElement("mdk", new XAttribute("version", MDKPackage.Version));
                    document.Add(root);
                }
                else
                {
                    document = XDocument.Load(FileName);
                    root = document.Element("mdk");
                    // ReSharper disable once JoinNullCheckWithUsage
                    if (root == null)
                        throw new InvalidOperationException("Not a valid MDK Options File");

                    gameBinPathElement = root.Element("gamebinpath");
                    useManualGameBinPathAttribute = gameBinPathElement?.Attribute("enabled");
                    installPathElement = root.Element("installpath");
                    outputPathElement = root.Element("installpath");
                    minifyElement = root.Element("minify");
                }

                if (gameBinPathElement == null)
                {
                    gameBinPathElement = new XElement("gamebinpath");
                    root.Add(gameBinPathElement);
                }
                if (useManualGameBinPathAttribute == null)
                {
                    useManualGameBinPathAttribute = new XAttribute("enabled", "");
                    gameBinPathElement.Add(useManualGameBinPathAttribute);
                }

                if (installPathElement == null)
                {
                    installPathElement = new XElement("installpath");
                    root.Add(installPathElement);
                }
                if (outputPathElement == null)
                {
                    outputPathElement = new XElement("outputpath");
                    root.Add(outputPathElement);
                }
                if (minifyElement == null)
                {
                    minifyElement = new XElement("minify");
                    root.Add(minifyElement);
                }

                gameBinPathElement.Value = GameBinPath.TrimEnd('\\');
                useManualGameBinPathAttribute.Value = UseManualGameBinPath ? "yes" : "no";
                installPathElement.Value = InstallPath.TrimEnd('\\');
                outputPathElement.Value = OutputPath.TrimEnd('\\');
                minifyElement.Value = Minify ? "yes" : "no";
                HasChanges = false;

                document.Save(FileName, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception e)
            {
                throw new ProjectScriptInfoException($"An error occurred while attempting to save project information to {FileName}.", e);
            }
        }

        /// <summary>
        /// Called whenever a trackable property changes
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Returns the actual game bin path to use, depending on the settings in this project.
        /// </summary>
        /// <param name="defaultPath">The default path to use when <see cref="UseManualGameBinPath"/> is <c>false</c></param>
        /// <returns></returns>
        public string GetActualGameBinPath(string defaultPath)
        {
            if (UseManualGameBinPath)
                return Path.GetFullPath(string.IsNullOrEmpty(GameBinPath) ? defaultPath : GameBinPath);
            return Path.GetFullPath(defaultPath);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
