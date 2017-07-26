using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using JetBrains.Annotations;

namespace MDK.Services
{
    /// <summary>
    /// Provides information about a given project and its script.
    /// </summary>
    public class ProjectScriptInfo : INotifyPropertyChanged
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        readonly MDKPackage _package;
        string _outputPath;
        bool _minify;
        bool _hasChanges;
        string _gameBinPath;
        string _utilityPath;
        bool _useManualGameBinPath;

        /// <summary>
        /// Creates an instance of <see cref="ProjectScriptInfo"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="projectFileName"></param>
        /// <param name="projectName"></param>
        public ProjectScriptInfo([NotNull] MDKPackage package, [NotNull] string projectFileName, [NotNull] string projectName)
        {
            if (string.IsNullOrEmpty(projectFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(projectFileName));
            _package = package ?? throw new ArgumentNullException(nameof(package));
            FileName = Path.GetFullPath(projectFileName);
            Name = projectName ?? throw new ArgumentNullException(nameof(projectName));
            if (!File.Exists(FileName))
            {
                IsValid = false;
                return;
            }

            try
            {
                var document = XDocument.Load(projectFileName);
                var xmlns = new XmlNamespaceManager(new NameTable());
                xmlns.AddNamespace("ms", Xmlns);
                var predicate = string.Join(" or ", Enum.GetNames(typeof(MDKProperties)).Select(tag => $"self::ms:{tag}"));
                var propertyElements = document.XPathSelectElements($"/ms:Project/ms:PropertyGroup/*[{predicate}]", xmlns)
                    .ToDictionary(e => (MDKProperties)Enum.Parse(typeof(MDKProperties), e.Name.LocalName));

                if (
                    !propertyElements.TryGetValue(MDKProperties.MDKUtilityPath, out var utilityPathElement)
                    || !propertyElements.TryGetValue(MDKProperties.MDKOutputPath, out var outputPathElement)
                )
                {
                    IsValid = false;
                    return;
                }
                propertyElements.TryGetValue(MDKProperties.MDKGameBinPath, out var gameBinPathElement);
                propertyElements.TryGetValue(MDKProperties.MDKMinify, out var minifyElement);
                propertyElements.TryGetValue(MDKProperties.MDKUseManualGameBinPath, out var useManualGameBinPathElement);

                UseManualGameBinPath = (useManualGameBinPathElement?.Value ?? "no").Trim().ToUpperInvariant() == "YES";
                GameBinPath = gameBinPathElement?.Value ?? package.Options.GetActualGameBinPath();
                UtilityPath = utilityPathElement.Value;
                OutputPath = outputPathElement.Value;
                Minify = (minifyElement?.Value ?? "no").Trim().ToUpperInvariant() == "YES";
                IsValid = true;
                HasChanges = false;
            }
            catch (Exception e)
            {
                package.LogPackageError(GetType().FullName, e);
                IsValid = false;
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

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
        public string UtilityPath
        {
            get => _utilityPath;
            set
            {
                if (value == _utilityPath)
                    return;
                _utilityPath = value ?? "";
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
        /// Saves the options of this project
        /// </summary>
        /// <remarks>Warning: If the originating project is not saved first, these changes might be overwritten.</remarks>
        public void Save()
        {
            try
            {
                var document = XDocument.Load(FileName);
                var xmlns = new XmlNamespaceManager(new NameTable());
                xmlns.AddNamespace("ms", Xmlns);
                var predicate = string.Join(" or ", Enum.GetNames(typeof(MDKProperties)).Select(tag => $"self::ms:{tag}"));
                var propertyElements = document.XPathSelectElements($"/ms:Project/ms:PropertyGroup/*[{predicate}]", xmlns)
                    .ToDictionary(e => (MDKProperties)Enum.Parse(typeof(MDKProperties), e.Name.LocalName));

                propertyElements.TryGetValue(MDKProperties.MDKUseManualGameBinPath, out var useManualGameBinPathElement);
                propertyElements.TryGetValue(MDKProperties.MDKGameBinPath, out var gameBinPathElement);
                propertyElements.TryGetValue(MDKProperties.MDKUtilityPath, out var utilityPathElement);
                propertyElements.TryGetValue(MDKProperties.MDKOutputPath, out var outputPathElement);
                propertyElements.TryGetValue(MDKProperties.MDKMinify, out var minifyElement);

                var propertyGroupElement = gameBinPathElement?.Parent ?? utilityPathElement?.Parent ?? outputPathElement?.Parent ?? minifyElement?.Parent;
                if (propertyGroupElement == null)
                {
                    propertyGroupElement = new XElement(XName.Get("PropertyGroup", Xmlns));
                    document.Root?.Add(propertyGroupElement);
                }

                if (useManualGameBinPathElement == null)
                {
                    useManualGameBinPathElement = new XElement(XName.Get(nameof(MDKProperties.MDKUseManualGameBinPath), Xmlns));
                    propertyGroupElement.Add(useManualGameBinPathElement);
                }
                if (gameBinPathElement == null)
                {
                    gameBinPathElement = new XElement(XName.Get(nameof(MDKProperties.MDKGameBinPath), Xmlns));
                    propertyGroupElement.Add(gameBinPathElement);
                }
                if (utilityPathElement == null)
                {
                    utilityPathElement = new XElement(XName.Get(nameof(MDKProperties.MDKUtilityPath), Xmlns));
                    propertyGroupElement.Add(utilityPathElement);
                }
                if (outputPathElement == null)
                {
                    outputPathElement = new XElement(XName.Get(nameof(MDKProperties.MDKOutputPath), Xmlns));
                    propertyGroupElement.Add(outputPathElement);
                }
                if (minifyElement == null)
                {
                    minifyElement = new XElement(XName.Get(nameof(MDKProperties.MDKMinify), Xmlns));
                    propertyGroupElement.Add(minifyElement);
                }

                useManualGameBinPathElement.Value = UseManualGameBinPath ? "yes" : "no";
                gameBinPathElement.Value = GameBinPath.TrimEnd('\\');
                utilityPathElement.Value = UtilityPath.TrimEnd('\\');
                outputPathElement.Value = OutputPath.TrimEnd('\\');
                minifyElement.Value = Minify ? "yes" : "no";
                HasChanges = false;

                document.Save(FileName, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception e)
            {
                _package.LogPackageError(GetType().FullName, e);
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

        enum MDKProperties
        {
            MDKGameBinPath,
            MDKUtilityPath,
            MDKOutputPath,
            MDKMinify,
            MDKUseManualGameBinPath
        }
    }
}
