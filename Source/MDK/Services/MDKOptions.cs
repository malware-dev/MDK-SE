using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
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

        /// <summary>
        /// Creates an instance of <see cref="MDKOptions" />
        /// </summary>
        public MDKOptions()
        {
            SpaceEngineers = new SpaceEngineers();

            ((MDKOptionsControl)Child).Options = this;
            _gameBinPath = SpaceEngineers.GetInstallPath("Bin64");
            _outputPath = SpaceEngineers.GetDataPath("IngameScripts", "local");
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The <see cref="SpaceEngineers"/> service
        /// </summary>
        public SpaceEngineers SpaceEngineers { get; }

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
                    _gameBinPath = SpaceEngineers.GetInstallPath("Bin64");
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
                    _outputPath = SpaceEngineers.GetDataPath("IngameScripts", "local");
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
    }
}
