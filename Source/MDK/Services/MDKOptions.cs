using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using Malware.MDKUtilities;
using MDK.Views;
using Microsoft.VisualStudio.Shell;

namespace MDK.Services
{
    /// <summary>
    /// Options page for the MDK extension
    /// </summary>
    [CLSCompliant(false)]
    [ComVisible(true)]
    public class MDKOptions : UIElementDialogPage, INotifyPropertyChanged
    {
        string _gameBinPath;
        bool _minify;
        string _outputPath;
        bool _useManualGameBinPath;
        bool _useManualOutputPath;
        bool _notifyUpdates = true;
        bool _notifyPrereleaseUpdates;
        SpaceEngineers _spaceEngineers;

        /// <summary>
        /// Creates an instance of <see cref="MDKOptions" />
        /// </summary>
        public MDKOptions()
        {
            _spaceEngineers = new SpaceEngineers();

            ((MDKOptionsControl)Child).Options = this;
            _gameBinPath = _spaceEngineers.GetInstallPath("Bin64");
            _outputPath = _spaceEngineers.GetDataPath("IngameScripts", "local");
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Determines whether <see cref="GameBinPath"/> should be used rather than the automatically retrieved one.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Use manual binary path")]
        [Description("If checked, use the manually specified binary path")]
        public bool UseManualGameBinPath
        {
            get => _useManualGameBinPath;
            set
            {
                if (_useManualGameBinPath == value)
                    return;
                _useManualGameBinPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// If <see cref="UseManualGameBinPath"/> is <c>true</c>, this value is used instead of the automatically retrieved path.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Space Engineers binary path")]
        [Description("A manual assignment of the path to the binary files of Space Engineers. Does not affect existing projects.")]
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                if (_gameBinPath == value)
                    return;
                _gameBinPath = value;
                if (string.IsNullOrWhiteSpace(_gameBinPath))
                    _gameBinPath = _spaceEngineers.GetInstallPath("Bin64");
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether <see cref="OutputPath"/> should be used rather than the automatically retrieved path.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Use manual output path")]
        [Description("If checked, use the manually specified output path")]
        public bool UseManualOutputPath
        {
            get => _useManualOutputPath;
            set
            {
                if (_useManualOutputPath == value)
                    return;
                _useManualOutputPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// If <see cref="UseManualOutputPath"/> is <c>true</c>, this value is used rather than the automatically retreived path.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Script Output Path")]
        [Description("A manual assignment of the path to the default output path for the final scripts. Does not affect existing projects.")]
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (_outputPath == value)
                    return;
                _outputPath = value;
                if (string.IsNullOrWhiteSpace(_outputPath))
                    _outputPath = _spaceEngineers.GetDataPath("IngameScripts", "local");
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether script projects should default to generating minified scripts.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Minify scripts")]
        [Description("Determines whether script projects should default to generating minified scripts. Does not affect existing projects.")]
        public bool Minify
        {
            get => _minify;
            set
            {
                if (_minify == value)
                    return;
                _minify = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether script projects should default to generating minified scripts.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Notify me about updates")]
        [Description("Checks for new releases on the GitHub repository, and shows a Visual Studio notification if a new version is detected.")]
        public bool NotifyUpdates
        {
            get => _notifyUpdates;
            set
            {
                if (_notifyUpdates == value)
                    return;
                _notifyUpdates = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether script projects should default to generating minified scripts.
        /// </summary>
        [Category("MDK/SE")]
        [DisplayName("Include prerelease versions")]
        [Description("Include prerelease versions when checking for new releases on the GitHub repository.")]
        public bool NotifyPrereleaseUpdates
        {
            get => _notifyPrereleaseUpdates;
            set
            {
                if (_notifyPrereleaseUpdates == value)
                    return;
                _notifyPrereleaseUpdates = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        protected sealed override UIElement Child { get; } = new MDKOptionsControl();

        /// <summary>
        /// Called when a trackable property changes.
        /// </summary>
        /// <param name="propertyName">The name of the changed property, or <c>null</c> to signify a global change.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Returns the actual game bin path which should be used, taking all settings into account.
        /// </summary>
        /// <returns></returns>
        public string GetActualGameBinPath()
        {
            return UseManualGameBinPath ? GameBinPath : _spaceEngineers.GetInstallPath("Bin64");
        }

        /// <summary>
        /// Returns the actual output path which should be used, taking all settings into account.
        /// </summary>
        /// <returns></returns>
        public string GetActualOutputPath()
        {
            return UseManualOutputPath ? OutputPath : _spaceEngineers.GetDataPath("IngameScripts", "local");
        }
    }
}
