using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Malware.MDKServices.Legacy
{
    /// <summary>
    /// Provides information about a given project and its script.
    /// </summary>
    // ReSharper disable once InconsistentNaming because this is an exception to the rule.
    sealed class LegacyProjectScriptInfo_1_1
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
