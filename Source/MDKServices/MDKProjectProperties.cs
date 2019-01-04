using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
            var mdkOptionsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.options.props"));
            var mdkPropsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.paths.props"));

            var options = MDKProjectOptions.Load(mdkOptionsFileName);
            var paths = MDKProjectPaths.Load(mdkPropsFileName);
            //if (!paths.IsValid && options.IsValid && options.Version < new Version(1, 2))
            //    paths = MDKProjectPaths.ImportLegacy(LegacyProjectScriptInfo_1_1.Load(projectFileName), mdkOptionsFileName);

            return new MDKProjectProperties(projectFileName, name, options, paths);
        }

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
    }
}
