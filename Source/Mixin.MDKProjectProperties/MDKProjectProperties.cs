using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Malware.MDKServices
{
    /// <summary>
    /// Provides information about a given project and its script.
    /// </summary>
    public partial class MDKProjectProperties : INotifyPropertyChanged
    {
        /// <summary>
        /// Loads script information from the given project file.
        /// </summary>
        /// <param name="projectFileName">The file name of this project</param>
        /// <param name="projectName">The display name of this project</param>
        /// <returns></returns>
        public static MDKProjectProperties Load([NotNull] string projectFileName, string projectName = null)
        {
            if (string.IsNullOrEmpty(projectFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(projectFileName));

            if (!File.Exists(projectFileName) || Regex.IsMatch(projectFileName, @"\w+://"))
                return new MDKProjectProperties(projectFileName, null, null, null);

            var fileName = Path.GetFullPath(projectFileName);
            var name = projectName ?? Path.GetFileNameWithoutExtension(projectFileName);
            var legacyOptionsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.options"));
            var mdkOptionsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.options.props"));
            var mdkPathsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.paths.props"));

            MDKProjectOptions options = null;
            MDKProjectPaths paths = null;
            if (File.Exists(mdkOptionsFileName)/* && File.Exists(mdkPathsFileName)*/)
            {
                options = MDKProjectOptions.Load(mdkOptionsFileName);
                paths = MDKProjectPaths.Load(mdkPathsFileName);

                return new MDKProjectProperties(projectFileName, name, options, paths);
            }

            if (File.Exists(legacyOptionsFileName))
            {
                ImportLegacy_1_1(projectFileName, ref options, mdkOptionsFileName, ref paths, mdkPathsFileName);
                if (options != null && paths != null)
                    return new MDKProjectProperties(projectFileName, name, options, paths);
            }

            return new MDKProjectProperties(projectFileName, null, null, null);
        }

        static partial void ImportLegacy_1_1(string legacyOptionsFileName, ref MDKProjectOptions options, string optionsFileName, ref MDKProjectPaths paths, string pathsFileName);

        bool _hasChanges;

        MDKProjectProperties(string fileName, string name, MDKProjectOptions options, MDKProjectPaths paths)
        {
            FileName = fileName;
            Name = name;
            Options = options;
            if (Options != null)
                Options.PropertyChanged += OnOptionsPropertyChanged;
            Paths = paths;
            if (Paths != null)
                Paths.PropertyChanged += OnPathsPropertyChanged;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The project paths
        /// </summary>
        public MDKProjectPaths Paths { get; }

        /// <summary>
        /// The project options
        /// </summary>
        public MDKProjectOptions Options { get; }

        /// <summary>
        /// Gets the name of the project
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines whether changes have been made to the options for this project
        /// </summary>
        public bool HasChanges
        {
            get => CheckForChanges();
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
        public bool IsValid => Options?.IsValid ?? false;

        /// <summary>
        /// Gets the project file name
        /// </summary>
        public string FileName { get; }

        void OnPathsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            HasChanges = CheckForChanges();
        }

        void OnOptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            HasChanges = CheckForChanges();
        }

        bool CheckForChanges() => Options.HasChanges || Paths.HasChanges;

        /// <summary>
        /// Commits all changes without saving. <see cref="HasChanges"/> will be false after this. This method is not required when calling <see cref="Save"/>.
        /// </summary>
        public void Commit()
        {
            Options.Commit();
            Paths.Commit();
            HasChanges = CheckForChanges();
        }

        /// <summary>
        /// Saves the options of this project
        /// </summary>
        /// <remarks>Warning: If the originating project is not saved first, these changes might be overwritten.</remarks>
        public void Save()
        {
            Options.Save();
            Paths.Save();
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
        public bool IsIgnoredFilePath(string filePath) => Options.IsIgnoredFilePath(filePath);

        /// <summary>
        /// Provides information about a given project and its script.
        /// </summary>
        // ReSharper disable once InconsistentNaming because this is an exception to the rule.
        internal sealed class LegacyProjectScriptInfo_1_1
        {
            /// <summary>
            /// Loads script information from the given project file.
            /// </summary>
            /// <param name="projectFileName">The file name of this project</param>
            /// <returns></returns>
            public static LegacyProjectScriptInfo_1_1 Load([NotNull] string projectFileName)
            {
                if (string.IsNullOrEmpty(projectFileName))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(projectFileName));
                var fileName = Path.GetFullPath(projectFileName);
                if (!File.Exists(fileName))
                    return new LegacyProjectScriptInfo_1_1(fileName, false, null, null);
                var mdkOptionsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.options"));
                if (!File.Exists(mdkOptionsFileName))
                    return new LegacyProjectScriptInfo_1_1(fileName, false, null, null);

                try
                {
                    var document = XDocument.Load(mdkOptionsFileName);
                    var root = document.Element("mdk");

                    // Check if this is a template options file
                    if ((string)root?.Attribute("version") == "$mdkversion$")
                        return new LegacyProjectScriptInfo_1_1(fileName, false, null, null);
                    var version = Version.Parse((string)root?.Attribute("version"));
                    var useManualGameBinPath = ((string)root?.Element("gamebinpath")?.Attribute("enabled") ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);
                    var gameBinPath = (string)root?.Element("gamebinpath");
                    var installPath = (string)root?.Element("installpath");
                    var outputPath = (string)root?.Element("outputpath");
                    var minify = ((string)root?.Element("minify") ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);
                    var trimTypes = ((string)root?.Element("trimtypes") ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);
                    string[] ignoredFolders = null;
                    string[] ignoredFiles = null;
                    var ignoreElement = root?.Element("ignore");
                    if (ignoreElement != null)
                    {
                        ignoredFolders = ignoreElement.Elements("folder").Select(e => (string)e).ToArray();
                        ignoredFiles = ignoreElement.Elements("file").Select(e => (string)e).ToArray();
                    }

                    var result = new LegacyProjectScriptInfo_1_1(fileName, true, ignoredFolders, ignoredFiles)
                    {
                        Version = version,
                        UseManualGameBinPath = useManualGameBinPath,
                        GameBinPath = gameBinPath,
                        InstallPath = installPath,
                        OutputPath = outputPath,
                        Minify = minify,
                        TrimTypes = trimTypes
                    };
                    return result;
                }
                catch (Exception e)
                {
                    throw new MDKProjectPropertiesException($"An error occurred while attempting to load project information from {fileName}.", e);
                }
            }

            string _gameBinPath;
            string _installPath;
            string _outputPath;

            LegacyProjectScriptInfo_1_1(string fileName, bool isValid, string[] ignoredFolders, string[] ignoredFiles)
            {
                FileName = fileName;
                IsValid = isValid;
                if (ignoredFolders != null)
                    IgnoredFolders = new ReadOnlyCollection<string>(ignoredFolders);
                if (ignoredFiles != null)
                    IgnoredFiles = new ReadOnlyCollection<string>(ignoredFiles);
            }

            /// <summary>
            /// Returns the options version
            /// </summary>
            public Version Version { get; private set; }

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
            public bool UseManualGameBinPath { get; private set; }

            /// <summary>
            /// Determines the path to the game's installed binaries.
            /// </summary>
            public string GameBinPath
            {
                get => _gameBinPath;
                private set => _gameBinPath = value ?? "";
            }

            /// <summary>
            /// Determines the installation path for the extension.
            /// </summary>
            public string InstallPath
            {
                get => _installPath;
                private set => _installPath = value ?? "";
            }

            /// <summary>
            /// Determines the output path where the finished deployed script will be stored
            /// </summary>
            public string OutputPath
            {
                get => _outputPath;
                private set => _outputPath = value ?? "";
            }

            /// <summary>
            /// Determines whether the script generated from this project should be run through the type trimmer which removes unused types
            /// </summary>
            public bool TrimTypes { get; private set; }

            /// <summary>
            /// Determines whether the script generated from this project should be run through the minifier
            /// </summary>
            public bool Minify { get; private set; }

            /// <summary>
            /// A list of folders which code will not be included in neither analysis nor deployment
            /// </summary>
            public ReadOnlyCollection<string> IgnoredFolders { get; }

            /// <summary>
            /// A list of files which code will not be included in neither analysis nor deployment
            /// </summary>
            public ReadOnlyCollection<string> IgnoredFiles { get; }
        }

    }
}
