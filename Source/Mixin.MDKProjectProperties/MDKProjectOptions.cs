using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using JetBrains.Annotations;

namespace Malware.MDKServices
{
    /// <summary>
    /// Represents a set of general options for an MDK project
    /// </summary>
    public partial class MDKProjectOptions : INotifyPropertyChanged
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        /// Loads options for a given MDK project
        /// </summary>
        /// <param name="fileName">The file name of the options file</param>
        /// <returns></returns>
        public static MDKProjectOptions Load([NotNull] string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(fileName));
            fileName = Path.GetFullPath(fileName);
            if (!File.Exists(fileName))
                return new MDKProjectOptions(fileName, false);

            try
            {
                using (var streamReader = File.OpenText(fileName))
                {
                    var readerSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true
                    };
                    var xmlReader = XmlReader.Create(streamReader, readerSettings);
                    var document = XDocument.Load(xmlReader);
                    var nameTable = xmlReader.NameTable;
                    if (nameTable == null)
                        return new MDKProjectOptions(fileName, false);
                    return Load(document, nameTable, fileName);
                }
            }
            catch (Exception e)
            {
                throw new MDKProjectPropertiesException($"An error occurred while attempting to load project information from {fileName}.", e);
            }
        }

        /// <summary>
        /// Loads options for a given MDK project
        /// </summary>
        /// <param name="xmlContent">The content of the options file</param>
        /// <param name="fileName">The file name of the options file</param>
        /// <returns></returns>
        public static MDKProjectOptions Parse(string xmlContent, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(fileName));
            fileName = Path.GetFullPath(fileName);
            if (!File.Exists(fileName))
                return new MDKProjectOptions(fileName, false);

            try
            {
                using (var streamReader = new StringReader(xmlContent))
                {
                    var readerSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true
                    };
                    var xmlReader = XmlReader.Create(streamReader, readerSettings);
                    var document = XDocument.Load(xmlReader);
                    var nameTable = xmlReader.NameTable;
                    if (nameTable == null)
                        return new MDKProjectOptions(fileName, false);
                    return Load(document, nameTable, fileName);
                }
            }
            catch (Exception e)
            {
                throw new MDKProjectPropertiesException($"An error occurred while attempting to load project information from {fileName}.", e);
            }
        }

        static MDKProjectOptions Load(XDocument document, XmlNameTable nameTable, string fileName)
        {
            var nsm = new XmlNamespaceManager(nameTable);
            nsm.AddNamespace("m", Xmlns);

            // Check if this is a template options file
            var versionElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKVersion", nsm);
            var versionStr = (string)versionElement;
            if (versionStr == "$mdkversion$")
                return new MDKProjectOptions(fileName, false);
            Version.TryParse(versionStr, out var version);

            var namespaceElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKNamespace", nsm);
            var ns = ((string)namespaceElement)?.Trim();
            var minifyElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKMinify/m:Enabled", nsm);
            var minify = ((string)minifyElement ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);
            var trimTypesElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKTrimTypes/m:Enabled", nsm);
            var trimTypes = ((string)trimTypesElement ?? "no").Trim().Equals("yes", StringComparison.CurrentCultureIgnoreCase);
            var ignoredFolders = document.XPathSelectElements("./m:Project/m:PropertyGroup/m:MDKIgnore/m:Folder", nsm).Select(e => (string)e).ToArray();
            var ignoredFiles = document.XPathSelectElements("./m:Project/m:PropertyGroup/m:MDKIgnore/m:File", nsm).Select(e => (string)e).ToArray();
            var excludeFromDeployAll = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKExcludeFromDeployAll", nsm) != null;

            var result = new MDKProjectOptions(fileName, true)
            {
                Version = version,
                Namespace = ns,
                Minify = minify,
                TrimTypes = trimTypes,
                ExcludeFromDeployAll = excludeFromDeployAll
            };
            if (ignoredFolders.Length > 0)
                foreach (var item in ignoredFolders)
                    result.IgnoredFolders.Add(item);
            if (ignoredFiles.Length > 0)
                foreach (var item in ignoredFiles)
                    result.IgnoredFiles.Add(item);
            result.HasChanges = false;
            return result;
        }

        bool _minify;
        bool _hasChanges;
        string[] _ignoredFilesCache;
        string[] _ignoredFoldersCache;
        bool _trimTypes;
        string _baseDir;
        Version _version;
        string _ns;
        bool _excludeFromDeployAll;

        MDKProjectOptions(string fileName, bool isValid)
        {
            FileName = fileName;
            IsValid = isValid;
            if (fileName != null)
                _baseDir = Path.GetFullPath(Path.GetDirectoryName(fileName) ?? ".");
            IgnoredFolders.CollectionChanged += OnIgnoredFoldersChanged;
            IgnoredFiles.CollectionChanged += OnIgnoredFilesChanged;
        }

        /// <summary>
        /// Occurs when a tracked property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
        /// The MDK version this options file was saved with
        /// </summary>
        public Version Version
        {
            get => _version;
            private set
            {
                if (Equals(value, _version))
                    return;
                _version = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether the script generated from this project should be run through the type trimmer which removes unused types
        /// </summary>
        public bool TrimTypes
        {
            get => _trimTypes;
            set
            {
                if (value == _trimTypes)
                    return;
                _trimTypes = value;
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the main script namespace. Used by the analyzer for the inconsistent namespace warning.
        /// </summary>
        public string Namespace
        {
            get => _ns;
            set
            {
                if (value == _ns)
                    return;
                _ns = value;
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
        /// Determines whether this script should be excluded when running the Deploy All command.
        /// </summary>
        public bool ExcludeFromDeployAll
        {
            get => _excludeFromDeployAll;
            set
            {
                if (value == _excludeFromDeployAll)
                    return;
                _excludeFromDeployAll = value;
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
            return Path.GetFullPath(Path.Combine(_baseDir, "..\\" + path));
        }

        string FullyQualifiedFolder(string path)
        {
            path = FullyQualifiedFile(path);
            if (path.EndsWith("\\"))
                return path;
            return path + "\\";
        }

        void OnIgnoredFilesChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs) { _ignoredFilesCache = null; }

        void OnIgnoredFoldersChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs) { _ignoredFoldersCache = null; }

        /// <summary>
        /// Commits all changes without saving. <see cref="HasChanges"/> will be false after this. This method is not required when calling <see cref="Save"/>.
        /// </summary>
        public void Commit()
        {
            Version = MDKProjectProperties.TargetPackageVersion;
            HasChanges = false;
        }

        /// <summary>
        /// Saves the options of this project
        /// </summary>
        public void Save()
        {
            try
            {
                XDocument document = null;
                XElement rootElement = null;
                XElement groupElement = null;
                XElement versionElement = null;
                XElement nsElement = null;
                XElement minifyGroupElement = null;
                XElement minifyElement = null;
                XElement trimTypesGroupElement = null;
                XElement trimTypesElement = null;
                XElement ignoreElement = null;
                XElement excludeFromDeployAllElement = null;
                if (File.Exists(FileName))
                {
                    using (var streamReader = File.OpenText(FileName))
                    {
                        var readerSettings = new XmlReaderSettings
                        {
                            IgnoreWhitespace = true
                        };
                        var xmlReader = XmlReader.Create(streamReader, readerSettings);
                        document = XDocument.Load(xmlReader);
                        var nameTable = xmlReader.NameTable;
                        if (nameTable != null)
                        {
                            var nsm = new XmlNamespaceManager(nameTable);
                            nsm.AddNamespace("m", Xmlns);

                            rootElement = document.XPathSelectElement("./m:Project", nsm);
                            groupElement = document.XPathSelectElement("./m:Project/m:PropertyGroup", nsm);
                            nsElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKNamespace", nsm);
                            versionElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKVersion", nsm);
                            minifyGroupElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKMinify", nsm);
                            minifyElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKMinify/m:Enabled", nsm);
                            trimTypesGroupElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKTrimTypes", nsm);
                            trimTypesElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKTrimTypes/m:Enabled", nsm);
                            ignoreElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKIgnore", nsm);
                            excludeFromDeployAllElement = document.XPathSelectElement("./m:Project/m:PropertyGroup/m:MDKExcludeFromDeployAll", nsm);
                        }
                    }
                }

                if (document == null)
                    document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

                if (rootElement == null)
                {
                    rootElement = new XElement(XName.Get("Project", Xmlns));
                    document.Add(rootElement);
                }

                if (groupElement == null)
                {
                    groupElement = new XElement(XName.Get("PropertyGroup", Xmlns));
                    rootElement.Add(groupElement);
                }

                if (versionElement == null)
                {
                    versionElement = new XElement(XName.Get("MDKVersion", Xmlns));
                    groupElement.Add(versionElement);
                }

                versionElement.Value = MDKProjectProperties.TargetPackageVersion.ToString();

                if (Namespace != null)
                {
                    if (nsElement == null)
                    {
                        nsElement = new XElement(XName.Get("MDKNamespace", Xmlns));
                        groupElement.Add(nsElement);
                    }

                    nsElement.Value = Namespace;
                }
                else
                    nsElement?.Remove();

                if (minifyGroupElement == null)
                {
                    minifyGroupElement = new XElement(XName.Get("MDKMinify", Xmlns));
                    groupElement.Add(minifyGroupElement);
                }

                if (minifyElement == null)
                {
                    minifyElement = new XElement(XName.Get("Enabled", Xmlns));
                    minifyGroupElement.Add(minifyElement);
                }

                minifyElement.Value = Minify ? "yes" : "no";

                if (trimTypesGroupElement == null)
                {
                    trimTypesGroupElement = new XElement(XName.Get("MDKTrimTypes", Xmlns));
                    groupElement.Add(trimTypesGroupElement);
                }

                if (trimTypesElement == null)
                {
                    trimTypesElement = new XElement(XName.Get("Enabled", Xmlns));
                    trimTypesGroupElement.Add(trimTypesElement);
                }

                trimTypesElement.Value = TrimTypes ? "yes" : "no";

                ignoreElement?.Remove();
                if (IgnoredFolders.Count > 0 || IgnoredFiles.Count > 0)
                {
                    ignoreElement = new XElement(XName.Get("MDKIgnore", Xmlns));
                    foreach (var folder in IgnoredFolders)
                        ignoreElement.Add(new XElement(XName.Get("Folder", Xmlns), folder));

                    foreach (var file in IgnoredFiles)
                        ignoreElement.Add(new XElement(XName.Get("File", Xmlns), file));

                    groupElement.Add(ignoreElement);
                }

                if (ExcludeFromDeployAll)
                {
                    if (excludeFromDeployAllElement == null)
                        excludeFromDeployAllElement = new XElement(XName.Get("MDKExcludeFromDeployAll", Xmlns));
                    groupElement.Add(excludeFromDeployAllElement);
                }
                else
                    excludeFromDeployAllElement?.Remove();

                HasChanges = false;

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    ConformanceLevel = ConformanceLevel.Document,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates
                };
                using (var writer = XmlWriter.Create(FileName, settings))
                {
                    document.WriteTo(writer);
                    writer.Flush();
                }

                Commit();
            }
            catch (Exception e)
            {
                throw new MDKProjectPropertiesException($"An error occurred while attempting to save project information to {FileName}.", e);
            }
        }

        /// <summary>
        /// Called whenever a trackable property changes
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

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
