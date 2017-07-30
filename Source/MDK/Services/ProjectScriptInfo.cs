using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
                string[] ignoredFolders = null;
                string[] ignoredFiles = null;
                var ignoreElement = root?.Element("ignore");
                if (ignoreElement != null)
                {
                    ignoredFolders = ignoreElement.Elements("folder").Select(e => (string)e).ToArray();
                    ignoredFiles = ignoreElement.Elements("file").Select(e => (string)e).ToArray();
                }

                var result = new ProjectScriptInfo(fileName, name, true)
                {
                    UseManualGameBinPath = useManualGameBinPath,
                    GameBinPath = gameBinPath,
                    InstallPath = installPath,
                    OutputPath = outputPath,
                    Minify = minify
                };
                if (ignoredFolders != null)
                    foreach (var item in ignoredFolders)
                        result.IgnoredFolders.Add(item);
                if (ignoredFiles != null)
                    foreach (var item in ignoredFiles)
                        result.IgnoredFiles.Add(item);
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
        string[] _ignoredFilesCache;
        string[] _ignoredFoldersCache;
        string _baseDir;

        ProjectScriptInfo(string fileName, string name, bool isValid)
        {
            FileName = fileName;
            if (fileName != null)
                _baseDir = Path.GetFullPath(Path.GetDirectoryName(fileName) ?? ".");
            Name = name;
            IsValid = isValid;
            IgnoredFolders.CollectionChanged += OnIgnoredFoldersChanged;
            IgnoredFiles.CollectionChanged += OnIgnoredFilesChanged;
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
        /// A list of folders which code will not be included in neither analysis nor deployment
        /// </summary>
        public ObservableCollection<string> IgnoredFolders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// A list of files which code will not be included in neither analysis nor deployment
        /// </summary>
        public ObservableCollection<string> IgnoredFiles { get; } = new ObservableCollection<string>();

        string FullyQualifiedFile(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);
            return Path.GetFullPath(Path.Combine(_baseDir, path));
        }

        string FullyQualifiedFolder(string path)
        {
            path = FullyQualifiedFile(path);
            if (path.EndsWith("\\"))
                return path;
            return path + "\\";
        }

        void OnIgnoredFilesChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            _ignoredFilesCache = null;
        }

        void OnIgnoredFoldersChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            _ignoredFoldersCache = null;
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
                var mdkOptionsFileName = new FileInfo(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FileName) ?? ".", @"mdk\mdk.options")));
                if (!mdkOptionsFileName.Directory?.Exists ?? true)
                    mdkOptionsFileName.Directory?.Create();
                XDocument document;
                XElement gameBinPathElement = null;
                XAttribute useManualGameBinPathAttribute = null;
                XElement installPathElement = null;
                XElement outputPathElement = null;
                XElement minifyElement = null;
                XElement ignoreElement = null;
                XElement root;
                if (!mdkOptionsFileName.Exists)
                {
                    document = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
                    root = new XElement("mdk", new XAttribute("version", MDKPackage.Version));
                    document.Add(root);
                }
                else
                {
                    document = XDocument.Load(mdkOptionsFileName.FullName);
                    root = document.Element("mdk");
                    // ReSharper disable once JoinNullCheckWithUsage
                    if (root == null)
                        throw new InvalidOperationException("Not a valid MDK Options File");

                    gameBinPathElement = root.Element("gamebinpath");
                    useManualGameBinPathAttribute = gameBinPathElement?.Attribute("enabled");
                    installPathElement = root.Element("installpath");
                    outputPathElement = root.Element("outputpath");
                    minifyElement = root.Element("minify");
                    ignoreElement = root.Element("ignore");
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
                if (ignoreElement == null && IgnoredFolders.Count > 0)
                {
                    ignoreElement = new XElement("ignore");
                    root.Add(ignoreElement);
                }

                gameBinPathElement.Value = GameBinPath.TrimEnd('\\');
                useManualGameBinPathAttribute.Value = UseManualGameBinPath ? "yes" : "no";
                installPathElement.Value = InstallPath.TrimEnd('\\');
                outputPathElement.Value = OutputPath.TrimEnd('\\');
                minifyElement.Value = Minify ? "yes" : "no";
                ignoreElement?.RemoveNodes();
                if (ignoreElement != null)
                {
                    foreach (var folder in IgnoredFolders)
                        ignoreElement.Add(new XElement("folder", folder));
                    foreach (var file in IgnoredFiles)
                        ignoreElement.Add(new XElement("file", file));
                }
                HasChanges = false;

                document.Save(mdkOptionsFileName.FullName, SaveOptions.OmitDuplicateNamespaces);
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

        /// <summary>
        /// Determines whether the given file path is within one of the ignored folders or files.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsIgnoredFilePath(string filePath)
        {
            filePath = Path.GetFullPath(filePath);

            if (_ignoredFilesCache == null)
                _ignoredFilesCache = IgnoredFiles.Select(FullyQualifiedFile).ToArray();
            if (_ignoredFilesCache.Any(path => filePath.Equals(path, StringComparison.CurrentCultureIgnoreCase)))
                return true;

            if (_ignoredFoldersCache == null)
                _ignoredFoldersCache = IgnoredFolders.Select(FullyQualifiedFolder).ToArray();
            if (_ignoredFoldersCache.Any(path => filePath.StartsWith(path, StringComparison.CurrentCultureIgnoreCase)))
                return true;

            return false;
        }
    }
}
